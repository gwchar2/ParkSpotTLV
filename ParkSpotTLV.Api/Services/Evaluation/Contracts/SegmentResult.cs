using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Contracts {

    /*
     * Final result emitted by IMapSegmentsEvaluator (Per segment)
     */
    public sealed record SegmentResult (

        Guid SegmentId,
        int? ZoneCode,
        Tariff Tariff,
        ParkingType ParkingType,
        string Group,                   // Free / Paid / Limited / Illegal (cant park)
        string Reason,
        bool IsLegalNow,
        DateTimeOffset? AvailableFrom,
        DateTimeOffset? AvailableUntil,
        DateTimeOffset? NextChange,
        bool PriceNow,                      // True if segment costs money at the moment
        int? FreeBudgetRemaining
    );
}
