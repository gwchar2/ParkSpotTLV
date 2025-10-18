

namespace ParkSpotTLV.Contracts.Parking {
    public class SessionStatusResponse {
        public bool Status { get; set; }
        public Guid SessionId { get; set; }
        public string Name { get; set; } = "";
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
    }
}
