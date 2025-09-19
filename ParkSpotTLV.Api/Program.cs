using System.Reflection;
using Scalar.AspNetCore;
using Serilog;
using ParkSpotTLV.Api.Errors;
using ParkSpotTLV.Api.Http;
using ParkSpotTLV.Api.Endpoints;
using ParkSpotTLV.Infrastructure;
using Microsoft.EntityFrameworkCore;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try { 
    Log.Information("Starting Up");

    var builder = WebApplication.CreateBuilder(args);

    // Connects to DB (secret connection)
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing connection string.");
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(conn, x => { 
            x.UseNetTopologySuite(); 
            x.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name); 
        }));

    

    // Serilog host hook (reads appsettings)
    builder.Host.UseSerilog((ctx, services, cfg) => {
        cfg.ReadFrom.Configuration(ctx.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .Enrich.WithEnvironmentName()
          .Enrich.WithMachineName()
          .Enrich.WithProcessId()
          .Enrich.WithThreadId();
    });

    /* Services */
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    /* Seeding Services (enabled in Development via appsettings.Development.json) */
    builder.Services.Configure<ParkSpotTLV.Infrastructure.Seeding.SeedOptions>(
        builder.Configuration.GetSection("Seeding"));
    builder.Services.AddHostedService<ParkSpotTLV.Infrastructure.Seeding.SeedRunner>();

    /* Runtime start */
    builder.Services.AddSingleton<RuntimeHealth>();

    var app = builder.Build();


    /* Pipeline */
    app.UseGlobalProblemDetails();                          // problem+json for errors
    app.UseMiddleware<TracingMiddleware>();                 // W3C trace + response headers
    app.UseMiddleware<RequestLoggingMiddleware>();          // request logs (no bodies)

    /* OpenAPI / Scalar UI */
    app.MapOpenApi();
    app.MapScalarApiReference(options => {
        options.Title = "ParkSpotTLV API";
        options.Theme = ScalarTheme.BluePlanet;
        options.DarkMode = true;
    });

    /* EndPoints */
    app.MapHealth();

    app.Run();
}
catch (Exception ex) {
    Log.Fatal(ex, "Application failed to start-up");
} finally {
    Log.CloseAndFlush();
}

/* Helper class that returns starting time + version (from csproj) */
public sealed class RuntimeHealth {
    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;
    public string Version { get; } =
        (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
        .GetName().Version?.ToString() ?? "0.0.0";
}