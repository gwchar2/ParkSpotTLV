using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Infrastructure.Entities {

    /*
     * StreetSegmentRuleWindow: optional per-segment overrides, used rarely.
     * Use for:
     *  - Absolute bans (Forbidden all day / certain hours)
     *  - Special paid windows different from the zone’s tariff group
     * Precedence: Forbidden (any) > segment Paid > tariff Paid > Free-by-default.
     */
    public class StreetSegmentRuleWindow {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StreetSegmentId { get; set; }
        public StreetSegment StreetSegment { get; set; } = default!;
        public SegmentWindowKind Kind { get; set; } = SegmentWindowKind.Forbidden;
        public DaysOfWeekMask Days { get; set; } = DaysOfWeekMask.All;          // Which days this override applies to
        public bool IsAllDay { get; set; } = false;     // Time range in local TZ (ignored if IsAllDay=true). If End < Start => crosses midnight.
        public TimeOnly? StartLocalTime { get; set; }
        public TimeOnly? EndLocalTime { get; set; }
        public SegmentSide AppliesToSide { get; set; } = SegmentSide.Both;      // Apply on left/right/both sides, matching your StreetSegment.Side
        public int Priority { get; set; } = 100; // overrides beat tariff windows by default
        public bool Enabled { get; set; } = true;
        public string? Note { get; set; }
    }
}