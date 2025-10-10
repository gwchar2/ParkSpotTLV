using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Api.Features.Parking.Services;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Time;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;
using System.Diagnostics;

namespace ParkSpotTLV.Api.Features.AutoStop{

    public sealed class AutoStopParkingService(IServiceProvider sp, ILogger<AutoStopParkingService> log, IClock clock) : BackgroundService {
        private readonly IServiceProvider _sp = sp;
        private readonly ILogger<AutoStopParkingService> _log = log;
        private readonly IClock _clock = clock;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

            while (await timer.WaitForNextTickAsync(stoppingToken)) {
                try {
                    await StopDueSessionsOnce(stoppingToken);
                }
                catch (Exception) {
                    _log.LogError("Waiting for next tick.");
                }
            }
        }

        private async Task StopDueSessionsOnce(CancellationToken ct) {

            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var budget = scope.ServiceProvider.GetRequiredService<IDailyBudgetService>(); 
            var nowUtc = _clock.UtcNow;

            var affected = await db.ParkingSession
                .Where(s =>
                    s.StoppedUtc == null &&
                    s.Status == ParkingSessionStatus.Active &&
                    s.PlannedEndUtc <= nowUtc)
                .ToListAsync(ct);


            if (affected.Count == 0) {
                _log.LogDebug("Auto-stop tick: no due sessions (nowUtc={NowUtc})", nowUtc);
                return;
            }

            foreach (ParkingSession session in affected) {
                var outcome = await budget.CalculateAsync(session, ct);
                session.ParkingBudgetUsed += outcome.FreeMinutesCharged;
                session.PaidMinutes += outcome.PaidMinutes;
                session.StoppedUtc = session.PlannedEndUtc;
                session.Status = ParkingSessionStatus.AutoStopped;
                session.UpdatedAtUtc = nowUtc;
            }
            await db.SaveChangesAsync(ct);
            var ids = affected.Select(s => s.Id).ToArray();
            _log.LogInformation(
                "Auto-stopped {Count} sessions at {NowUtc}. IDs={Ids}",
                affected.Count, nowUtc, ids);

        }
    }
}