using System.ComponentModel.DataAnnotations;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Contracts.Permits {

    public sealed record PermitCreateRequest(

        [Required] PermitType Type,
        [Required] Guid VehicleId,
        bool HasDisabledPermit = false,
        int? ResidentZoneCode = null
    );


}
