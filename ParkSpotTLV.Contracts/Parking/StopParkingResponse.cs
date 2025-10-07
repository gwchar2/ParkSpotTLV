
namespace ParkSpotTLV.Contracts.Parking {
    public sealed class StopParkingResponse {
        public Guid SessionId { get; set; }
        public Guid VehicleId { get; set; }
        public DateTimeOffset StartedLocalUtc { get; set; }
        public DateTimeOffset StoppedLocalUtc { get; set; }
        public int TotalMinutes { get; set; }
        public int FreeMinutesCharged { get; set; }
        public int PaidMinutes { get; set; }
        public int RemainingToday { get; set; }

    }
    
}
