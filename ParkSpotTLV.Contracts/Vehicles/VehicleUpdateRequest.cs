using ParkSpotTLV.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Contracts.Vehicles { 
/*
 * VehicleUpdateRequest
 * --------------------
 * Partial update. A valid RowVersion is REQUIRED for concurrency.
 * Any of the fields may be provided; omitted fields are left unchanged.
 * After applying changes, the vehicle MUST STILL HAVE >= 1 effective permit.
 *
 * Notes:
 * - ResidentZoneCode is nullable: set null explicitly to REMOVE residency.
 * - DisabledPermit is nullable: true/false to change, null to leave unchanged.
 */
    public sealed record VehicleUpdateRequest (

        string RowVersion,
        VehicleType? Type,
        string? Name,
        int? ResidentZoneCode,         
        bool? DisabledPermit     
        
    );

}
