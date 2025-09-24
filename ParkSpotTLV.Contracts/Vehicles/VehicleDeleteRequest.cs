
using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Contracts.Vehicles {
    /*
     * VehicleDeleteRequest
     * --------------------
     * Delete requires a RowVersion to prevent deleting a record that was just updated.
    */
    public sealed record VehicleDeleteRequest (
        [Required] string RowVersion
    );
}
