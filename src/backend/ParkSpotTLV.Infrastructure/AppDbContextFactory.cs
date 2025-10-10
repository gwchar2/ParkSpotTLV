using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Reflection;

namespace ParkSpotTLV.Infrastructure {
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {
        public AppDbContext CreateDbContext(string[] args) {

            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            conn ??= "Host=localhost;Port=5433;Database=parkspot_dev;Username=admin;Password=admin";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(conn, npgsql => {
                    npgsql.UseNetTopologySuite();
                    npgsql.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                })
                .UseSnakeCaseNamingConvention()
                .Options;

            return new AppDbContext(options);
        }
    }
}
