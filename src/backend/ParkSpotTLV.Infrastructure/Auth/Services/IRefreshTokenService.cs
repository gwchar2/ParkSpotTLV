

namespace ParkSpotTLV.Infrastructure.Auth.Services {
    /* IRefreshTokenService
     * Manages *refresh tokens* (DB-backed):
     *  - Issue(userId) → create new refresh token
     *  - ValidateAndRotate(raw) → rotate and issue new JWT + new refresh token
     *  - RevokeByRawToken(raw) / RevokeAllForUser(userId)
     */

    public readonly record struct RefreshIssueResult(string RefreshToken, DateTimeOffset ExpiresAtUtc);
    public readonly record struct RefreshRotateResult(string AccessToken, DateTimeOffset AccessExpiresAtUtc,
                                                      string RefreshToken, DateTimeOffset RefreshExpiresAtUtc);

    public interface IRefreshTokenService {
        RefreshIssueResult Issue(Guid userId);
        RefreshRotateResult ValidateAndRotate(string rawRefreshToken);
        void RevokeByRawToken(string rawRefreshToken);
        void RevokeAllForUser(Guid userId);
    }

}