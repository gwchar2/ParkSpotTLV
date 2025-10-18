using System.ComponentModel.DataAnnotations;


namespace ParkSpotTLV.Infrastructure.Auth.Models {

    /* 
     * Reads auth options configuration from api settings file
     */
    public sealed class AuthOptions {

        [Required] public string Issuer { get; set; } = default!;

        [Required] public string Audience { get; set; } = default!;

        public int AccessTokenMinutes { get; init; } = 10;

        public int RefreshTokenDays { get; init; } = 14;

        public int ClockSkewMinutes { get; init; } = 0;

        public SigningOptions Signing { get; set; } = new();

        public sealed class SigningOptions {
            [Required] public string Type { get; set; } = "HMAC";   // HMAC for development, RSA later.
            public string HmacSecret { get; set; } = "";                // Loaded from user secrets
        }


    }
}
