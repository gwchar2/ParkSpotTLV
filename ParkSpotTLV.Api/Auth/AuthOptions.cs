using System.ComponentModel.DataAnnotations;


namespace ParkSpotTLV.Api.Auth
{
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
            public string? HmacSecret { get; set; }
        }


    }
}
