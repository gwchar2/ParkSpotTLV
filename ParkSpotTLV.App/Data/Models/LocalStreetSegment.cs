using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public class LocalStreetSegment
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Name { get; set; }

    [Required]
    public string GeometryJson { get; set; } = string.Empty;

    public string? ZoneId { get; set; }

    public string ParkingType { get; set; } = "Unknown";

    public string ParkingHours { get; set; } = "Unknown";

    public string Side { get; set; } = "Both";

    public double? LengthMeters { get; set; }

    public int StylePriority { get; set; } = 100;

    public DateTime? LastUpdated { get; set; }

    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}