using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.App.Data.Models;
using ParkSpotTLV.App.Data;

namespace ParkSpotTLV.App.Services;

// 
public class LocalDataService
{
    /*
     * Database initialization - ensures SQLite database exists with proper schema
     * Recovery strategy: If corruption detected, recreate database from scratch
     */
    public async Task InitializeAsync(){
        using var context = new LocalDbContext();

        try
        {
            // Create database and tables if they don't exist (idempotent operation)
            var created = await context.Database.EnsureCreatedAsync();

            // Ensure default preferences exist for app functionality
            var session = await context.Session.FirstOrDefaultAsync();
            if (session is null)
            {
                await context.Session.AddAsync(new Session());
                await context.SaveChangesAsync();
            }
        }
        catch (Exception )
        {
            // Recovery strategy: Recreate corrupted database
            try
            {
                await context.Database.EnsureDeletedAsync(); // Nuclear option
                await context.Database.EnsureCreatedAsync();

                // Restore essential data
                await context.Session.AddAsync(new Session());
                await context.SaveChangesAsync();
            }
            catch (Exception )
            {
                throw; // Can't recover - app will need to be reinstalled
            }
        }
    }

    public async Task AddSessionAsync(Session session) {

        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        if (existing is not null) 
            context.Session.Remove(existing);
        
        await context.Session.AddAsync(session);
        await context.SaveChangesAsync();
    }

    public async Task DeleteSessionAsync() {

        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        if (existing is not null) 
            context.Session.Remove(existing);
        await context.SaveChangesAsync();
    }

    public async Task UpdatePreferencesAsync(int? minParkingTime = null,
                                        bool? showFree = null,
                                        bool? showPaid = null,
                                        bool? showRestricted = null,
                                        bool? showNoParking = null,
                                        string? lastPickedCarId = null) {

        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        if (existing is null)
        return;

        // Only update if a new value was provided
        if (minParkingTime.HasValue)
            existing. MinParkingTime = minParkingTime.Value;

        if (showFree.HasValue)
            existing.ShowFree = showFree.Value;

        if (showPaid.HasValue)
            existing.ShowPaid = showPaid.Value;

        if (showRestricted.HasValue)
            existing.ShowRestricted = showRestricted.Value;

        if (showNoParking.HasValue)
            existing.ShowNoParking = showNoParking.Value;

        if (lastPickedCarId is not null)
            existing.LastPickedCarId = lastPickedCarId;

        existing.LastUpdated = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task UpdateTokenAsync(String token, DateTimeOffset expiresAt) {

        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        if (existing is null)
        return;

        existing.RefreshToken = token;
        existing.TokenExpiresAt = expiresAt;
        existing.LastUpdated = DateTimeOffset.Now;

        await context.SaveChangesAsync();
    }

    public async Task<Session?> GetSessionAsync() {

        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        return existing;
    }

}