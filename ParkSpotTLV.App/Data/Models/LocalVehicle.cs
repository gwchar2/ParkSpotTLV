using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public class LocalVehicle
{
    [Key]
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string Type { get; set; } = "Car";

    public bool HasResidentPermit { get; set; } = false;

    public int ResidentPermitNumber { get; set; } = 0;

    public bool HasDisabledPermit { get; set; } = false;

    public string? ResidentZoneId { get; set; }

    public DateTime? PermitValidTo { get; set; }

    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}