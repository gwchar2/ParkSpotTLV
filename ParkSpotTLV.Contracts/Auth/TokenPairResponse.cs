

namespace ParkSpotTLV.Contracts.Auth {
    public sealed record TokenPairResponse(

        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt,
        string TokenType = "Bearer"

    );
}
