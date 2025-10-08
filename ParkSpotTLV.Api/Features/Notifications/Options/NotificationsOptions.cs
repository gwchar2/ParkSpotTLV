namespace ParkSpotTLV.Api.Features.Notifications.Options {
    public sealed class NotificationsOptions {
        public const string SectionName = "Notifications";
        public bool SuggestCategory { get; set; } = false;      
        public int MaxRetryCount { get; set; } = 3;
        public int MaxErrorLength { get; set; } = 2000;        
    }
}
