
using Serilog;
using System.Reflection;
using ParkSpotTLV.Api.Composition;
using ParkSpotTLV.Api.Endpoints;

/* --------------------------------------------------------------------------
 * BOOTSTRAP LOGGING
 * -------------------------------------------------------------------------- */
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
/*
 * Feature Extension Methods — Each feature exposes small IServiceCollection/IEndpointRouteBuilder extension methods 
 * (e.g., AddAuthFeature(), AddInfrastructure(), MapCustomOpenApi()). 
 * Program.cs becomes a short, readable script that composes features via one-liners. 
 * All detailed registrations, options binding, and endpoint wiring live inside their feature extensions, 
 * keeping concerns local, testable, and easy to evolve.
 */
try {
    Log.Information("Starting Up");

    var builder = WebApplication.CreateBuilder(args);

    // Infrastructure (EF, Serilog host hook, seeding, helpers)
    builder.AddInfrastructure();

    // OpenAPI (document + transformers)
    builder.Services.AddCustomOpenApi();

    // Auth (options, hashing, JWT, refresh)
    builder.Services.AddAuthFeature(builder.Configuration);

    // Map segment services
    builder.Services.AddEvaluation();

    var app = builder.Build();

    // Pipeline
    app.UseAppPipeline();

    // OpenAPI / Scalar
    app.MapCustomOpenApi();

    // Endpoints
    app.MapHealth();
    app.MapAuth();
    app.MapVehicles();
    app.MapPermits();
    app.MapSegments();

    app.Run();
}
catch (Exception ex) {
    Log.Fatal(ex, "Application failed to start-up");
}
finally {
    Log.CloseAndFlush();
}

/* --------------------------------------------------------------------------
 * RUNTIME HEALTH
 * -------------------------------------------------------------------------- */
public sealed class RuntimeHealth(IConfiguration cfg) {
    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;
    public string Version { get; } = cfg["App:Version"]
               ?? (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
                  .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
                  .GetName().Version?.ToString()
               ?? "0.0.0-dev";
}
