
using System.Security.Claims;

namespace ParkSpotTLV.Api.Endpoints.Support {
    public static class Guards {

        public static bool TryGetUserId(HttpContext ctx, out Guid userId, out IResult? problem) {
            problem = null;

            var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out userId)) {
                problem = GlobalProblemManager.UnauthorizedToken("Invalid or expired token.", ctx);
                return false;
            }
            return true;
        }

        public static bool TryBase64ToUInt32(string? base64, out uint expectedXmin) {
            expectedXmin = default;

            if (string.IsNullOrWhiteSpace(base64))
                return false;

            try {
                expectedXmin = BitConverter.ToUInt32(Convert.FromBase64String(base64));
                return true;
            }
            catch {
                return false;

            }
        }
    }
}