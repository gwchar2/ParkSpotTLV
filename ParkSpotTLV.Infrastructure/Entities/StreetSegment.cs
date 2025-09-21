using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Infrastructure.Entities {
    // stays as-is
    public enum ParkingType { Unknown = 0, Free = 1, Paid = 2, Limited = 3 }
    public enum ParkingHours { Unknown = 0, SpecificHours = 1 }

    // new: which curb side the rule applies to
    public enum SegmentSide { Both = 0, Left = 1, Right = 2 }

    public class StreetSegment {
        
        public Guid Id { get; set; } = Guid.NewGuid();

        [MaxLength(128)]
        public string? Name { get; set; }

        // Geometry of ONE segment between intersections.
        [Required]
        public LineString Geom { get; set; } = default!;

        // What zone does this street belong to? (kept)
        public Guid? ZoneId { get; set; }

        public Zone? Zone { get; set; }

        public bool CarsOnly { get; set; } = false;

        public ParkingType ParkingType { get; set; } = ParkingType.Unknown;
        public ParkingHours ParkingHours { get; set; } = ParkingHours.Unknown;


        // Segment graph endpoints (for clean splitting/merging and future routing)
        public Guid? FromNodeId { get; set; }
        public Guid? ToNodeId { get; set; }

        // Which curb side the parking info applies to (coloring often differs by side)
        public SegmentSide Side { get; set; } = SegmentSide.Both;

        // Time-based rules (optional; keep simple now, grow later)
        public ICollection<ParkingRule> ParkingRules { get; set; } = new List<ParkingRule>();

        // Last time any rule/status affecting this segment was updated
        public DateTimeOffset? LastUpdated { get; set; }
    }

    // Minimal time-window rule so you can color “correctly right now” if you choose
    public class ParkingRule {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StreetSegmentId { get; set; }
        public StreetSegment StreetSegment { get; set; } = default!;

        // Day-of-week window (0=Sunday .. 6=Saturday to match Israel defaults; adjust if you prefer ISO-8601)
        public int DayOfWeek { get; set; } // 0..6

        // Local time window (no date). Use TimeOnly in .NET 6+/EF Core; string "HH:mm" if you prefer.
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        // When multiple rules overlap, higher priority wins (lower number = higher priority)
        public int StylePriority { get; set; } = 100;

        // Resulting classification for this window
        public ParkingType ParkingType { get; set; } = ParkingType.Unknown;

        // Optional constraints
        public int? MaxDurationMinutes { get; set; } = (-1);

        // Free-text note shown in UI/tooltips if needed
        [MaxLength(256)]
        public string? Note { get; set; }
    }
}


