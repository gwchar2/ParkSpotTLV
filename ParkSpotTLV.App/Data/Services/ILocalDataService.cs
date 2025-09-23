using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Services;

public interface ILocalDataService
{
    Task InitializeAsync();

    Task<UserPreferences> GetUserPreferencesAsync();
    Task SaveUserPreferencesAsync(UserPreferences preferences);

    Task<LocalUser?> GetCurrentUserAsync();
    Task SaveUserAsync(LocalUser user);
    Task<bool> IsUserLoggedInAsync();
    Task LogoutAsync();

    Task<List<LocalVehicle>> GetUserVehiclesAsync(string userId);
    Task SaveVehicleAsync(LocalVehicle vehicle);
    Task DeleteVehicleAsync(string vehicleId);

    Task<List<LocalPermit>> GetVehiclePermitsAsync(string vehicleId);
    Task SavePermitAsync(LocalPermit permit);
    Task DeletePermitAsync(string permitId);
    Task<List<LocalPermit>> GetZonePermitsAsync(string zoneId);

    Task<List<LocalZone>> GetZonesAsync();
    Task SaveZonesAsync(List<LocalZone> zones);

    Task<List<LocalStreetSegment>> GetStreetSegmentsAsync(string? zoneId = null);
    Task SaveStreetSegmentsAsync(List<LocalStreetSegment> segments);

    Task ClearCacheAsync();
    Task<DateTime?> GetLastSyncTimeAsync();
    Task UpdateLastSyncTimeAsync();
}