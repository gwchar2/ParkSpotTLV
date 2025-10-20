using ParkSpotTLV.Infrastructure.Auth.Models;

namespace ParkSpotTLV.Infrastructure.Auth.Services {

    /* 
     * Issues *access tokens* (short-lived JWT) used for API authorization. HMAC-SHA256 for now.
     */
    public interface IJwtService {
        JwtIssueResult IssueAccessToken(Guid userId, string username);
    }
}
