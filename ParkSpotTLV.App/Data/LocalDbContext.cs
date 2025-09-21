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
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<LocalUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
        });
        

        modelBuilder.Entity<LocalZone>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code);
        });

        modelBuilder.Entity<LocalStreetSegment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ZoneId);
        });

        modelBuilder.Entity<LocalVehicle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
        });

        base.OnModelCreating(modelBuilder);
    }
}