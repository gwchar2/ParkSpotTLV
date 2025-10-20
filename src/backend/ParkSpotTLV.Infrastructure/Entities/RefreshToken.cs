

using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Infrastructure.Entities {
    /* RefreshToken
     * Client gets a 256-bit random refresh token (base64url).
     * Server stores TokenHash = HMACSHA256(rawToken, AUTH_HMAC_SECRET)
     * Database entity for refresh tokens:
     *  - Store *only* HMAC-SHA256 hex (TokenHash), never the raw token
     *  - ReplacedByTokenHash links rotation chain for reuse detection
     *  - RevokedAtUtc marks revocation (logout/rotation/security)
     */
    public class RefreshToken {

        /* 
         * Ownership
         */
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }

        /*
         * Concurrency 
         */
        public required string TokenHash { get; set; }
        [Required] public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public DateTimeOffset? RevokedAtUtc { get; set; }       // Set when user logs out or when token is changed.
        public string? ReplacedByTokenHash { get; set; }        // Replace history - improved security.
        public User User { get; set; } = null!;

    }
}
