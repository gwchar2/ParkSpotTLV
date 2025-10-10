using ParkSpotTLV.Contracts.Map;

namespace ParkSpotTLV.Contracts.Parking {
    public sealed record StartParkingRequest (

        SegmentResponseDTO Segment,
        Guid VehicleId,
        int? MinParkingTime = 120

        );
    
}
