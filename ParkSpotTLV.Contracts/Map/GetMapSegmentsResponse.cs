

namespace ParkSpotTLV.Contracts.Map {
    public sealed class GetMapSegmentsResponse {
        public DateTimeOffset Now { get; init; }
        public Guid PermitId { get; init; }
        public int MinParkingTime { get; init; }
        public int Count { get; init; }
        public IReadOnlyList<SegmentResponseDTO> Segments { get; init; } = [];
    }
}
