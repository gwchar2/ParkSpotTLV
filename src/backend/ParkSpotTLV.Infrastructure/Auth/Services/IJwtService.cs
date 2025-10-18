
namespace ParkSpotTLV.Infrastructure.Auth.Services {
    public readonly record struct JwtIssueResult(string AccessToken, DateTimeOffset ExpiresAtUtc);

    /* 
     * Issues *access tokens* (short-lived JWT) used for API authorization. HMAC-SHA256 for now.
     */
    public interface IJwtService {
        JwtIssueResult IssueAccessToken(Guid userId, string username);
    }
}
