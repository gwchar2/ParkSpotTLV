using System.ComponentModel.DataAnnotations;


namespace ParkSpotTLV.Infrastructure.Auth.Models {

    /* AuthOptions
     * Central configuration for *both* JWT (access tokens) and Refresh tokens.
     * Keep secrets in user-secrets; this class only reads values.
     *
     * Sections:
     *  - Issuer/Audience  → put into JWT claims/validation
     *  - AccessTokenMinutes → how long access tokens live
     *  - RefreshTokenDays  → how long refresh tokens live
     *  - ClockSkewMinutes  → leeway for validator (token lifetime)
     *  - Signing.HmacSecret → HMAC key for both JWT signing & refresh token hashing
     */
    public sealed class AuthOptions {

        [Required] public string Issuer { get; set; } = default!;

        [Required] public string Audience { get; set; } = default!;

        public int AccessTokenMinutes { get; init; } = 10;

        public int RefreshTokenDays { get; init; } = 14;

        public int ClockSkewMinutes { get; init; } = 0;

        public SigningOptions Signing { get; set; } = new();

        public sealed class SigningOptions {

            // HMAC for development, RSA later.
            [Required] public string Type { get; set; } = "HMAC";

            // Only used when Type = HMAC.
            public string HmacSecret { get; set; } = "";                // Loaded from user secrets
        }


    }
}
