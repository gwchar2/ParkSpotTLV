using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ParkSpotTLV.Api.Middleware {

    /* Adds a global ProblemDetails pipeline
     * All unhandled exceptions are returned in JSON format.
    */
    public static class ProblemDetailsMiddleware {

        public static IApplicationBuilder UseGlobalProblemDetails(this IApplicationBuilder app) {

            var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();

            /* Turning unknown / unhandled exceptions to JSON*/
            app.UseExceptionHandler(errorApp => {
                errorApp.Run(async ctx => {

                    var feature = ctx.Features.Get<IExceptionHandlerFeature>();
                    var ex = feature?.Error;

                    var traceId = Activity.Current?.TraceId.ToString() ?? ctx.TraceIdentifier;
                    var http = ctx.Request;

                    var problem = new ProblemDetails {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "An unexpected error occured.",
                        Type = "https://httpstatuses.com/500",
                        Instance = http.Path
                    };

                    problem.Extensions["traceId"] = traceId;
                    problem.Extensions["method"] = http.Method;
                    problem.Extensions["path"] = http.Path.Value;

                    // If we are in development launch, we can add extra info
                    if (env.IsDevelopment() && ex is not null) {
                        problem.Detail = ex.Message;
                        problem.Extensions["exceptionType"] = ex.GetType().FullName;
                    }

                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    ctx.Response.ContentType = "application/problem+json; charset=utf-8";

                    await ctx.Response.WriteAsJsonAsync(problem, ProblemJsonOptions());
                });
            });

            // Converting regular errors to JSON (4xx, 5xx)
            app.UseStatusCodePages(async ctx => {
                var response = ctx.HttpContext.Response;
                var request = ctx.HttpContext.Request;
                var traceId = Activity.Current?.TraceId.ToString() ?? ctx.HttpContext.TraceIdentifier;

                // If the body of the error already exists, or it is not an error, we return nothing new.
                if (response.HasStarted || response.StatusCode < 400) return;


                var problem = new ProblemDetails {
                    Status = response.StatusCode,
                    Title = response.StatusCode >= 500 ? "An unexpected error occurred." : "Request failed.",
                    Type = response.StatusCode >= 500
                                ? "https://httpstatuses.com/500"
                                : $"https://httpstatuses.com/{response.StatusCode}",
                    Instance = request.Path
                };

                problem.Extensions["traceId"] = traceId;
                problem.Extensions["method"] = request.Method;
                problem.Extensions["path"] = request.Path.Value;

                if (env.IsDevelopment()) {
                    problem.Detail = "See logs by traceId for more context.";
                }

                response.ContentType = "application/problem+json; charset=utf-8";
                await ctx.HttpContext.Response.WriteAsJsonAsync(problem, ProblemJsonOptions());
            });

            return app;
        }


        public static JsonSerializerOptions ProblemJsonOptions() => new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
}
