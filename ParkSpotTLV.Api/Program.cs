using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ParkSpotTLV.Api.Endpoints;
using ParkSpotTLV.Api.Errors;
using ParkSpotTLV.Api.Http;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Security;      // Argon2PasswordHasher (your existing namespace)
using ParkSpotTLV.Core.Auth;                   // IPasswordHasher, AuthOptions
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;
using System.Text;

/* --------------------------------------------------------------------------
 * BOOTSTRAP LOGGING
 * -------------------------------------------------------------------------- */
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try {
    Log.Information("Starting Up");

    /* ----------------------------------------------------------------------
     * BUILDER
     * ---------------------------------------------------------------------- */
    var builder = WebApplication.CreateBuilder(args);

    /* ----------------------------------------------------------------------
     * DATABASE (EF Core + Npgsql + PostGIS)
     * ---------------------------------------------------------------------- */
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing connection string.");
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(conn, x => {
            x.UseNetTopologySuite();
            x.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
        })
        .UseSnakeCaseNamingConvention()
    );

    /* ----------------------------------------------------------------------
     * SERILOG HOST HOOK
     * ---------------------------------------------------------------------- */
    builder.Host.UseSerilog((ctx, services, cfg) => {
        cfg.ReadFrom.Configuration(ctx.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .Enrich.WithEnvironmentName()
          .Enrich.WithMachineName()
          .Enrich.WithProcessId()
          .Enrich.WithThreadId();
    });

    /* ----------------------------------------------------------------------
     * PLATFORM / HOSTED SERVICES / UTILITIES
     * ---------------------------------------------------------------------- */
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();

    // Seeding (enabled via appsettings.Development.json)
    builder.Services.Configure<ParkSpotTLV.Infrastructure.Seeding.SeedOptions>(
        builder.Configuration.GetSection("Seeding"));
    builder.Services.AddHostedService<ParkSpotTLV.Infrastructure.Seeding.SeedRunner>();

    // Runtime helpers
    builder.Services.AddSingleton<RuntimeHealth>();
    builder.Services.AddSingleton(TimeProvider.System);

    /* ----------------------------------------------------------------------
     * AUTH OPTIONS (SINGLE SOURCE OF TRUTH)
     * ---------------------------------------------------------------------- */
    builder.Services.AddOptions<AuthOptions>()
        .Bind(builder.Configuration.GetSection("Auth"))
        .ValidateDataAnnotations()
        .Validate(o => o.Signing.Type == "HMAC"
                       ? !string.IsNullOrWhiteSpace(o.Signing.HmacSecret)
                       : true,
                  "HMAC Selected but Auth:Signing:HmacSecret is missing!");

    /* ----------------------------------------------------------------------
     * PASSWORD HASHING (Argon2id)
     * ---------------------------------------------------------------------- */
    builder.Services.Configure<Argon2Options>(builder.Configuration.GetSection("Auth:Argon2"));
    builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

    /* ----------------------------------------------------------------------
     * AUTHENTICATION (JWT Bearer) + AUTHORIZATION
     * ---------------------------------------------------------------------- */
    var authOpts = builder.Configuration.GetSection("Auth").Get<AuthOptions>()!;
    if (authOpts.Signing.Type.Equals("HMAC", StringComparison.OrdinalIgnoreCase)) {
        var keyBytes = Encoding.UTF8.GetBytes(authOpts.Signing.HmacSecret!);
        var signingKey = new SymmetricSecurityKey(keyBytes);

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
        throw new NotSupportedException("ONLY HMAC IS WIRED AT THE MOMENT - IMPLEMENT RSA AT A LATER TIME!");
    }
    builder.Services.AddAuthorization();

    /* ----------------------------------------------------------------------
     * TOKEN SERVICES (JWT + Refresh) — use AuthOptions directly
     * ---------------------------------------------------------------------- */
    builder.Services.AddScoped<EfRefreshTokenStore>(); // keep if used elsewhere
    builder.Services.AddSingleton<IJwtService, JwtService>();
    builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();

    /* ----------------------------------------------------------------------
     * BUILD APP
     * ---------------------------------------------------------------------- */
    var app = builder.Build();

    /* ----------------------------------------------------------------------
     * MIDDLEWARE PIPELINE
     * ---------------------------------------------------------------------- */
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseGlobalProblemDetails();               // problem+json for errors
    app.UseMiddleware<TracingMiddleware>();      // W3C trace + response headers
    app.UseMiddleware<RequestLoggingMiddleware>(); // request logs (no bodies)

    /* ----------------------------------------------------------------------
     * OPENAPI / SCALAR UI
     * ---------------------------------------------------------------------- */
    app.MapOpenApi();
    app.MapScalarApiReference(options => {
        options.Title = "ParkSpotTLV API";
        options.Theme = ScalarTheme.BluePlanet;
        options.DarkMode = true;
    });

    /* ----------------------------------------------------------------------
     * ENDPOINTS
     * ---------------------------------------------------------------------- */
    app.MapHealth();

    /* ----------------------------------------------------------------------
     * RUN
     * ---------------------------------------------------------------------- */
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
public sealed class RuntimeHealth {
    public DateTimeOffset StartedAtUtc { get; }
    public string Version { get; }

    public RuntimeHealth(IConfiguration cfg) {
        Version = cfg["App:Version"]
               ?? (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
                  .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly())
                  .GetName().Version?.ToString()
               ?? "0.0.0-dev";

        StartedAtUtc = DateTimeOffset.UtcNow;
    }
}
