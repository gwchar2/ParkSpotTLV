using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Services;

public interface ILocalDataService
{
    public Task InitializeAsync();
    public Task AddSessionAsync(Session session);
    public Task DeleteSessionAsync();
    public Task UpdatePreferencesAsync(int? minParkingTime = null,
                                    bool? notificationsEnabled = null,
                                    int? notificationMinutesBefore = null,
                                    bool? showFree = null,
                                    bool? showPaid = null,
                                    bool? showRestricted = null,
                                    bool? showNoParking = null,
                                    String? lastPickedCarId = null);
    public Task UpdateTokenAsync(String token, DateTimeOffset expiresAt);
    public Task<Session?> GetSessionAsync();
}