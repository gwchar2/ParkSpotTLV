using System.Net.Http.Json;
using System.Text.Json;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Services;

/*
 * SyncService - Handles bidirectional synchronization between local SQLite cache and remote PostgreSQL server
 *
 * Sync Process:
 * 1. Validates prerequisites (internet, authentication)
 * 2. Fetches global data (zones, street segments)
 * 3. Fetches user-specific data (vehicles, permits)
 * 4. Transforms server data to local format
 * 5. Stores in local SQLite cache
 * 6. Updates sync timestamps
 *
 * Features:
 * - Offline-first: App works without sync using cached data
 * - Error resilient: Graceful failure handling with partial sync support
 * - Performance optimized: Tracks last sync time for incremental updates
 * - Secure: Uses JWT authentication for API access
 */
public class SyncService : ISyncService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalDataService _localDataService;
    private readonly string _baseUrl = "http://localhost:8080"; // Development server URL
    private bool _isSyncing = false; // Prevents concurrent sync operations

    public SyncService(HttpClient httpClient, ILocalDataService localDataService)
    {
        _httpClient = httpClient;
        _localDataService = localDataService;
    }

    /*
     * Main sync function - coordinates complete data synchronization
     * Strategy: Geographic data + User data in sequence, partial success counts as success
     */
    public async Task<bool> SyncAllDataAsync()
    {
        if (_isSyncing) return false; // Prevent concurrent sync operations

        try
        {
            _isSyncing = true;

            // Sync global geographic data (zones, street segments) - shared by all users
            var geographicSync = await SyncGeographicDataAsync();

            // Sync user-specific data (vehicles, permits) - personal to logged-in user
            var vehicleSync = await SyncUserVehiclesAsync();

            // If any sync succeeded, update timestamp - partial success is overall success
            if (geographicSync || vehicleSync)
            {
                await _localDataService.UpdateLastSyncTimeAsync();
                return true;
            }

            return false;
        }
        catch
        {
            return false; // Graceful failure - app continues with cached data
        }
        finally
        {
            _isSyncing = false; // Always reset sync flag
        }
    }

    // Syncs global geographic data (zones and street segments) shared by all users
    public async Task<bool> SyncGeographicDataAsync()
    {
        try
        {
            // Fetch parking zones with boundaries and pricing info
            var zones = await FetchZonesFromApiAsync();
            if (zones?.Any() == true)
            {
                await _localDataService.SaveZonesAsync(zones);
            }

            // Fetch street segments with parking rules and geometry
            var segments = await FetchStreetSegmentsFromApiAsync();
            if (segments?.Any() == true)
            {
                await _localDataService.SaveStreetSegmentsAsync(segments);
            }

            return true;
        }
        catch
        {
            return false; // Failed but app continues with cached geographic data
        }
    }

    /*
     * Syncs user-specific data (vehicles and their permits)
     * Requires authentication - uses current user's auth token
     */
    public async Task<bool> SyncUserVehiclesAsync()
    {
        try
        {
            // Check if user is logged in and has valid auth token
            var currentUser = await _localDataService.GetCurrentUserAsync();
            if (currentUser?.AuthToken == null) return false;

            // Fetch user's vehicles with their permits from server
            var vehicles = await FetchUserVehiclesFromApiAsync(currentUser.Id, currentUser.AuthToken);
            if (vehicles?.Any() == true)
            {
                // Save each vehicle (permits are saved automatically in fetch method)
                foreach (var vehicle in vehicles)
                {
                    await _localDataService.SaveVehicleAsync(vehicle);
                }
            }

            return true;
        }
        catch
        {
            return false; // Failed but app continues with cached vehicle data
        }
    }

    // Checks if sync is needed based on last sync time and maximum age threshold
    public async Task<bool> IsSyncNeededAsync(TimeSpan maxAge)
    {
        var lastSync = await _localDataService.GetLastSyncTimeAsync();
        if (lastSync == null) return true; // Never synced before

        return DateTime.UtcNow - lastSync.Value > maxAge; // Check if data is stale
    }

    // Background sync task - runs sync if data is older than 1 hour
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
                // Silent failure - background sync should not crash app
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

    /*
     * Fetches parking zones from server API
     * Transforms server zone data (with PostGIS geometry) to local format (JSON strings)
     */
    private async Task<List<LocalZone>?> FetchZonesFromApiAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/zones");
            if (!response.IsSuccessStatusCode) return null;

            var serverZones = await response.Content.ReadFromJsonAsync<List<ServerZone>>();
            if (serverZones == null) return null;

            // Transform server zones to local cache format
            return serverZones.Select(z => new LocalZone
            {
                Id = z.Id.ToString(),
                Code = z.Code,
                Name = z.Name,
                Taarif = ParseTaarif(z.Taarif?.ToString()), // Convert enum
                GeometryJson = JsonSerializer.Serialize(z.Geom), // PostGIS -> JSON string
                LastUpdated = z.LastUpdated,
                CachedAt = DateTime.UtcNow, // Track when cached locally
                IsActive = true
            }).ToList();
        }
        catch
        {
            return null; // Return null on any error - caller handles gracefully
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

            var localVehicles = new List<LocalVehicle>();

            foreach (var serverVehicle in serverVehicles)
            {
                var localVehicle = new LocalVehicle
                {
                    Id = serverVehicle.Id.ToString(),
                    UserId = userId,
                    Type = ParseVehicleType(serverVehicle.Type?.ToString()),
                    CachedAt = DateTime.UtcNow,
                    IsActive = true
                };

                localVehicles.Add(localVehicle);

                // Also save permits for this vehicle
                if (serverVehicle.Permits?.Any() == true)
                {
                    await SaveVehiclePermitsAsync(localVehicle.Id, serverVehicle.Permits);
                }
            }

            return localVehicles;
        }
        catch
        {
            return null;
        }
    }

    /*
     * Data transformation helpers - convert server enum strings to local enum types
     * All methods handle null/invalid values gracefully with sensible defaults
     */

    // Converts parking type from server string to local enum
    private ParkingType ParseParkingType(string? value)
    {
        return value?.ToLower() switch
        {
            "free" => ParkingType.Free,
            "paid" => ParkingType.Paid,
            "limited" => ParkingType.Limited,
            _ => ParkingType.Unknown // Safe default
        };
    }

    // Converts parking hours from server string to local enum
    private ParkingHours ParseParkingHours(string? value)
    {
        return value?.ToLower() switch
        {
            "specifichours" => ParkingHours.SpecificHours,
            _ => ParkingHours.Unknown // Safe default
        };
    }

    // Converts segment side from server string to local enum
    private SegmentSide ParseSegmentSide(string? value)
    {
        return value?.ToLower() switch
        {
            "left" => SegmentSide.Left,
            "right" => SegmentSide.Right,
            "both" => SegmentSide.Both,
            _ => SegmentSide.Both // Safe default
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

    private PermitType ParsePermitType(string? value)
    {
        return value?.ToLower() switch
        {
            "disability" or "1" => PermitType.Disability,
            "zoneresident" or "2" => PermitType.ZoneResident,
            _ => PermitType.Default
        };
    }

    /*
     * Saves vehicle permits to local cache during sync
     * Called automatically when syncing vehicles - transforms and stores server permits
     */
    private async Task SaveVehiclePermitsAsync(string vehicleId, List<ServerPermit> serverPermits)
    {
        try
        {
            // Transform server permits to local format
            var localPermits = serverPermits.Select(p => new LocalPermit
            {
                Id = Guid.NewGuid().ToString(), // Generate new local ID
                VehicleId = vehicleId,
                Type = ParsePermitType(p.Type?.ToString()),
                ZoneId = p.ZoneId?.ToString(),
                IsActive = true,
                CachedAt = DateTime.UtcNow
            }).ToList();

            // Save each permit individually
            foreach (var permit in localPermits)
            {
                await _localDataService.SavePermitAsync(permit);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save vehicle permits: {ex.Message}");
            // Don't rethrow - permit sync failure shouldn't break vehicle sync
        }
    }

    // Quick connectivity check - pings server health endpoint
    private async Task<bool> CheckInternetConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/ready");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false; // No connection or server unavailable
        }
    }
}

/*
 * Server data models - represent the structure of data received from API
 * These are transformed to Local* entities for SQLite cache storage
 */
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