using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Infrastructure;

namespace ParkSpotTLV.Api.Endpoints;

public static class HealthEndpoints {
    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder routes) {
       
        var group = routes.MapGroup("/");

        group.MapGet("/health", (RuntimeHealth rh) => {
            var now = DateTimeOffset.UtcNow;
            var uptime = now - rh.StartedAtUtc;

            return Results.Ok(new {
                status = "ok",
                version = rh.Version,
                uptimeSec = (long)uptime.TotalSeconds,
                nowUtc = now
            });
        })
        .WithName("Health")
        .WithSummary("Liveness check")
        .WithDescription("Returns status, version, uptime.")
        .Produces(StatusCodes.Status200OK)
        .WithOpenApi();

        
        group.MapGet("/ready", async (HttpContext ctx, AppDbContext db) => {
            try {
                var ct = ctx.RequestAborted;

                // DB reachable?
                await db.Database.ExecuteSqlRawAsync("SELECT 1", ct);

                // PostGIS present?  Returns '0' or '1'
                var hasPostGis = await db.Database
                    .SqlQueryRaw<string>("SELECT extname FROM pg_extension WHERE extname = 'postgis'")
                    .AnyAsync(ct);

                var body = new { status = hasPostGis ? "green" : "red", database = true, postgis = hasPostGis };
                return hasPostGis
                    ? Results.Ok(body)
                    : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception ex) {
                var body = new { status = "red", database = false, postgis = false, error = ex.Message };
                return Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("Ready")
        .WithSummary("Readiness check (DB + PostGIS)")
        .WithDescription("Returns green only if DB is reachable and PostGIS extension is present.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status503ServiceUnavailable)
        .WithOpenApi();

        
        group.MapGet("/version", (RuntimeHealth rh) => {
            return Results.Ok(new { version = rh.Version });
        })
        .WithName("Version")
        .WithSummary("Version check")
        .WithDescription("Returns the application build version.")
        .Produces(StatusCodes.Status200OK)
        .WithOpenApi();

        return routes;
    }
}