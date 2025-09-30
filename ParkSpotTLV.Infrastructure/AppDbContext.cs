using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Infrastructure {

    /*
     *  Configures the builder pattern for different entities in the table. 
    */
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
        public DbSet<User> Users => Set<User>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Zone> Zones => Set<Zone>();
        public DbSet<Permit> Permits => Set<Permit>();    
        public DbSet<StreetSegment> StreetSegments => Set<StreetSegment>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<StreetSegmentRuleWindow> StreetSegmentRuleWindows => Set<StreetSegmentRuleWindow>();
        public DbSet<TariffGroupWindow> TariffGroupWindows => Set<TariffGroupWindow>();

        protected override void OnModelCreating(ModelBuilder model) {
            model.HasPostgresExtension("postgis");
            model.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
