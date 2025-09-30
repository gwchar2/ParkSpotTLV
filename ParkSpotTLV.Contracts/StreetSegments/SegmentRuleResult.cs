using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Contracts.StreetSegments {

    /*
     * Result of evaluating a single segment at a given instant for a given vehicle.
     * 'AvailableUntilUtc' is computed for a boolean includePaid (caller passes showPaid).
     */
    public sealed record SegmentRuleResult(
        SegmentStatus StatusNow,
        DateTimeOffset? NextChangeAtUtc,
        DateTimeOffset? AvailableUntilUtc,
        int MinutesAvailable,
        string Hint
    );
}