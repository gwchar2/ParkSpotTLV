using ParkSpotTLV.Core.Models;

namespace ParkSpotTLV.Contracts.Vehicles {

    public sealed record VehicleUpdateRequest(

        string RowVersion,
        VehicleType? Type = null,
        string? Name = null

    );

}
