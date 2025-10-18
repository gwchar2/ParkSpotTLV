namespace ParkSpotTLV.Api.Features.Parking.Models {
    /*
     * DTO from request to evaluation
     */
    public sealed class MapSegmentsRequestDto {

        public double MinLon { get; init; }
        public double MaxLon { get; init; }
        public double MinLat { get; init; }
        public double MaxLat { get; init; }
        public double CenterLon { get; init; }
        public double CenterLat { get; init; }
        public DateTimeOffset Now { get; init; } = DateTimeOffset.Now;
        public PermitSnapshotDto Pov { get; init; } = default!;
        /* Minimal time treshold / duration of parking */
        public int MinParkingTime { get; init; } = 60;
    }
}
