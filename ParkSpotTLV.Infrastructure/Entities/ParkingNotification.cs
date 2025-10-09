

namespace ParkSpotTLV.Infrastructure.Entities {
    public class ParkingNotification {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public DateTimeOffset NotifyAt { get; set; }
        public int NotificationMinutes { get; set; }


        public bool IsSent { get; set; } = false;   // IsSent = false -> Notification is scheduled but hasn’t been delivered yet, true -> The notification was actually sent to the user.
        public DateTimeOffset CreatedAt { get; set; }


        public string Status { get; set; } = "Pending";              // Pending / Sent / Failed / Canceled
        public string? JobId { get; set; }                           // Scheduler handle for cancel/reschedule
        public int AttemptCount { get; set; } = 0;
        public DateTimeOffset? LastAttemptAt { get; set; }
        public string? Error { get; set; }                           // Last error string
        public string? ProviderMessageId { get; set; }              

        public Guid? TokenIdSnapshot { get; set; }                   // Which device we targeted

    }
}
