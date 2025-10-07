using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Map;
using static System.Net.Mime.MediaTypeNames;

namespace ParkSpotTLV.Contracts.Parking {
    public class StartParkingResponse {
        public string NameEnglish { get; set; }
        public string NameHebrew { get; set; }
        public int? ZoneCode { get; set; }
        public string Group { get; set; }
        public string Tariff { get; set; }
        public int? FreeBudgetRemaining { get; set; }
        public DateTimeOffset? SessionStarted { get; set; }
        public DateTimeOffset? SessionEnding { get; set; }
        public DateTimeOffset? NotifyAt { get; set; }
        public Guid SegmentId { get; set; }
        public Guid SessionId { get; set; }
        public Guid VehicleId { get; set; }
        public Guid? NotificationId { get; set; }

        };
    
}