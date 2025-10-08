using System;

namespace ParkSpotTLV.Infrastructure.Entities {
    public class DevicePushToken {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Platform { get; set; } = default!;
        public string Token { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastSeen { get; set; }            // The last time the app confirmed this token
        public bool IsRevoked { get; set; }

    }
}
