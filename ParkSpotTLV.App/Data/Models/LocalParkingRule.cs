using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public class LocalParkingRule
{
    [Key]
    public string Id { get; set; } = string.Empty;

    public string StreetSegmentId { get; set; } = string.Empty;
    public LocalStreetSegment StreetSegment { get; set; } = default!;

    public int DayOfWeek { get; set; } // 0..6

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int StylePriority { get; set; } = 2;

    public ParkingType ParkingType { get; set; } = ParkingType.Unknown;

    public int? MaxDurationMinutes { get; set; } = (-1);

    [MaxLength(256)]
    public string? Note { get; set; }
    
    // Local cache management properties
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}