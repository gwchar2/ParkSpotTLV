using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Data.Services;

/*
 * LocalDataService - Manages local SQLite database operations for offline-first app experience
 *
 * Responsibilities:
 * - Database initialization and migration handling
 * - User preferences management (parking settings, notifications)
 * - User authentication and session management
 * - Vehicle and permit CRUD operations
 * - Geographic data cache (zones, street segments)
 * - Sync timestamp tracking
 *
 * Design Patterns:
 * - Repository pattern for data access
 * - Unit of Work pattern with using statements
 * - Soft deletes with IsActive flags
 * - Individual property updates to avoid entity tracking issues
 */
public class LocalDataService : ILocalDataService
{
    /*
     * Database initialization - ensures SQLite database exists with proper schema
     * Recovery strategy: If corruption detected, recreate database from scratch
     */
    public async Task InitializeAsync()
    {
        using var context = new LocalDbContext();

        try
        {

            // Create database and tables if they don't exist (idempotent operation)
            var created = await context.Database.EnsureCreatedAsync();

            // Ensure default preferences exist for app functionality
            var preferences = await context.UserPreferences.FirstOrDefaultAsync();
            if (preferences == null)
            {
                await context.UserPreferences.AddAsync(new UserPreferences());
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
                await context.UserPreferences.AddAsync(new UserPreferences());
                await context.SaveChangesAsync();
            }
            catch (Exception )
            {
                throw; // Can't recover - app will need to be reinstalled
            }
        }
    }

    #region User Preferences
    /*
     * User preferences management - app settings and parking configurations
     * Always returns valid preferences object (creates default if none exist)
     */

    // Retrieves current user preferences, returns defaults if none exist
    public async Task<UserPreferences> GetUserPreferencesAsync()
    {
        using var context = new LocalDbContext();
        var preferences = await context.UserPreferences.FirstOrDefaultAsync();
        return preferences ?? new UserPreferences(); // Fallback to defaults
    }

    /*
     * Saves user preferences with individual property updates to avoid EF tracking issues
     * Strategy: Update existing record or create new one if none exists
     */
    public async Task SaveUserPreferencesAsync(UserPreferences preferences)
    {
        using var context = new LocalDbContext();
        preferences.LastUpdated = DateTime.UtcNow;

        var existing = await context.UserPreferences.FirstOrDefaultAsync();
        if (existing != null)
        {
            // Update properties individually to avoid ID conflicts
            existing.ParkingThresholdMinutes = preferences.ParkingThresholdMinutes;
            existing.NotificationsEnabled = preferences.NotificationsEnabled;
            existing.NotificationMinutesBefore = preferences.NotificationMinutesBefore;
            existing.AutoSyncEnabled = preferences.AutoSyncEnabled;
            existing.LastUpdated = preferences.LastUpdated;
            context.UserPreferences.Update(existing);
        }
        else
        {
            // First-time preferences setup
            await context.UserPreferences.AddAsync(preferences);
        }
        await context.SaveChangesAsync();
    }

    #endregion

    #region User Authentication
    /*
     * User authentication and session management
     * Supports single-user login with JWT token persistence
     */

    // Returns currently logged-in user or null if no active session
    public async Task<LocalUser?> GetCurrentUserAsync()
    {
        using var context = new LocalDbContext();
        return await context.Users.FirstOrDefaultAsync(u => u.IsLoggedIn);
    }

    // Saves user data including authentication tokens and session info
    public async Task SaveUserAsync(String token, DateTimeOffset expiresAt)
    {
        using var context = new LocalDbContext();
        var existing = await context.Users.FirstOrDefaultAsync();
        if (existing is not null)
        {
            // Update existing user session
            // context.Entry(existing).CurrentValues.SetValues(token, expiresAt, DateTimeOffset.Now);
        }
        else
        {
            // New user registration/login
            //await context.Users.AddAsync(token, expiresAt, DateTimeOffset.Now);
        }
        await context.SaveChangesAsync();
    }

    // Quick check for active user session
    public async Task<bool> IsUserLoggedInAsync()
    {
        using var context = new LocalDbContext();
        return await context.Users.AnyAsync(u => u.IsLoggedIn);
    }

    // Clears all user sessions and auth tokens (supports multi-user cleanup)
    public async Task LogoutAsync()
    {
        using var context = new LocalDbContext();
        var existing = await context.Users.FirstOrDefaultAsync();
        // if (existing is not null)
        // {
        //     // Update existing user session
        //     // context.Entry(existing).CurrentValues.SetValues(null, null, null);
        // }
        
        await context.SaveChangesAsync();
    }
    #endregion

    #region Vehicle Management
    /*
     * Vehicle and permit management for parking compliance
     * Uses soft deletes to maintain data integrity and sync history
     */

    // Retrieves all active vehicles for user - excludes soft-deleted vehicles
    public async Task<List<LocalVehicle>> GetUserVehiclesAsync(string userId)
    {
        using var context = new LocalDbContext();
        return await context.Vehicles
            .Where(v => v.UserId == userId && v.IsActive)
            .ToListAsync();
    }

    // Saves vehicle data - supports both new vehicles and updates from sync
    public async Task SaveVehicleAsync(LocalVehicle vehicle)
    {
        using var context = new LocalDbContext();
        var existing = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id);
        if (existing != null)
        {
            // Update from sync or user modification
            context.Entry(existing).CurrentValues.SetValues(vehicle);
        }
        else
        {
            // New vehicle registration
            await context.Vehicles.AddAsync(vehicle);
        }
        await context.SaveChangesAsync();
    }

    // Soft delete preserves data for sync and audit trail
    public async Task DeleteVehicleAsync(string vehicleId)
    {
        using var context = new LocalDbContext();
        var vehicle = await context.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicleId);
        if (vehicle != null)
        {
            vehicle.IsActive = false; // Soft delete - preserves relationships
            await context.SaveChangesAsync();
        }
    }
    #endregion

    #region Permit Management

    // Get all active permits for a specific vehicle
    public async Task<List<LocalPermit>> GetVehiclePermitsAsync(string vehicleId)
    {
        using var context = new LocalDbContext();
        return await context.Permits
            .Where(p => p.VehicleId == vehicleId && p.IsActive)
            .ToListAsync();
    }

    // Save or update a permit record
    public async Task SavePermitAsync(LocalPermit permit)
    {
        using var context = new LocalDbContext();
        var existing = await context.Permits.FirstOrDefaultAsync(p => p.Id == permit.Id);
        if (existing != null)
        {
            // Update existing permit properties individually
            existing.Type = permit.Type;
            existing.ZoneCode = permit.ZoneCode;
            existing.ZoneId = permit.ZoneId;
            existing.ValidTo = permit.ValidTo;
            existing.IsActive = permit.IsActive;
            existing.CachedAt = permit.CachedAt;
            context.Permits.Update(existing);
        }
        else
        {
            // Add new permit
            await context.Permits.AddAsync(permit);
        }
        await context.SaveChangesAsync();
    }

    // Soft delete a permit (mark as inactive)
    public async Task DeletePermitAsync(string permitId)
    {
        using var context = new LocalDbContext();
        var permit = await context.Permits.FirstOrDefaultAsync(p => p.Id == permitId);
        if (permit != null)
        {
            permit.IsActive = false; // Soft delete
            await context.SaveChangesAsync();
        }
    }

    // Get all permits for a specific zone
    public async Task<List<LocalPermit>> GetZonePermitsAsync(string zoneId)
    {
        using var context = new LocalDbContext();
        return await context.Permits
            .Where(p => p.ZoneId == zoneId && p.IsActive)
            .ToListAsync();
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