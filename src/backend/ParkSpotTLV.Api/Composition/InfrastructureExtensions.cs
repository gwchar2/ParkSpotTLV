using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Infrastructure;
using Serilog;

namespace ParkSpotTLV.Api.Composition {
    /* ----------------------------------------------------------------------
    * ---------------------------------------------------------------------- */

    public static class InfrastructureExtensions {
        // Configure EF, Serilog host hook, hosted seed runner, helpers, endpoints explorer, OpenAPI scaffolding hook.
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

            // Serilog host hook
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

            // Minimal API metadata & OpenAPI doc scaffolding
            builder.Services.AddEndpointsApiExplorer();

            return builder;
        }
    }
}