
namespace ParkSpotTLV.Contracts.Parking {
    public sealed class StopParkingResponse {
        public Guid SessionId { get; set; }
        public Guid VehicleId { get; set; }
        public DateTimeOffset StartedLocal { get; set; }
        public DateTimeOffset StoppedLocal { get; set; }
        public int TotalMinutes { get; set; }
        public int PaidMinutes { get; set; }
        public int FreeMinutes { get; set; }
        public int FreeMinutesCharged { get; set; }
        public int RemainingBudgetToday { get; set; }

    }
    
}
