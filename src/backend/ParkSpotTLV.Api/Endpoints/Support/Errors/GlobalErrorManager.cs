using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ParkSpotTLV.Api.Endpoints.Support.Errors {

    public static class GlobalErrorManager {

        public static ProblemHttpResult BadRequest(string title, HttpContext? ctx = null) =>
            (ProblemHttpResult)Results.Problem(
                title: title,
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://httpstatuses.com/400",
                instance: ctx?.Request.Path);
        public static ProblemHttpResult UnauthorizedToken(string title, HttpContext? ctx = null) =>
            (ProblemHttpResult)Results.Problem(
                title: title,
                statusCode: StatusCodes.Status401Unauthorized,
                type: "https://httpstatuses.com/401");

        public static ProblemHttpResult Forbidden(string title, HttpContext? ctx = null) =>
           (ProblemHttpResult)Results.Problem(
               title: title,
               statusCode: StatusCodes.Status403Forbidden,
               type: "https://httpstatuses.com/403",
               instance: ctx?.Request.Path);

        public static ProblemHttpResult NotFound(string title, HttpContext? ctx = null) =>
            (ProblemHttpResult)Results.Problem(
                title: title,
                statusCode: StatusCodes.Status404NotFound,
                type: "https://httpstatuses.com/404",
                instance: ctx?.Request.Path);
        public static ProblemHttpResult Conflict(string title, HttpContext? ctx = null) =>
            (ProblemHttpResult)Results.Problem(
                title: title,
                statusCode: StatusCodes.Status409Conflict,
                type: "https://httpstatuses.com/409",
                instance: ctx?.Request.Path);
        public static ProblemHttpResult Unexpected(string title, HttpContext? ctx = null) =>
            (ProblemHttpResult)Results.Problem(
                title: title,
                statusCode: StatusCodes.Status500InternalServerError,
                type: "https://httpstatuses.com/500",
                instance: ctx?.Request.Path);
    }
}
