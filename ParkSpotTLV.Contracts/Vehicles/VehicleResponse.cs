using ParkSpotTLV.Core.Models;

namespace ParkSpotTLV.Contracts.Vehicles {
    /*
 * VehicleResponse
 * ---------------
 * Standard shape returned by GET/POST/PATCH.
 * - rowVersion is an opaque concurrency token (backed by Postgres xmin).
 * - residentZoneCode is null when the user has no residency permit.
 * - disabledPermit tells if a Disability permit is active.
 */
    public sealed record VehicleResponse {
        public Guid Id { get; init; }
        public VehicleType Type { get; init; }               
        public int? ResidentZoneCode { get; init; }            // null => no residency
        public bool DisabledPermit { get; init; }
        public string RowVersion { get; init; } = default!;    // send back on PATCH/DELETE
    }
}
