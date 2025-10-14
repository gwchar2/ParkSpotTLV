using Microsoft.AspNetCore.Http.HttpResults;

namespace ParkSpotTLV.Api.Endpoints.Support.Errors {
    public static class SessionErrors {
        public static ProblemHttpResult Exists(HttpContext ctx) =>
          GlobalErrorManager.Conflict("Active session exists for this vehicle.", ctx);
        public static ProblemHttpResult Unavailable(HttpContext ctx) =>
         GlobalErrorManager.BadRequest("Street is unavailable for parking.", ctx);
        public static ProblemHttpResult NotFound(HttpContext ctx) =>
        GlobalErrorManager.NotFound("No active parking session found.", ctx);
        public static ProblemHttpResult InvalidStart(HttpContext ctx) =>
         GlobalErrorManager.BadRequest("Session has no start time.", ctx);
        public static ProblemHttpResult InvalidStop(HttpContext ctx) =>
         GlobalErrorManager.BadRequest("Session already stopped.", ctx);
    }
}
