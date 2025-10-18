using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Infrastructure;
using Serilog;

namespace ParkSpotTLV.Api.Composition {
    /* 
     * Feature Extensions Methods for the map segments evaluation feature.
     * Initiated by program.cs 
     */
    public static class InfrastructureExtensions {

        // Configure EF, Serilog hook, seed runner, helpers, endpoints explorer, OpenAPI.
        public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder) {
            // Database (EF Core + Npgsql + PostGIS)
            var conn = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing connection string.");

            builder.Services.AddDbContext<AppDbContext>(opt =>
                opt.UseNpgsql(conn, x => {
                    x.UseNetTopologySuite();
                    x.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
                })
                .UseSnakeCaseNamingConvention()
            );

            // Serilog
            builder.Host.UseSerilog((ctx, services, cfg) => {
                cfg.ReadFrom.Configuration(ctx.Configuration)
                  .ReadFrom.Services(services)
                  .Enrich.FromLogContext()
                  .Enrich.WithEnvironmentName()
                  .Enrich.WithMachineName()
                  .Enrich.WithProcessId()
                  .Enrich.WithThreadId();
            });

            // Seeding (enabled via appsettings.Development.json)
            builder.Services.AddHostedService<Infrastructure.Seeding.SeedRunner>();
            builder.Services.Configure<Infrastructure.Seeding.SeedOptions>(
                builder.Configuration.GetSection("Seeding"));

            // Runtime helpers
            builder.Services.AddSingleton<RuntimeHealth>();
            builder.Services.AddSingleton(TimeProvider.System);

            // Minimal API metadata
            builder.Services.AddEndpointsApiExplorer();

            return builder;
        }
    }
}