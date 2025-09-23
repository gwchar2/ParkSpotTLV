using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public enum ParkingType { Unknown = 0, Free = 1, Paid = 2, Limited = 3 }
public enum ParkingHours { Unknown = 0, SpecificHours = 1 }
public enum SegmentSide { Both = 0, Left = 1, Right = 2 }

public class LocalStreetSegment
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? Name { get; set; }

    [Required]
    public string GeometryJson { get; set; } = string.Empty;

    public string? ZoneId { get; set; }

    public LocalZone? Zone { get; set; }

    public bool CarsOnly { get; set; } = false;

    public ParkingType ParkingType { get; set; } = ParkingType.Unknown;

    public ParkingHours ParkingHours { get; set; } = ParkingHours.Unknown;

    public SegmentSide Side { get; set; } = SegmentSide.Both;

    public double? LengthMeters { get; set; }

    public int StylePriority { get; set; } = 100;

    public ICollection<LocalParkingRule> ParkingRules { get; set; } = new List<LocalParkingRule>();

    public DateTimeOffset? LastUpdated { get; set; }

    // Local cache management properties
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}