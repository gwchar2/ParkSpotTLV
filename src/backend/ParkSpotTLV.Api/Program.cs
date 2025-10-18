
using Serilog;
using System.Reflection;
using ParkSpotTLV.Api.Composition;
using ParkSpotTLV.Api.Endpoints;
using ParkSpotTLV.Contracts.Time;

/* 
 * BOOTSTRAP LOGGING
 */
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
/*
 * Feature Extension Methods
 */
try {
#if DEBUG
    Log.Information("Starting Up App Build: DEBUG:");
#else
    Log.Information("Starting Up App Build: RELEASE:");
#endif

    var builder = WebApplication.CreateBuilder(args); 
    builder.Services.AddSingleton<IClock, SystemClock>(); 

    // Infrastructure (EF, Serilog host hook, seeding, helpers)
    builder.AddInfrastructure();

    // OpenAPI (document + transformers)
    builder.Services.AddCustomOpenApi();

    // Auth (options, hashing, JWT, refresh)
    builder.Services.AddAuthFeature(builder.Configuration);

    // Map segment evluation service
    builder.Services.AddParking();

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
    app.MapParking();

    app.Run();
}
catch (Exception ex) {
    Log.Fatal(ex, "Application failed to start-up");
}
finally {
    Log.CloseAndFlush();
}

/* 
 * RUNTIME HEALTH
 */
public sealed class RuntimeHealth(IConfiguration cfg) {
    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;
    public string Version { get; } = cfg["App:Version"]
               ?? (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
                  .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
                  .GetName().Version?.ToString()
               ?? "0.0.0-dev";
}
