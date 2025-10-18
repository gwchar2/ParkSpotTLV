using Microsoft.AspNetCore.Http.HttpResults;

namespace ParkSpotTLV.Api.Endpoints.Support.Errors {
    /*
     * Errors for vehicles specifically
     */
    public static class VehicleErrors {
        public static ProblemHttpResult NotFound(HttpContext ctx) =>
            GlobalErrorManager.NotFound("Vehicle not found", ctx);
        public static ProblemHttpResult Forbidden(HttpContext ctx) =>
           GlobalErrorManager.Forbidden("Vehicle not found or not owned by user.", ctx);
        public static ProblemHttpResult NameExists(HttpContext ctx) =>
            GlobalErrorManager.BadRequest("Vehicle already exists with this name.", ctx);
        public static ProblemHttpResult CantRemove(HttpContext ctx) =>
            GlobalErrorManager.BadRequest("Profile must have at least one vehicle.", ctx);
        public static ProblemHttpResult NoActiveSession(HttpContext ctx) =>
            GlobalErrorManager.NotFound("Active session not found.", ctx);
        public static ProblemHttpResult ForbiddenId(HttpContext ctx) =>
            GlobalErrorManager.NotFound("Vehicle ID is required.", ctx);
    }
}
