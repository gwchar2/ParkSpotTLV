namespace ParkSpotTLV.Api.Services.Evaluation.Contracts {
    /*
     * API Level request used by our facade
     */
    public sealed class MapSegmentsRequest {

        public double MinLon { get; init; }
        public double MaxLon { get; init; }
        public double MinLat { get; init; }
        public double Maxlat { get; init; }
        public DateTimeOffset Now { get; init; } = DateTimeOffset.Now;
        public PermitPov Pov { get; init; } = default!;

        /* Preferences */
        public bool ShowFree { get; init; } = true;
        public bool ShowPaid { get; init; } = true;
        public bool ShowLimited { get; init; } = true;
        public bool ShowAll { get; init; } = false;             // Will also show illegal? NEED TO ASK MICHAL

        /* Minimal time treshold / duration of parking */
        public int LimitedThresholdMinutes { get; init; } = 30;
        public int MinDurationMinutes { get; init; } = 120;
    }
}
