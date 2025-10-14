using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Contracts.Time;

namespace ParkSpotTLV.Api.Endpoints {

    public static class HealthEndpoints {
        public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder routes) {

            var group = routes.MapGroup("/");
            /* HEALTH REQUEST 
             * Accepts: Nothing
             * Returns: 
             *      200 with { status, version, uptimeSec, nowutc } if API is live.
             */
            group.MapGet("/health", 
                (RuntimeHealth rh, IClock clock) => {
                var now = DateTimeOffset.UtcNow;
                var uptime = now - rh.StartedAtUtc;

                return Results.Ok(new {
                    status = "ok",
                    version = rh.Version,
                    uptimeSec = (long)uptime.TotalSeconds,
                    nowLocal = clock.LocalNow
                });
            })
            .WithName("Health")
            .WithSummary("Liveness check")
            .WithDescription("Returns status, version, uptime.")
            .Produces(StatusCodes.Status200OK)
            .WithOpenApi();

            /* READY REQUEST 
             * Accepts: Nothing
             * Returns: 
             *      200 with { status, database_status, postgis_status } if DB is live.
             *      503 if DB is offline.
             */
            group.MapGet("/ready", 
                async (HttpContext ctx, AppDbContext db) => {
                try {
                    var ct = ctx.RequestAborted;

                    // DB reachable?
                    await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);

                    // PostGIS present?  Returns '0' or '1'
                    var hasPostGis = await db.Database
                        .SqlQueryRaw<string>("SELECT extname FROM pg_extension WHERE extname = 'postgis'")
                        .AnyAsync(ct);

                    var resp = new { 
                        status = hasPostGis ? "green" : "red", 
                        database = true, 
                        postgis = hasPostGis 
                    };
                    return hasPostGis
                        ? Results.Ok(resp)
                        : Results.Json(
                            resp, 
                            statusCode: StatusCodes.Status503ServiceUnavailable
                            );
                }
                catch (Exception ex) {
                    var resp = new { 
                        status = "red", 
                        database = false, 
                        postgis = false, 
                        error = ex.Message 
                    };
                    return Results.Json(
                        resp, 
                        statusCode: StatusCodes.Status503ServiceUnavailable
                        );
                }
            })
            .WithName("Ready")
            .WithSummary("Readiness check (DB + PostGIS)")
            .WithDescription("Returns green only if DB is reachable and PostGIS extension is present.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status503ServiceUnavailable)
            .WithOpenApi();

            /* VERSION REQUEST 
             * Accepts: Nothing
             * Returns: 
             *      200 with { version } 
             */
            group.MapGet("/version", (RuntimeHealth rh) => {
                return Results.Ok(new { 
                    version = rh.Version 
                });
            })
            .WithName("Version")
            .WithSummary("Version check")
            .WithDescription("Returns the application build version.")
            .Produces(StatusCodes.Status200OK)
            .WithOpenApi();

            return routes;
        }
    }
}