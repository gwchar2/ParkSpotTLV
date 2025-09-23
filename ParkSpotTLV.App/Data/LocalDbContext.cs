using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data;

public class LocalDbContext : DbContext
{
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<LocalUser> Users => Set<LocalUser>();
    public DbSet<LocalZone> Zones => Set<LocalZone>();
    public DbSet<LocalStreetSegment> StreetSegments => Set<LocalStreetSegment>();
    public DbSet<LocalVehicle> Vehicles => Set<LocalVehicle>();
    public DbSet<LocalPermit> Permits => Set<LocalPermit>();
    public DbSet<LocalParkingRule> ParkingRules => Set<LocalParkingRule>();
    public DbSet<LocalRefreshToken> RefreshTokens => Set<LocalRefreshToken>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "parkspot_local.db");
        System.Diagnostics.Debug.WriteLine($"SQLite database path: {dbPath}");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.LogTo(message => System.Diagnostics.Debug.WriteLine($"EF Core: {message}"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all local entity configurations
        modelBuilder.ApplyConfiguration(new Config.LocalUserConfig());
        modelBuilder.ApplyConfiguration(new Config.LocalZoneConfig());
        modelBuilder.ApplyConfiguration(new Config.LocalStreetSegmentConfig());
        modelBuilder.ApplyConfiguration(new Config.LocalVehicleConfig());
        modelBuilder.ApplyConfiguration(new Config.LocalPermitConfig());
        modelBuilder.ApplyConfiguration(new Config.LocalParkingRuleConfig());
        modelBuilder.ApplyConfiguration(new Config.LocalRefreshTokenConfig());

        // Simple configuration for UserPreferences (local-only entity)
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.ToTable("user_preferences");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LastUpdated).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}