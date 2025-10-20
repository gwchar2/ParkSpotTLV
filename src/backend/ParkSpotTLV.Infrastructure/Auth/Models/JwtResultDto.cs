

namespace ParkSpotTLV.Infrastructure.Auth.Models {
    public readonly record struct JwtIssueResult(
        string AccessToken,
        DateTimeOffset ExpiresAtUtc
        );
}
