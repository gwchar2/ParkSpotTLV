namespace ParkSpotTLV.Api.Features.Notifications.Options {
    public sealed class HangfireOptions {
        public const string SectionName = "Hangfire";
        public string? ConnectionString { get; set; }           
        public bool UseMainDbConnection { get; set; } = true;   
    }
}
