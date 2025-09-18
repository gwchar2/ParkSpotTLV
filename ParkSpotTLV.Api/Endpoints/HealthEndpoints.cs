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

        
        group.MapGet("/ready", () => {
            return Results.Ok(new { status = "ready" });
        })
        .WithName("Ready")
        .WithSummary("Readiness check")
        .WithDescription("Currently always 200")
        .Produces(StatusCodes.Status200OK)
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