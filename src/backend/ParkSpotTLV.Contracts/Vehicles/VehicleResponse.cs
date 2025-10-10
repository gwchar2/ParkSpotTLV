

namespace ParkSpotTLV.Contracts.Vehicles {
    public sealed record VehicleResponse {
        public Guid Id { get; init; }
        public required string Type { get; init; }
        public required string Name { get; init; }
        public Guid? ResidencyPermitId { get; init; }
        public int? ResidentZoneCode { get; init; }            // null => no residency
        public Guid? DisabilityPermitId { get; init; }
        public bool DisabledPermit { get; init; }
        public Guid? DefaultPermitId { get; init; }
        public string RowVersion { get; init; } = default!;    // send back on PATCH/DELETE

    }
}
