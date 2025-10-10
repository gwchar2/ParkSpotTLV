
namespace ParkSpotTLV.Contracts.Parking {
    public class StartParkingResponse {
        public required string NameEnglish { get; set; }
        public required string NameHebrew { get; set; }
        public int? ZoneCode { get; set; }
        public required string Group { get; set; }
        public required string Tariff { get; set; }
        public int? FreeBudgetRemaining { get; set; }
        public DateTimeOffset? SessionStarted { get; set; }
        public DateTimeOffset? SessionEnding { get; set; }
        public Guid SegmentId { get; set; }
        public Guid SessionId { get; set; }
        public Guid VehicleId { get; set; }

        };
    
}