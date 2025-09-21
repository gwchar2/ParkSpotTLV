using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Services;

// Service for managing local SQLite database operations
// Handles user preferences, authentication, vehicles, and cached geographic data
public class LocalDataService : ILocalDataService
{
    // Initialize the local database and create default preferences if needed
    public async Task InitializeAsync()
    {
        using var context = new LocalDbContext();

        try
        {
            // Try to create database/tables if they don't exist
            await context.Database.EnsureCreatedAsync();

            // Create default preferences if none exist
            var preferences = await context.UserPreferences.FirstOrDefaultAsync();
            if (preferences == null)
            {
                await context.UserPreferences.AddAsync(new UserPreferences());
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex}");

            // If tables are missing or corrupted, recreate the database
            try
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();

                // Create default preferences
                await context.UserPreferences.AddAsync(new UserPreferences());
                await context.SaveChangesAsync();
            }
            catch (Exception ex2)
            {
                System.Diagnostics.Debug.WriteLine($"Database recreation failed: {ex2}");
                throw;
            }
        }
    }

    #region User Preferences
    // Get user preferences (parking thresholds, notifications, etc.)
    public async Task<UserPreferences> GetUserPreferencesAsync()
    {
        using var context = new LocalDbContext();
        var preferences = await context.UserPreferences.FirstOrDefaultAsync();
        return preferences ?? new UserPreferences();
    }

    // Save user preferences to local database
    public async Task SaveUserPreferencesAsync(UserPreferences preferences)
    {
        using var context = new LocalDbContext();
        preferences.LastUpdated = DateTime.UtcNow;

        var existing = await context.UserPreferences.FirstOrDefaultAsync();
        if (existing != null)
        {
            // Update existing preferences
            context.Entry(existing).CurrentValues.SetValues(preferences);
        }
        else
        {
            // Create new preferences record
            await context.UserPreferences.AddAsync(preferences);
        }
        await context.SaveChangesAsync();
    }

    #endregion

    #region User Authentication
    // Get the currently logged-in user
    public async Task<LocalUser?> GetCurrentUserAsync()
    {
        using var context = new LocalDbContext();
        return await context.Users.FirstOrDefaultAsync(u => u.IsLoggedIn);
    }

    // Save or update user information and authentication tokens
    public async Task SaveUserAsync(LocalUser user)
    {
        using var context = new LocalDbContext();
        var existing = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (existing != null)
        {
            // Update existing user
            context.Entry(existing).CurrentValues.SetValues(user);
        }
        else
        {
            // Add new user
            await context.Users.AddAsync(user);
        }
        await context.SaveChangesAsync();
    }

    // Check if any user is currently logged in
    public async Task<bool> IsUserLoggedInAsync()
    {
        using var context = new LocalDbContext();
        return await context.Users.AnyAsync(u => u.IsLoggedIn);
    }

    // Log out all users and clear authentication tokens
    public async Task LogoutAsync()
    {
        using var context = new LocalDbContext();
        var users = await context.Users.Where(u => u.IsLoggedIn).ToListAsync();
        foreach (var user in users)
        {
            user.IsLoggedIn = false;
            user.AuthToken = null;
            user.RefreshToken = null;
        }
        await context.SaveChangesAsync();
    }
    #endregion

    #region Vehicle Management

    // Get all active vehicles for a specific user
    public async Task<List<LocalVehicle>> GetUserVehiclesAsync(string userId)
    {
        using var context = new LocalDbContext();
        return await context.Vehicles
            .Where(v => v.UserId == userId && v.IsActive)
            .ToListAsync();
    }

    // Save or update a vehicle record
    public async Task SaveVehicleAsync(LocalVehicle vehicle)
    {
        using var context = new LocalDbContext();
        var existing = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id);
        if (existing != null)
        {
            // Update existing vehicle
            context.Entry(existing).CurrentValues.SetValues(vehicle);
        }
        else
        {
            // Add new vehicle
            await context.Vehicles.AddAsync(vehicle);
        }
        await context.SaveChangesAsync();
    }

    // Soft delete a vehicle (mark as inactive)
    public async Task DeleteVehicleAsync(string vehicleId)
    {
        using var context = new LocalDbContext();
        var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle != null)
        {
            vehicle.IsActive = false; // Soft delete
            await context.SaveChangesAsync();
        }
    }
    #endregion

    #region Geographic Data Cache
    // Get all active parking zones, ordered by zone code
    public async Task<List<LocalZone>> GetZonesAsync()
    {
        using var context = new LocalDbContext();
        return await context.Zones
            .Where(z => z.IsActive)
            .OrderBy(z => z.Code)
            .ToListAsync();
    }

    // Cache zones from server data
    public async Task SaveZonesAsync(List<LocalZone> zones)
    {
        using var context = new LocalDbContext();
        foreach (var zone in zones)
        {
            var existing = await context.Zones.FirstOrDefaultAsync(z => z.Id == zone.Id);
            if (existing != null)
            {
                // Update existing zone
                context.Entry(existing).CurrentValues.SetValues(zone);
            }
            else
            {
                // Add new zone
                await context.Zones.AddAsync(zone);
            }
        }
        await context.SaveChangesAsync();
    }

    // Get street segments, optionally filtered by zone
    public async Task<List<LocalStreetSegment>> GetStreetSegmentsAsync(string? zoneId = null)
    {
        using var context = new LocalDbContext();
        var query = context.StreetSegments.Where(s => s.IsActive);

        if (!string.IsNullOrEmpty(zoneId))
        {
            query = query.Where(s => s.ZoneId == zoneId);
        }

        return await query.ToListAsync();
    }

    // Cache street segments from server data
    public async Task SaveStreetSegmentsAsync(List<LocalStreetSegment> segments)
    {
        using var context = new LocalDbContext();
        foreach (var segment in segments)
        {
            var existing = await context.StreetSegments.FirstOrDefaultAsync(s => s.Id == segment.Id);
            if (existing != null)
            {
                // Update existing segment
                context.Entry(existing).CurrentValues.SetValues(segment);
            }
            else
            {
                // Add new segment
                await context.StreetSegments.AddAsync(segment);
            }
        }
        await context.SaveChangesAsync();
    }
    #endregion

    #region Cache Management

    // Clear all cached geographic and vehicle data (keep user preferences and auth)
    public async Task ClearCacheAsync()
    {
        using var context = new LocalDbContext();
        context.Zones.RemoveRange(context.Zones);
        context.StreetSegments.RemoveRange(context.StreetSegments);
        context.Vehicles.RemoveRange(context.Vehicles);
        await context.SaveChangesAsync();
    }

    // Get the last time data was synced from the server
    public async Task<DateTime?> GetLastSyncTimeAsync()
    {
        using var context = new LocalDbContext();
        var user = await GetCurrentUserAsync();
        return user?.LastSyncAt;
    }

    // Update the last sync timestamp for the current user
    public async Task UpdateLastSyncTimeAsync()
    {
        using var context = new LocalDbContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.IsLoggedIn);
        if (user != null)
        {
            user.LastSyncAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
    #endregion
}