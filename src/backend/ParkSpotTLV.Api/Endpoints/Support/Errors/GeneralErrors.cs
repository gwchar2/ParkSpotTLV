using Microsoft.AspNetCore.Http.HttpResults;

namespace ParkSpotTLV.Api.Endpoints.Support.Errors {
    /*
     * General error cases
     */
    public static class GeneralErrors {
        public static ProblemHttpResult InvalidZoneCode(HttpContext ctx) =>
            GlobalErrorManager.BadRequest("Invalid zone code", ctx);
        public static ProblemHttpResult InvalidorMissingRowVersion(HttpContext ctx) =>
            GlobalErrorManager.BadRequest("Invalid or missing RowVersion format", ctx);
        public static ProblemHttpResult ConcurrencyError(HttpContext ctx) =>
            GlobalErrorManager.BadRequest("The value was modified by another request. Reload and try again.", ctx);
        public static ProblemHttpResult MissingRefresh(HttpContext ctx) =>
               GlobalErrorManager.BadRequest("Refresh token is required.", ctx);
        public static ProblemHttpResult ExpiredToken(HttpContext ctx) =>
             GlobalErrorManager.UnauthorizedToken("Invalid or expired refresh token.", ctx);
        public static ProblemHttpResult MissingBody(HttpContext ctx) =>
               GlobalErrorManager.BadRequest("Request body is required.", ctx);
        public static ProblemHttpResult Unexpected(HttpContext ctx) =>
            GlobalErrorManager.Unexpected("Unexpected error while revoking tokens.", ctx);
        public static ProblemHttpResult NotFound(HttpContext ctx) =>
            GlobalErrorManager.NotFound("User not found.", ctx);
    }
}
