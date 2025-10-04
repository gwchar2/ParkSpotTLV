using Microsoft.AspNetCore.Http.HttpResults;

namespace ParkSpotTLV.Api.Endpoints.Support {
    public static class GeneralProblems {
        public static ProblemHttpResult InvalidZoneCode(HttpContext ctx) =>
            GlobalProblemManager.BadRequest("Invalid zone code", ctx);
        public static ProblemHttpResult MissingRowVersion(HttpContext ctx) =>
            GlobalProblemManager.BadRequest("Missing RowVersion", ctx);
        public static ProblemHttpResult InvalidRowVersion(HttpContext ctx) =>
            GlobalProblemManager.BadRequest("Invalid RowVersion format", ctx);
        public static ProblemHttpResult ConcurrencyError(HttpContext ctx) =>
            GlobalProblemManager.BadRequest("The value was modified by another request. Reload and try again.", ctx);
        public static ProblemHttpResult MissingRefresh(HttpContext ctx) =>
               GlobalProblemManager.BadRequest("Refresh token is required.", ctx);
        public static ProblemHttpResult ExpiredToken(HttpContext ctx) =>
             GlobalProblemManager.UnauthorizedToken("Invalid or expired refresh token.", ctx);
        public static ProblemHttpResult MissingBody(HttpContext ctx) =>
               GlobalProblemManager.BadRequest("Request body is required.", ctx);
        public static ProblemHttpResult Unexpected(HttpContext ctx) =>
            GlobalProblemManager.Unexpected("Unexpected error while revoking tokens.", ctx);
        public static ProblemHttpResult NotFound(HttpContext ctx) =>
            GlobalProblemManager.NotFound("User not found.", ctx);
    }
    public static class VehicleProblems {
        public static ProblemHttpResult NotFound(HttpContext ctx) =>
            GlobalProblemManager.NotFound("Vehicle not found", ctx);
        public static ProblemHttpResult Forbidden(HttpContext ctx) =>
           GlobalProblemManager.Forbidden("Vehicle not found or not owned by user.", ctx);
        public static ProblemHttpResult NameExists(HttpContext ctx) =>
            GlobalProblemManager.BadRequest("Vehicle already exists with this name.", ctx);
    }

    public static class AuthProblems {
        public static ProblemHttpResult MissingInfo(HttpContext ctx) =>
            GlobalProblemManager.BadRequest("Username and password are required.", ctx);
        public static ProblemHttpResult UsernameTaken(HttpContext ctx) =>
            GlobalProblemManager.Conflict("Username already taken.", ctx);
        public static ProblemHttpResult InvalidCreds(HttpContext ctx) =>
            GlobalProblemManager.UnauthorizedToken("Invalid credentials.", ctx);
        public static ProblemHttpResult AuthentRequired(HttpContext ctx) =>
            GlobalProblemManager.UnauthorizedToken("Authentication required to log out all devices.", ctx);
        public static ProblemHttpResult SingleSession(HttpContext ctx) =>
           GlobalProblemManager.BadRequest("Either set allDevices=true or provide refreshToken.", ctx);
        public static ProblemHttpResult OldInfo(HttpContext ctx) =>
            GlobalProblemManager.BadRequest("Old and new passwords are required.", ctx);
        public static ProblemHttpResult InvalidOldPass(HttpContext ctx) =>
            GlobalProblemManager.BadRequest("Invalid old password.", ctx);
    }

    public static class PermitProblems {
        public static ProblemHttpResult MaxHit(HttpContext ctx) =>
           GlobalProblemManager.BadRequest("Maximum of 2 permits per vehicle", ctx);
        public static ProblemHttpResult MaxOne(HttpContext ctx) =>
               GlobalProblemManager.Conflict("Vehicle can not have more than 1 permit of same type", ctx);
        public static ProblemHttpResult MissingZoneCode(HttpContext ctx) =>
          GlobalProblemManager.BadRequest("Please add a zone code!", ctx);
        public static ProblemHttpResult ChooseType(HttpContext ctx) =>
          GlobalProblemManager.BadRequest("Please choose type of permit", ctx);
        public static ProblemHttpResult Forbidden(HttpContext ctx) =>
           GlobalProblemManager.Forbidden("Permit not found or not owned by user.", ctx);
        public static ProblemHttpResult MissingZone(HttpContext ctx) =>
          GlobalProblemManager.BadRequest("Zone permit must include a zone.", ctx);
        public static ProblemHttpResult CantRemoveDef(HttpContext ctx) =>
          GlobalProblemManager.BadRequest("Can not remove default permit.", ctx);
    }
}
