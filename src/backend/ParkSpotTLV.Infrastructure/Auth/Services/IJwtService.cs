
namespace ParkSpotTLV.Infrastructure.Auth.Services {
    public readonly record struct JwtIssueResult(string AccessToken, DateTimeOffset ExpiresAtUtc);

    /* IJwtService
     * Issues *access tokens* (short-lived JWT) used for API authorization.
     * Your implementation is HMAC-SHA256 for now.
     */
    public interface IJwtService {
        JwtIssueResult IssueAccessToken(Guid userId, string username);
    }
}
