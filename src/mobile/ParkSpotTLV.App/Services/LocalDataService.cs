using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.App.Data;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Services;

/*
* Manages local SQLite database operations for user sessions and preferences.
* Handles session storage, token updates, and user preference persistence.
*/
public class LocalDataService
{
    /*
    * Initializes the local database. Creates database and default session if needed.
    * Recreates database from scratch if initialization fails.
    */
    public async Task InitializeAsync()
    {
        using var context = new LocalDbContext();

        try
        {
            var created = await context.Database.EnsureCreatedAsync();

            var session = await context.Session.FirstOrDefaultAsync();
            if (session is null)
            {
                await context.Session.AddAsync(new Session());
                await context.SaveChangesAsync();
            }
        }
        catch (Exception)
        {
            try
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();

                await context.Session.AddAsync(new Session());
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }

    /*
    * Retrieves the current user session from local database.
    * Returns the session if it exists, null otherwise.
    */
    public async Task<Session?> GetSessionAsync()
    {
        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        return existing;
    }

    /*
    * Adds or replaces the user session in local database.
    * Removes existing session before adding new one.
    */
    public async Task AddSessionAsync(Session session)
    {
        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        if (existing is not null)
            context.Session.Remove(existing);

        await context.Session.AddAsync(session);
        await context.SaveChangesAsync();
    }

    /*
    * Deletes the current user session from local database.
    * Used during logout to clear stored authentication data.
    */
    public async Task DeleteSessionAsync()
    {
        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        if (existing is not null)
            context.Session.Remove(existing);
        await context.SaveChangesAsync();
    }

    /*
    * Updates the refresh token and expiration time in the current session.
    * Called after successful token refresh to store new credentials.
    */
    public async Task UpdateTokenAsync(string token, DateTimeOffset expiresAt)
    {
        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        if (existing is null)
            return;

        existing.RefreshToken = token;
        existing.TokenExpiresAt = expiresAt;
        existing.LastUpdated = DateTimeOffset.Now;

        await context.SaveChangesAsync();
    }

    /*
    * Updates user preferences in the current session.
    * Allows partial updates - only specified parameters are updated.
    */
    public async Task UpdatePreferencesAsync(int? minParkingTime = null,
                                        bool? showFree = null,
                                        bool? showPaid = null,
                                        bool? showRestricted = null,
                                        bool? showNoParking = null,
                                        string? lastPickedCarId = null)
    {
        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        if (existing is null)
            return;

        if (minParkingTime.HasValue)
            existing.MinParkingTime = minParkingTime.Value;

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
}