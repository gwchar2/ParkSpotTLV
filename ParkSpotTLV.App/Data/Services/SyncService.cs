using System.Net.Http.Json;
using System.Text.Json;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Services;

public class SyncService : ISyncService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalDataService _localDataService;
    private readonly string _baseUrl = "http://localhost:8080";
    private bool _isSyncing = false;

    public SyncService(HttpClient httpClient, ILocalDataService localDataService)
    {
        _httpClient = httpClient;
        _localDataService = localDataService;
    }

    public async Task<bool> SyncAllDataAsync()
    {
        if (_isSyncing) return false;

        try
        {
            _isSyncing = true;

            var geographicSync = await SyncGeographicDataAsync();
            var vehicleSync = await SyncUserVehiclesAsync();

            if (geographicSync || vehicleSync)
            {
                await _localDataService.UpdateLastSyncTimeAsync();
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    public async Task<bool> SyncGeographicDataAsync()
    {
        try
        {
            var zones = await FetchZonesFromApiAsync();
            if (zones?.Any() == true)
            {
                await _localDataService.SaveZonesAsync(zones);
            }

            var segments = await FetchStreetSegmentsFromApiAsync();
            if (segments?.Any() == true)
            {
                await _localDataService.SaveStreetSegmentsAsync(segments);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SyncUserVehiclesAsync()
    {
        try
        {
            var currentUser = await _localDataService.GetCurrentUserAsync();
            if (currentUser?.AuthToken == null) return false;

            var vehicles = await FetchUserVehiclesFromApiAsync(currentUser.Id, currentUser.AuthToken);
            if (vehicles?.Any() == true)
            {
                foreach (var vehicle in vehicles)
                {
                    await _localDataService.SaveVehicleAsync(vehicle);
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsSyncNeededAsync(TimeSpan maxAge)
    {
        var lastSync = await _localDataService.GetLastSyncTimeAsync();
        if (lastSync == null) return true;

        return DateTime.UtcNow - lastSync.Value > maxAge;
    }

    public Task SyncInBackgroundAsync()
    {
        return Task.Run(async () =>
        {
            try
            {
                if (await IsSyncNeededAsync(TimeSpan.FromHours(1)))
                {
                    await SyncAllDataAsync();
                }
            }
            catch
            {
            }
        });
    }

    public async Task<SyncStatus> GetSyncStatusAsync()
    {
        var lastSync = await _localDataService.GetLastSyncTimeAsync();
        var zones = await _localDataService.GetZonesAsync();
        var segments = await _localDataService.GetStreetSegmentsAsync();

        var currentUser = await _localDataService.GetCurrentUserAsync();
        var vehicles = currentUser != null
            ? await _localDataService.GetUserVehiclesAsync(currentUser.Id)
            : new List<LocalVehicle>();

        return new SyncStatus
        {
            LastSyncTime = lastSync,
            IsSyncing = _isSyncing,
            HasInternetConnection = await CheckInternetConnectionAsync(),
            CachedZones = zones.Count,
            CachedStreetSegments = segments.Count,
            CachedVehicles = vehicles.Count
        };
    }

    private async Task<List<LocalZone>?> FetchZonesFromApiAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/zones");
            if (!response.IsSuccessStatusCode) return null;

            var serverZones = await response.Content.ReadFromJsonAsync<List<ServerZone>>();
            if (serverZones == null) return null;

            return serverZones.Select(z => new LocalZone
            {
                Id = z.Id.ToString(),
                Code = z.Code,
                Name = z.Name,
                Taarif = ParseTaarif(z.Taarif?.ToString()),
                GeometryJson = JsonSerializer.Serialize(z.Geom),
                LastUpdated = z.LastUpdated,
                CachedAt = DateTime.UtcNow,
                IsActive = true
            }).ToList();
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<LocalStreetSegment>?> FetchStreetSegmentsFromApiAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/street-segments");
            if (!response.IsSuccessStatusCode) return null;

            var serverSegments = await response.Content.ReadFromJsonAsync<List<ServerStreetSegment>>();
            if (serverSegments == null) return null;

            return serverSegments.Select(s => new LocalStreetSegment
            {
                Id = s.Id.ToString(),
                Name = s.Name,
                GeometryJson = JsonSerializer.Serialize(s.Geom),
                ZoneId = s.ZoneId?.ToString(),
                ParkingType = ParseParkingType(s.ParkingType?.ToString()),
                ParkingHours = ParseParkingHours(s.ParkingHours?.ToString()),
                Side = ParseSegmentSide(s.Side?.ToString()),
                CarsOnly = s.CarsOnly,
                LengthMeters = s.LengthMeters,
                StylePriority = s.StylePriority,
                LastUpdated = s.LastUpdated,
                CachedAt = DateTime.UtcNow,
                IsActive = true
            }).ToList();
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<LocalVehicle>?> FetchUserVehiclesFromApiAsync(string userId, string authToken)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}/vehicles");
            if (!response.IsSuccessStatusCode) return null;

            var serverVehicles = await response.Content.ReadFromJsonAsync<List<ServerVehicle>>();
            if (serverVehicles == null) return null;

            return serverVehicles.Select(v => new LocalVehicle
            {
                Id = v.Id.ToString(),
                UserId = userId,
                Type = ParseVehicleType(v.Type?.ToString()),
                CachedAt = DateTime.UtcNow,
                IsActive = true
            }).ToList();
        }
        catch
        {
            return null;
        }
    }

    private ParkingType ParseParkingType(string? value)
    {
        return value?.ToLower() switch
        {
            "free" => ParkingType.Free,
            "paid" => ParkingType.Paid,
            "limited" => ParkingType.Limited,
            _ => ParkingType.Unknown
        };
    }

    private ParkingHours ParseParkingHours(string? value)
    {
        return value?.ToLower() switch
        {
            "specifichours" => ParkingHours.SpecificHours,
            _ => ParkingHours.Unknown
        };
    }

    private SegmentSide ParseSegmentSide(string? value)
    {
        return value?.ToLower() switch
        {
            "left" => SegmentSide.Left,
            "right" => SegmentSide.Right,
            "both" => SegmentSide.Both,
            _ => SegmentSide.Both
        };
    }

    private VehicleType ParseVehicleType(string? value)
    {
        return value?.ToLower() switch
        {
            "truck" => VehicleType.Truck,
            "car" => VehicleType.Car,
            _ => VehicleType.Car
        };
    }

    private Taarif ParseTaarif(string? value)
    {
        return value?.ToLower() switch
        {
            "city_center" or "1" => Taarif.City_Center,
            "city_outskirts" or "2" => Taarif.City_Outskirts,
            _ => Taarif.City_Center
        };
    }

    private async Task<bool> CheckInternetConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/ready");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

public class ServerZone
{
    public Guid Id { get; set; }
    public int? Code { get; set; }
    public string? Name { get; set; }
    public object? Taarif { get; set; }
    public object Geom { get; set; } = default!;
    public DateTimeOffset? LastUpdated { get; set; }
}

public class ServerStreetSegment
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public object Geom { get; set; } = default!;
    public Guid? ZoneId { get; set; }
    public bool CarsOnly { get; set; } = false;
    public object? ParkingType { get; set; }
    public object? ParkingHours { get; set; }
    public object? Side { get; set; }
    public double? LengthMeters { get; set; }
    public int StylePriority { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
}

public class ServerVehicle
{
    public Guid Id { get; set; }
    public object? Type { get; set; }
    public List<ServerPermit>? Permits { get; set; }
}

public class ServerPermit
{
    public object? Type { get; set; }
    public Guid? ZoneId { get; set; }
}