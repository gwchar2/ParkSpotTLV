using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using ParkSpotTLV.Api.Endpoints;
using ParkSpotTLV.Api.Http;
using ParkSpotTLV.Api.Middleware;
using ParkSpotTLV.Core.Auth;                   
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Auth;
using ParkSpotTLV.Infrastructure.Security;      
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
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
    builder.Services.AddCustomOpenApi();
    builder.Services.AddEndpointsApiExplorer();

    // Seeding (enabled via appsettings.Development.json)
    builder.Services.AddHostedService<ParkSpotTLV.Infrastructure.Seeding.SeedRunner>();
    builder.Services.Configure<ParkSpotTLV.Infrastructure.Seeding.SeedOptions>(
        builder.Configuration.GetSection("Seeding"));

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
                    ClockSkew = TimeSpan.FromMinutes(authOpts.ClockSkewMinutes),
                    NameClaimType = JwtRegisteredClaimNames.Sub             // "sub" claim inside the JWT is the string representation of the user’s Guid. This maps the JWT "sub" claim onto ClaimTypes.NameIdentifier
                };
            });
    } else {
        throw new NotSupportedException("ONLY HMAC IS WIRED AT THE MOMENT - IMPLEMENT RSA AT A LATER TIME!");
    }
    builder.Services.AddAuthorization();

    /* ----------------------------------------------------------------------
     * TOKEN SERVICES (JWT + Refresh) — use AuthOptions directly
     * ---------------------------------------------------------------------- */
    builder.Services.AddScoped<EfRefreshTokenStore>(); 
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
    app.MapAuth();
    app.MapVehicles();
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



/* --------------------------------------------------------------------------
 * OpenAPI Extensions
 * -------------------------------------------------------------------------- */
public static class OpenApiExtensions {
    public static IServiceCollection AddCustomOpenApi(this IServiceCollection services) {
        services.AddOpenApi(options => {
            // Add a global Bearer security scheme
            options.AddDocumentTransformer((document, context, cancellationToken) => {
                document.Components ??= new();
                document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter: {your JWT}"
                };

                return Task.CompletedTask;
            });

            // Auto-apply Bearer requirement for authorized endpoints
            options.AddOperationTransformer((operation, context, cancellationToken) => {
                var requiresAuth = context.Description?.ActionDescriptor?.EndpointMetadata?
                    .OfType<IAuthorizeData>()?.Any() == true;

                if (requiresAuth) {
                    operation.Security ??= new List<OpenApiSecurityRequirement>();
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}