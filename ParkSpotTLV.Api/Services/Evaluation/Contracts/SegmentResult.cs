using NetTopologySuite.Geometries;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Contracts {

    /*
     * Final result emitted by IMapSegmentsEvaluator (Per segment)
     */
    public sealed record SegmentResult (

        Guid SegmentId,
        int? ZoneCode,
        Tariff Tariff,                                      // Tariff for parking
        string? NameEnglish,
        string? NameHebrew,
        string Group, //"FREE" -> Free the entire duration /  "PAID" -> Paid some time during the duration / "LIMITED" -> Turns to restricted / "RESTRICTED" -> Always restricted
        string Reason,                                      // Reason for grouping
        ParkingType ParkingType,
        bool IsPayNow,                                      // True if parking costs money at this moment
        bool IsPaylater,                                    // True if segment will costs money at any time during parking
        DateTimeOffset? AvailableFrom,
        DateTimeOffset? AvailableUntil,
        DateTimeOffset? NextChange,
        int? FreeBudgetRemaining,                           // Amount of free parking remaining at the moment. If parking is overnight, this will show zero.
        LineString Geom

    );
}