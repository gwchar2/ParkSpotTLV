using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.App.Data;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Services;

public class LocalDataService
{
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

    public async Task<Session?> GetSessionAsync()
    {
        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        return existing;
    }

    public async Task AddSessionAsync(Session session)
    {
        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        if (existing is not null)
            context.Session.Remove(existing);

        await context.Session.AddAsync(session);
        await context.SaveChangesAsync();
    }

    public async Task DeleteSessionAsync()
    {
        using var context = new LocalDbContext();
        var existing = await context.Session.FirstOrDefaultAsync();

        if (existing is not null)
            context.Session.Remove(existing);
        await context.SaveChangesAsync();
    }

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