

namespace ParkSpotTLV.Contracts.Parking {
    public sealed record StopParkingRequest(
        Guid SessionId,
        Guid VehicleId
    );

}
