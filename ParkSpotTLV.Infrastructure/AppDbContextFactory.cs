using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Reflection;

namespace ParkSpotTLV.Infrastructure {
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext> {
        public AppDbContext CreateDbContext(string[] args) {

            var conn = Environment.GetEnvironmentVariable("DB_CONNECTION");
            conn ??= "Host=localhost;Port=5432;Database=parkspot_dev;Username=admin;Password=admin";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(conn, npgsql => {
                    npgsql.UseNetTopologySuite();
                    npgsql.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                })
                .Options;

            return new AppDbContext(options);
        }
    }
}
