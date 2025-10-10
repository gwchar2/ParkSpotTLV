

using System.Threading;

namespace ParkSpotTLV.Contracts.Time {

    public sealed class SystemClock : IClock {
        public TimeZoneInfo TZ { get; }

        public SystemClock() {
            TZ = TryGetTz("Asia/Jerusalem") ?? TryGetTz("Israel Standard Time")
                 ?? TimeZoneInfo.Utc;
        }

        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public DateTimeOffset LocalNow => TimeZoneInfo.ConvertTime(UtcNow, TZ);

        public DateTimeOffset ToLocal(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, TZ);
        public DateTimeOffset ToLocal(DateTimeOffset? utc) => TimeZoneInfo.ConvertTime((DateTimeOffset)utc!, TZ);
        public DateTimeOffset ToUtc(DateTimeOffset local) => local.ToUniversalTime();
        public DateTimeOffset? ToUtc(DateTimeOffset? local) => local?.ToUniversalTime();

        private static TimeZoneInfo? TryGetTz(string id) {
            try {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch {
                return null;
            }
        }

    }
}