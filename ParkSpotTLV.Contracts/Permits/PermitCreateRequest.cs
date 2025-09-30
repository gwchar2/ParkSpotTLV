using System.ComponentModel.DataAnnotations;
using ParkSpotTLV.Core.Models;

namespace ParkSpotTLV.Contracts.Permits {

    public sealed record PermitCreateRequest(

        [Required] PermitType Type,
        [Required] Guid VehicleId,
        bool HasDisabledPermit,
        int? ResidentZoneCode = null
    );


}
