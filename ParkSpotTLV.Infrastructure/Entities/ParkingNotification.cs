

namespace ParkSpotTLV.Infrastructure.Entities {
    public class ParkingNotification {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public DateTimeOffset NotifyAt { get; set; }
        public int NotificationMinutes { get; set; }
        public bool IsSent { get; set; } = false;   // IsSent = false -> Notification is scheduled but hasn’t been delivered yet, true -> The notification was actually sent to the user.
        public DateTimeOffset? SentAt {get; set;}
        public DateTimeOffset CreatedAt { get; set; }
    }
}
