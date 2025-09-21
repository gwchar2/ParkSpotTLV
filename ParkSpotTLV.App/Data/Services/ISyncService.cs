namespace ParkSpotTLV.App.Data.Services;

public interface ISyncService
{
    Task<bool> SyncAllDataAsync();
    Task<bool> SyncGeographicDataAsync();
    Task<bool> SyncUserVehiclesAsync();
    Task<bool> IsSyncNeededAsync(TimeSpan maxAge);
    Task SyncInBackgroundAsync();
    Task<SyncStatus> GetSyncStatusAsync();
}

public class SyncStatus
{
    public DateTime? LastSyncTime { get; set; }
    public bool IsSyncing { get; set; }
    public bool HasInternetConnection { get; set; }
    public string? LastError { get; set; }
    public int CachedZones { get; set; }
    public int CachedStreetSegments { get; set; }
    public int CachedVehicles { get; set; }
}