using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Services;

public interface ILocalDataService
{
    Task InitializeAsync();
    Task<Session?> GetSessionAsync();
    Task AddSessionAsync(Session session);
    Task DeleteSessionAsync();
    Task UpdateTokenAsync(string token, DateTimeOffset expiresAt);
    Task UpdatePreferencesAsync(int? minParkingTime = null,
                                bool? showFree = null,
                                bool? showPaid = null,
                                bool? showRestricted = null,
                                bool? showNoParking = null,
                                string? lastPickedCarId = null);
}
