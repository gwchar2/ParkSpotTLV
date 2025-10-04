using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Infrastructure.Entities;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ParkSpotTLV.Infrastructure.Seeding {
    public sealed class SeedOptions {
        public bool Enabled { get; set; } = true;
        public SeedPaths Paths { get; set; } = new();
    }

    /* ALWAYS UPDATE PATHS IN DOCKERFILE AS WELL!!! */
    public sealed class SeedPaths {
        public string Zones { get; set; } = "ParkSpotTLV.Infrastructure/db/Seed/zones.geojson";
        public string Users { get; set; } = "ParkSpotTLV.Infrastructure/db/Seed/users_seed.json";
        public string StreetSegments { get; set; } = "ParkSpotTLV.Infrastructure/db/Seed/street_segments.geojson";
    }

    public sealed class SeedRunner(
        IServiceProvider sp,
        IHostEnvironment env,
        IOptions<SeedOptions> opts,
        ILogger<SeedRunner> log) : IHostedService {
        private readonly IServiceProvider _sp = sp;
        private readonly IHostEnvironment _env = env;
        private readonly ILogger<SeedRunner> _log = log;
        private readonly SeedOptions _opts = opts.Value;

        public async Task StartAsync(CancellationToken ct) {
            if (!_opts.Enabled) {
                _log.LogInformation("Seeding disabled via configuration (env: {Env}).", _env.EnvironmentName);
                return;
            }

            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // If migrations haven’t been applied yet, skip (so you see a clear warning)
            if ((await db.Database.GetPendingMigrationsAsync(ct)).Any()) {
                _log.LogWarning("Pending migrations detected. Apply migrations before seeding.");
                return;
            }

            await SeedZonesAsync(db, ct);
            await SeedStreetSegmentsAsync(db, ct);
            await SeedUsersAsync(db, ct);
            await SeedTariffWindowsAsync(db, ct);

            _log.LogInformation("Seeding completed.");
        }

        public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

        // ---------------------- Zones ----------------------

        private async Task SeedZonesAsync(AppDbContext db, CancellationToken ct) {
            if (await db.Zones.AsNoTracking().AnyAsync(ct)) {
                _log.LogInformation("Zones already present. Skipping.");
                return;
            }

            _log.LogInformation("Seeding Zones from {Path}", _opts.Paths.Zones);

            foreach (var (geom, props) in GeoJsonLoader.LoadFeatures(_opts.Paths.Zones)) {
                var zone = new Zone {
                    Geom = ToMultiPolygon(geom),
                    Code = GetInt(props, "code"),
                    Name = GetString(props, "name"),
                    LastUpdated = DateTimeOffset.UtcNow
                };
                var temp = GetInt(props, "tariff");
                zone.Taarif = temp.HasValue ? (Tariff)temp.Value : Tariff.City_Center;
                db.Zones.Add(zone);
            }

            await db.SaveChangesAsync(ct);
        }

        // ---------------------- Street Segments ----------------------
        private async Task SeedStreetSegmentsAsync(AppDbContext db, CancellationToken ct) {
            if (await db.StreetSegments.AsNoTracking().AnyAsync(ct)) {
                _log.LogInformation("StreetSegments already present. Skipping.");
                return;
            }

            _log.LogInformation("Seeding StreetSegments from {Path}", _opts.Paths.StreetSegments);

            var zonesByCode = await db.Zones.AsNoTracking().Where(z => z.Code.HasValue).ToDictionaryAsync(z => z.Code!.Value, z => z.Id, ct);

            foreach (var (geom, props) in GeoJsonLoader.LoadFeatures(_opts.Paths.StreetSegments)) {
                var line = ToLineString(geom);
                var osmID = GetString(props, "@id");

                var (type, side, explicitZoneCode) = ParseParkingTags(props);           // returns (type, side, explicitZoneCode, Priveleged?)
                var segment = new StreetSegment {
                    OSMId = string.IsNullOrWhiteSpace(osmID) ? "" : osmID,
                    NameEnglish = GetString(props, "name:en"),
                    NameHebrew = GetString(props, "name"),
                    Geom = line,
                    ParkingType = type,
                    Side = side
                };

                Guid? zoneId = null;

                // Prefer explicit zone code from tags (paid)
                if (explicitZoneCode.HasValue && zonesByCode.TryGetValue(explicitZoneCode.Value, out var zidFromProps))
                    zoneId = zidFromProps;

                // If free and no explicit zone, infer by geometry
                if (zoneId == null && type == ParkingType.Free) {
                    var centroid = line.Centroid;

                    // Try centroid containment
                    var zoneByCentroidId = await db.Zones.AsNoTracking()
                        .Where(z => z.Geom != null && z.Geom.Contains(centroid))
                        .Select(z => (Guid?)z.Id)
                        .FirstOrDefaultAsync(ct);

                    if (zoneByCentroidId != null) {
                        zoneId = zoneByCentroidId;
                    } else {
                        var zoneByIntersectId = await db.Zones.AsNoTracking()
                            .Where(z => z.Geom != null && z.Geom.Intersects(line))
                            .Select(z => (Guid?)z.Id)
                            .FirstOrDefaultAsync(ct);

                        if (zoneByIntersectId != null)
                            zoneId = zoneByIntersectId;
                    }
                }

                segment.ZoneId = zoneId;
                db.StreetSegments.Add(segment);
            }

            await db.SaveChangesAsync(ct);
        }

        // ---------------------- Taarif Windows Async ----------------------
        /* 
        * Seeds A/B tariff paid windows:
        *  - Group A: Sun–Thu 08:00–19:00; Fri 08:00–17:00; Sat none
        *  - Group B: Sun–Thu 08:00–21:00; Fri 08:00–17:00; Sat none
        *  - Outside paid windows -> FREE by definition.
        */
        private static async Task SeedTariffWindowsAsync(AppDbContext db, CancellationToken ct) {
            if (await db.TariffWindows.AnyAsync(ct))
                return; 

            var id = 1;
            var data = new List<TariffWindow>();

            void AddRange(Tariff t, (DayOfWeek dow, string start, string end)[] items) {
                foreach (var (dow, start, end) in items) {
                    data.Add(new TariffWindow {
                        Id = id++,
                        Tariff = t,
                        DayOfWeek = dow,
                        StartLocal = TimeOnly.Parse(start),
                        EndLocal = TimeOnly.Parse(end)
                    });
                }
            }

            // City_Center
            AddRange(Tariff.City_Center,[
                (DayOfWeek.Sunday,    "08:00", "21:00"),
                (DayOfWeek.Monday,    "08:00", "21:00"),
                (DayOfWeek.Tuesday,   "08:00", "21:00"),
                (DayOfWeek.Wednesday, "08:00", "21:00"),
                (DayOfWeek.Thursday,  "08:00", "21:00"),
                (DayOfWeek.Friday,    "08:00", "17:00"),
            ]);

            // City_Outskirts
            AddRange(Tariff.City_Outskirts,[
                (DayOfWeek.Sunday,    "08:00", "19:00"),
                (DayOfWeek.Monday,    "08:00", "19:00"),
                (DayOfWeek.Tuesday,   "08:00", "19:00"),
                (DayOfWeek.Wednesday, "08:00", "19:00"),
                (DayOfWeek.Thursday,  "08:00", "19:00"),
                (DayOfWeek.Friday,    "08:00", "17:00"),
            ]);

            db.TariffWindows.AddRange(data);
            await db.SaveChangesAsync(ct);
        }

        // ---------------------- Users / Vehicles / Permits ----------------------

        private async Task SeedUsersAsync(AppDbContext db, CancellationToken ct) {
            if (await db.Users.AsNoTracking().AnyAsync(ct)) {
                _log.LogInformation("Users already present. Skipping.");
                return;
            }

            if (!File.Exists(_opts.Paths.Users)) {
                _log.LogWarning("Users seed file missing: {Path}. Skipping users.", _opts.Paths.Users);
                return;
            }

            _log.LogInformation("Seeding Users from {Path}", _opts.Paths.Users);

            var zonesByCode = await db.Zones.AsNoTracking().Where(z => z.Code != null).ToDictionaryAsync(z => z.Code!.Value, z => z, ct);

            var json = await File.ReadAllTextAsync(_opts.Paths.Users, ct);
            var users = JsonSerializer.Deserialize<List<JsonObject>>(json) ?? [];

            foreach (var u in users) {
                var user = new User {
                    Id = ParseGuid(GetString(u, "id")) ?? Guid.NewGuid(),
                    Username = GetString(u, "username") ?? "user",
                    PasswordHash = GetString(u, "passwordHash") ?? "",
                };

                // Vehicles + vehicle-scoped permits (no user-scoped permits in current model)
                foreach (var v in u["vehicles"]?.AsArray() ?? []) {
                    var vo = v!.AsObject();
                    var vehicle = new Vehicle {
                        Id = ParseGuid(GetString(vo, "id")) ?? Guid.NewGuid(),
                        Owner = user,
                        Name = GetString(vo, "name") ?? "Default Car Name",
                        Type = ParseEnum<VehicleType>(GetString(vo, "type")) ?? VehicleType.Private
                    };

                    foreach (var p in vo["permits"]?.AsArray() ?? []) {
                        var po = p!.AsObject();
                        var permit = new Permit {
                            Id = ParseGuid(GetString(po, "id")) ?? Guid.NewGuid(),
                            Type = ParseEnum<PermitType>(GetString(po, "type")) ?? PermitType.Default,
                            Vehicle = vehicle
                        };

                        var zoneCode = GetInt(po, "zoneCode");
                        if (zoneCode is not null && zonesByCode.ContainsKey(zoneCode.Value)) 
                            permit.ZoneCode = zoneCode.Value;

                        permit.LastUpdated = DateTimeOffset.UtcNow;
                        vehicle.Permits.Add(permit);
                    }
                    user.Vehicles.Add(vehicle);
                }

                db.Users.Add(user);
            }

            await db.SaveChangesAsync(ct);
        }

        // ---------------------- Helpers ----------------------

        private static NetTopologySuite.Geometries.MultiPolygon ToMultiPolygon(NetTopologySuite.Geometries.Geometry g)
            => g switch {
                NetTopologySuite.Geometries.MultiPolygon mp => mp,
                NetTopologySuite.Geometries.Polygon p => new NetTopologySuite.Geometries.MultiPolygon([p]),
                _ => throw new InvalidDataException($"Expected Polygon/MultiPolygon, got {g.GeometryType}.")
            };

        private static NetTopologySuite.Geometries.LineString ToLineString(NetTopologySuite.Geometries.Geometry g)
            => g switch {
                NetTopologySuite.Geometries.LineString ls => ls,
                _ => throw new InvalidDataException($"Expected LineString, got {g.GeometryType}.")
            };

        // Json helpers
        private static string? GetString(JsonObject o, string key) => o[key]?.GetValue<string>();
        private static int? GetInt(JsonObject o, string key) => o[key] is null ? null : o[key]!.GetValue<int?>();
        private static bool? GetBool(JsonObject o, string key) => o[key] is null ? null : o[key]!.GetValue<bool?>();
        private static Guid? ParseGuid(string? s) => Guid.TryParse(s, out var g) ? g : null;

        private static TEnum? ParseEnum<TEnum>(string? s) where TEnum : struct {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return Enum.TryParse<TEnum>(s, ignoreCase: true, out var v) ? v : null;
        }
        private static int? TryParseInt(string? s) {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return int.TryParse(s, out var n) ? n : null;
        }
        private static (ParkingType type, SegmentSide side, int? explicitZoneCode) ParseParkingTags(JsonObject props) {
            string? T(string key) =>
                props.TryGetPropertyValue(key, out var v) ? v?.ToString()?.Trim() : null;

            // Paid variants (with explicit zone in tags)
            // We check specific sides first; if both present, side=Both.
            int? zoneRight = TryParseInt(T("parking:right:zone"));
            int? zoneLeft = TryParseInt(T("parking:left:zone"));
            int? zoneBoth = TryParseInt(T("parking:both:zone"));

            bool paidRight = zoneRight.HasValue;
            bool paidLeft = zoneLeft.HasValue;
            bool paidBoth = zoneBoth.HasValue;

            // Figure out if parking is privileged or not
            bool privileged = string.Equals(T("restriction:conditional"),  "zone_only @ 8:00-21:00", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(T("restriction:conditional"), "zone_only @ 8:00-17:00", StringComparison.OrdinalIgnoreCase);

            // If any paid tag exists, we consider the segment Paid.
            if (paidRight || paidLeft || paidBoth) {
                var side = SegmentSide.Both;
                if ((paidLeft || paidRight) && !(paidLeft && paidRight)) {
                    side = paidLeft ? SegmentSide.Left : SegmentSide.Right;
                } else if (paidBoth) {
                    side = SegmentSide.Both;
                } else if (paidLeft && paidRight) {
                    side = SegmentSide.Both;
                }
                // Pick a zone code deterministically: prefer specific side over "both"
                int? zoneCode = zoneRight ?? zoneLeft ?? zoneBoth;

                return (privileged ? ParkingType.Privileged : ParkingType.Paid, side, zoneCode);
            } 

            // If nothing matched (shouldn’t happen after your prefilter), default to Free/Both without zone.
            return (privileged ? ParkingType.Privileged : ParkingType.Free, SegmentSide.Both, null);
        }

        

    }

}
