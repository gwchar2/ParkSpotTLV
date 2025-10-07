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
        public DbSet<TariffWindow> TariffWindows => Set<TariffWindow>();
        public DbSet<ParkingDailyBudget> ParkingDailyBudget => Set<ParkingDailyBudget>();
        public DbSet<ParkingSession> ParkingSession => Set<ParkingSession>();
        public DbSet<ParkingNotification> ParkingNotification => Set<ParkingNotification>();
        protected override void OnModelCreating(ModelBuilder model) {
            model.HasPostgresExtension("postgis");
            model.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
