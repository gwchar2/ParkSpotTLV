using Microsoft.AspNetCore.Http.HttpResults;

namespace ParkSpotTLV.Api.Endpoints.Support.Errors {
    public static class PermitErrors {
        public static ProblemHttpResult MaxHit(HttpContext ctx) =>
           GlobalErrorManager.BadRequest("Maximum of 2 permits per vehicle", ctx);
        public static ProblemHttpResult MaxOne(HttpContext ctx) =>
               GlobalErrorManager.Conflict("Vehicle can not have more than 1 permit of same type", ctx);
        public static ProblemHttpResult MissingZoneCode(HttpContext ctx) =>
          GlobalErrorManager.BadRequest("Please add a zone code!", ctx);
        public static ProblemHttpResult ChooseType(HttpContext ctx) =>
          GlobalErrorManager.BadRequest("Please choose type of permit", ctx);
        public static ProblemHttpResult Forbidden(HttpContext ctx) =>
           GlobalErrorManager.Forbidden("Permit not found or not owned by user.", ctx);
        public static ProblemHttpResult MissingZone(HttpContext ctx) =>
          GlobalErrorManager.BadRequest("Zone permit must include a zone.", ctx);
        public static ProblemHttpResult CantRemoveDef(HttpContext ctx) =>
          GlobalErrorManager.BadRequest("Can not remove default permit.", ctx);
        public static ProblemHttpResult NotFound(HttpContext ctx) =>
          GlobalErrorManager.NotFound("Permit not found.", ctx);
    }
}
