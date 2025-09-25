using System.ComponentModel.DataAnnotations;
using ParkSpotTLV.Core.Models;

namespace ParkSpotTLV.Contracts.Vehicles {
    /*
     * At least ONE permit must be present:
     * - Either residentZoneCode (int) OR disabledPermit == true (or both).
     * Validation for “zone exists” happens server-side (not via attributes).
    */
    public sealed record VehicleCreateRequest (

        [Required] VehicleType Type,

        [Required] string Name,
        // Null => no residency permit. If provided, must match an existing Zone.Code.
        int? ResidentZoneCode,
        // If true => a Disability permit will be created. Defaults to false if omitted.
        bool HasDisabledPermit
    );

}
