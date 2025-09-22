using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ParkSpotTLV.Api.Endpoints;
using ParkSpotTLV.Api.Errors;
using ParkSpotTLV.Api.Http;
using ParkSpotTLV.Api.Auth;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Security;
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;
using System.Text;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try {
    Log.Information("Starting Up");

    var builder = WebApplication.CreateBuilder(args);


    /* Adds a DbContext & Connects to DB (secret connection) */
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("Missing connection string.");
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(conn, x => {
            x.UseNetTopologySuite();
            x.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
        })
        .UseSnakeCaseNamingConvention()
        );

    /* Serilog host hook (reads appsettings) */
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
    /* Logging Services */
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();

    /* Seeding Services (enabled in Development via appsettings.Development.json) */
    builder.Services.Configure<ParkSpotTLV.Infrastructure.Seeding.SeedOptions>(
        builder.Configuration.GetSection("Seeding"));
    builder.Services.AddHostedService<ParkSpotTLV.Infrastructure.Seeding.SeedRunner>();

    /* On Start (RunTime threads) */
    builder.Services.AddSingleton<RuntimeHealth>();

    /* API Authentication Service */
    builder.Services.AddOptions<AuthOptions>()
        .Bind(builder.Configuration.GetSection("Auth"))
        .ValidateDataAnnotations()
        .Validate(o => o.Signing.Type == "HMAC" ? !string.IsNullOrWhiteSpace(o.Signing.HmacSecret) : true,
                                                    "HMAC Selected but Auth:Signing:HmacSecret is missing!");

    var authOpts = builder.Configuration.GetSection("Auth").Get<AuthOptions>()!;
    if (authOpts.Signing.Type.Equals("HMAC", StringComparison.OrdinalIgnoreCase)) {
        var keyBytes = Encoding.UTF8.GetBytes(authOpts.Signing.HmacSecret!);            // Encodes the HMAC key
        var signingKey = new SymmetricSecurityKey(keyBytes);                            // Creates a symmetric key from the encoded hmac

        /* Registers the how of validating tokens (JWT bearer scheme). */
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = true,
                    ValidIssuer = authOpts.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOpts.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    RequireSignedTokens = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(authOpts.ClockSkewMinutes)
                };
            });
    } else {
        throw new NotSupportedException("ONLY HMAC IS WIRED AT THE MOMENT");
    }
    builder.Services.AddAuthorization();                    // Registers the policies and [Authorize] system.
    builder.Services.AddScoped<EfRefreshTokenStore>();      // Registers the entity framework for handling refresh tokens
    builder.Services.AddSingleton(TimeProvider.System);     


    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();

    
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
    public string Version { get; }
    public RuntimeHealth(IConfiguration cfg) {
        Version = cfg["App:Version"]
               ?? (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
                  .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
                  .GetName().Version?.ToString()
               ?? "0.0.0-dev";
    }
}