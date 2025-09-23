using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public enum PermitType { Default = 0, Disability = 1, ZoneResident = 2 }

public class LocalPermit
{
    [Key]
    public string Id { get; set; } = string.Empty;

    // Foreign key for Vehicle relationship (local convenience for querying)
    public string VehicleId { get; set; } = string.Empty;
    public LocalVehicle? Vehicle { get; set; }

    public PermitType Type { get; set; } = PermitType.Default;

    public int? ZoneCode { get; set; }

    // Foreign key for Zone relationship (local convenience for querying)
    public string? ZoneId { get; set; }
    public LocalZone? Zone { get; set; }

    public DateOnly? ValidTo { get; set; }

    // Local cache management properties
    public bool IsActive { get; set; } = true;
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
}