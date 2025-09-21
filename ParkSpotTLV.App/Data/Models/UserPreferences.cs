using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public class UserPreferences
{
    [Key]
    public int Id { get; set; } = 1;

    public int ParkingThresholdMinutes { get; set; } = 30;

    public bool NotificationsEnabled { get; set; } = true;

    public int NotificationMinutesBefore { get; set; } = 30;

    public bool AutoSyncEnabled { get; set; } = true;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}