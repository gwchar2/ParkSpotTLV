using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data;

/*
* Local database context for SQLite database operations.
* Manages local storage of user sessions and app data.
*/
public class LocalDbContext : DbContext
{
    public DbSet<Session> Session => Set<Session>();

    /*
    * Configures the database context to use SQLite with the local app data directory.
    */
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "parkspot_local.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        optionsBuilder.EnableSensitiveDataLogging();
    }


}