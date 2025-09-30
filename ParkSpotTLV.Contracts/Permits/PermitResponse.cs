using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Contracts.Permits {
    public sealed record PermitResponse {
        public Guid PermitId { get; init; }
        public Guid VehicleId { get; init; }
        public PermitType Type { get; init; }
        public int? ResidentZoneCode { get; init; }            // null => no residency
        public DateTimeOffset? LastUpdated { get; init; }
        public string RowVersion { get; init; } = default!;
    }
}
