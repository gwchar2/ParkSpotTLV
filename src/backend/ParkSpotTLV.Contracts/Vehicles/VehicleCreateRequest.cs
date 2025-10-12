using System.ComponentModel.DataAnnotations;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Contracts.Vehicles {
    /*
     * At least ONE permit must be present:
     * - Either residentZoneCode (int) OR disabledPermit == true (or both).
     * Validation for “zone exists” happens server-side (not via attributes).
    */
    public sealed record VehicleCreateRequest(

        [Required] VehicleType Type,
        [Required] string Name,
        int? ResidentZoneCode,  // Null => no residency permit. If provided, must match an existing Zone.Code.
        bool HasDisabledPermit  // If true => a Disability permit will be created. Defaults to false if omitted.

    );


}
