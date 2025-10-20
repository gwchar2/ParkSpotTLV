using ParkSpotTLV.Infrastructure.Auth.Models;

namespace ParkSpotTLV.Infrastructure.Auth.Services {
    
    /* 
     * Manages refresh tokens (DB-backed):
     *  - Issue(userId) -> create new refresh token
     *  - ValidateAndRotate(raw) -> rotate and issue new JWT + new refresh token
     *  - RevokeByRawToken(raw) / RevokeAllForUser(userId)
     */

    public interface IRefreshTokenService {
        RefreshIssueResult Issue(Guid userId);
        RefreshRotateResult ValidateAndRotate(string rawRefreshToken);
        void RevokeByRawToken(string rawRefreshToken);
        void RevokeAllForUser(Guid userId);
    }

}