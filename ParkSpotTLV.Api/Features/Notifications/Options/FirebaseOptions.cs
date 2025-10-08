namespace ParkSpotTLV.Api.Features.Notifications.Options {

    public sealed class FirebaseOptions {
        public const string SectionName = "Firebase";

        // "Legacy" or "V1"
        public string Mode { get; set; } = "V1";

        // Your Firebase project ID (e.g., "parkspottlv-fc570")
        public string? ProjectId { get; set; }

        // Nested service-account details (used when Mode == "V1")
        public ServiceAccountOptions ServiceAccount { get; set; } = new();

        // Legacy server key (used only when Mode == "Legacy")
        public string? ServerKey { get; set; }

        // General networking option
        public int HttpTimeoutSeconds { get; set; } = 10;
    }

    public sealed class ServiceAccountOptions {
        // "Path" | "Base64"
        public string Source { get; set; } = "Path";

        // Optional — used when Source == "Path"
        public string? JsonPath { get; set; }

        // Optional — used when Source == "Base64"
        public string? JsonBase64 { get; set; }
    }
}