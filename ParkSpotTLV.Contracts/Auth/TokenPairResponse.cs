

namespace ParkSpotTLV.Contracts.Auth {
    /* Standard token pair returned by auth endpoints. */
    public sealed record TokenPairResponse(

        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        string TokenType = "Bearer"

    );
}
