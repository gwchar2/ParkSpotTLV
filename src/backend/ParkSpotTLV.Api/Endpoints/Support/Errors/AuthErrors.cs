using Microsoft.AspNetCore.Http.HttpResults;

namespace ParkSpotTLV.Api.Endpoints.Support.Errors {
    /*
     * Errors for auths specifically
     */
    public static class AuthErrors {
        public static ProblemHttpResult MissingInfo(HttpContext ctx) =>
            GlobalErrorManager.BadRequest("Username and password are required.", ctx);
        public static ProblemHttpResult UsernameTaken(HttpContext ctx) =>
            GlobalErrorManager.Conflict("Username already taken.", ctx);
        public static ProblemHttpResult InvalidCreds(HttpContext ctx) =>
            GlobalErrorManager.UnauthorizedToken("Invalid credentials.", ctx);
        public static ProblemHttpResult AuthentRequired(HttpContext ctx) =>
            GlobalErrorManager.UnauthorizedToken("Authentication required to log out all devices.", ctx);
        public static ProblemHttpResult SingleSession(HttpContext ctx) =>
           GlobalErrorManager.BadRequest("Either set allDevices=true or provide refreshToken.", ctx);
        public static ProblemHttpResult OldInfo(HttpContext ctx) =>
            GlobalErrorManager.BadRequest("Old and new passwords are required.", ctx);
        public static ProblemHttpResult InvalidOldPass(HttpContext ctx) =>
            GlobalErrorManager.BadRequest("Invalid old password.", ctx);
    }
}
