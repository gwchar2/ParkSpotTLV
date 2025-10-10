namespace ParkSpotTLV.Contracts.Time {
    public interface IClock {
        TimeZoneInfo TZ { get; }
        DateTimeOffset UtcNow { get; }
        DateTimeOffset LocalNow { get; }
        DateTimeOffset ToLocal(DateTimeOffset utc);
        DateTimeOffset ToLocal(DateTimeOffset? utc);
        DateTimeOffset ToUtc(DateTimeOffset local);
        DateTimeOffset? ToUtc(DateTimeOffset? nextChange);
    }
}