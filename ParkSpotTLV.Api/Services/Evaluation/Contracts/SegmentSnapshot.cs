using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Contracts {

    /*
     * Needed for evaluation per segment.
     * Produced by ISegmentQueryService (Query Object)
     */
    public sealed record SegmentSnapshot (
        
        Guid SegmentId,
        int? ZoneCode,
        Tariff Tariff, 
        ParkingType ParkingType

    );
    
}
