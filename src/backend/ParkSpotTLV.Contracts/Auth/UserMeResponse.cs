

namespace ParkSpotTLV.Contracts.Auth {
    public sealed record UserMeResponse(

        Guid Id, 
        string Username, 
        int VehiclesCount

        );
}
