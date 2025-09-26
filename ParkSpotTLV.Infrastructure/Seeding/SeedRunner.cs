using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParkSpotTLV.Infrastructure.Entities;
using ParkSpotTLV.Core.Models;
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

    public sealed class SeedRunner : IHostedService {
        private readonly IServiceProvider _sp;
        private readonly IHostEnvironment _env;
        private readonly ILogger<SeedRunner> _log;
        private readonly SeedOptions _opts;

        public SeedRunner(
            IServiceProvider sp,
            IHostEnvironment env,
            IOptions<SeedOptions> opts,
            ILogger<SeedRunner> log) {
            _sp = sp;
            _env = env;
            _opts = opts.Value;
            _log = log;
            }

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
                var temp = GetInt(props, "taarif");
                zone.Taarif = temp.HasValue ? (Taarif)temp.Value : Taarif.City_Center;
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

            var zonesByCode = await db.Zones.AsNoTracking()
                .Where(z => z.Code != null)
                .ToDictionaryAsync(z => z.Code!.Value, z => z.Id, ct);

            foreach (var (geom, props) in GeoJsonLoader.LoadFeatures(_opts.Paths.StreetSegments)) {
                var line = ToLineString(geom);
                var (ptype, pside) = ParseParkingTags(props);
                var osmID = GetString(props, "@id");

                var segment = new StreetSegment {
                    OSMId = string.IsNullOrWhiteSpace(osmID) ? "" : osmID,
                    NameEnglish = GetString(props, "name:en"),
                    NameHebrew = GetString(props, "name"),
                    Geom = line,
                    ParkingType = ptype,
                    Side = pside,
                    LastUpdated = DateTimeOffset.UtcNow
                };

                int? zoneCode = null;
                var rawZone = GetString(props, "parking_zone");
                if (!string.IsNullOrWhiteSpace(rawZone) && int.TryParse(rawZone, out var parsed))
                    zoneCode = parsed;

                if (zoneCode.HasValue && zonesByCode.TryGetValue(zoneCode.Value, out var zid))
                    segment.ZoneId = zid;

                db.StreetSegments.Add(segment);
            }

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

            var zonesByCode = await db.Zones.AsNoTracking()
                .Where(z => z.Code != null)
                .ToDictionaryAsync(z => z.Code!.Value, z => z.Id, ct);

            var json = await File.ReadAllTextAsync(_opts.Paths.Users, ct);
            var users = JsonSerializer.Deserialize<List<JsonObject>>(json) ?? new();

            foreach (var u in users) {
                var user = new User {
                    Id = ParseGuid(GetString(u, "id")) ?? Guid.NewGuid(),
                    Username = GetString(u, "username") ?? "user",
                    PasswordHash = GetString(u, "passwordHash") ?? ""
                };

                // Vehicles + vehicle-scoped permits (no user-scoped permits in current model)
                foreach (var v in u["vehicles"]?.AsArray() ?? new()) {
                    var vo = v!.AsObject();
                    var vehicle = new Vehicle {
                        Id = ParseGuid(GetString(vo, "id")) ?? Guid.NewGuid(),
                        Owner = user,
                        Name = GetString(vo, "name") ?? "Default Car Name",
                        Type = ParseEnum<VehicleType>(GetString(vo, "type")) ?? VehicleType.Car
                    };

                    foreach (var p in vo["permits"]?.AsArray() ?? new()) {
                        var po = p!.AsObject();
                        var permit = new Permit {
                            Id = ParseGuid(GetString(po, "id")) ?? Guid.NewGuid(),
                            Type = ParseEnum<PermitType>(GetString(po, "type")) ?? PermitType.Default,
                            Vehicle = vehicle,
                            ValidTo = ParseDateOnly(GetString(po, "validTo")),
                            IsActive = GetBool(po, "isActive") ?? true
                        };

                        var zoneCode = GetInt(po, "zoneCode");
                        if (zoneCode is not null && zonesByCode.ContainsKey(zoneCode.Value))
                            permit.ZoneCode = zoneCode.Value;




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
                NetTopologySuite.Geometries.Polygon p => new NetTopologySuite.Geometries.MultiPolygon(new[] { p }),
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
        private static DateOnly? ParseDateOnly(string? s) => DateOnly.TryParse(s, out var d) ? d : null;

        private static TEnum? ParseEnum<TEnum>(string? s) where TEnum : struct {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return Enum.TryParse<TEnum>(s, ignoreCase: true, out var v) ? v : null;
        }

        private static (ParkingType type, SegmentSide side) ParseParkingTags(JsonObject props) {
            string? T(string key) =>
                props.TryGetPropertyValue(key, out var v) ? v?.ToString()?.Trim().ToLowerInvariant() : null;

            bool IsYes(string? s) => s == "yes" || s == "true" || s == "designated";
            bool IsNo(string? s) => s == "no" || s == "false";

            // same logic as before
            var bothRaw = T("parking:both");
            bool leftAllowed, rightAllowed;

            if (bothRaw != null) {
                if (IsYes(bothRaw)) { leftAllowed = rightAllowed = true; } else if (IsNo(bothRaw)) { leftAllowed = rightAllowed = false; } else {
                    leftAllowed = IsYes(T("parking:left"));
                    rightAllowed = IsYes(T("parking:right"));
                }
            } else {
                leftAllowed = IsYes(T("parking:left"));
                rightAllowed = IsYes(T("parking:right"));
            }

            SegmentSide side;
            if (leftAllowed && rightAllowed) side = SegmentSide.Both;
            else if (leftAllowed) side = SegmentSide.Left;
            else if (rightAllowed) side = SegmentSide.Right;
            else side = SegmentSide.Both; // “none” in current enum

            ParkingType type = ParkingType.CantPark;
            if (leftAllowed || rightAllowed) {
                switch (T("parking:type")) {
                    case "paid": type = ParkingType.Paid; break;
                    case "free": type = ParkingType.Free; break;
                    default: type = ParkingType.CantPark; break;
                }
            }

            return (type, side);
        }
    }
}
