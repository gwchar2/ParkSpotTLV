using ParkSpotTLV.Contracts.Map;

namespace ParkSpotTLV.Contracts.Parking {
    public sealed record StartParkingRequest (

        SegmentResponseDTO Segment,
        Guid VehicleId,
        int? NotificationMinutes = 30,  // IF VALUE IS < 30 -> WE WILL NOT NOTIFY AT ALL!
        int? MinParkingTime = 120

        );
    
}
