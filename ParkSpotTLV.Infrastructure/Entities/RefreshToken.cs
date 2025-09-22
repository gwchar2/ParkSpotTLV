using System;

namespace ParkSpotTLV.Infrastructure.Entities {
    public class RefreshToken {

        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public required string TokenHash { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }     // Set when user logs out or when token is changed.
        public string? ReplacedByTokenHash { get; set; }    // Helps against attacks / malicious attempts with old codes.
        public User User { get; set; } = null!;

    }
}
