using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public class Session
{

    [Key] 
    public int Id { get; set; }

    // Prefernces
    public int MinParkingTime { get; set; } = 30;

    public bool NotificationsEnabled { get; set; } = true;

    public int NotificationMinutesBefore { get; set; } = 30;

    public bool ShowFree {get; set; } = true ;
    public bool ShowPaid {get; set; } = true ;
    public bool ShowRestricted {get; set; } = true ;
    public bool ShowNoParking {get; set; } = true ;

    public string? LastPickedCarId { get; set; }

    // public bool IsParking { get; set; } = false;

    // User Auth
    public string UserName { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public DateTimeOffset TokenExpiresAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;



}