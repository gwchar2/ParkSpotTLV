using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Reflection;

namespace ParkSpotTLV.Infrastructure {
    /*
     * Configures the database connection environment, npgsql for net topology, and snake case naming for database variables
     */
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {
        public AppDbContext CreateDbContext(string[] args) {

            var conn = "Host=localhost;Port=5433;Database=parkspot_dev;Username=admin;Password=admin";

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
