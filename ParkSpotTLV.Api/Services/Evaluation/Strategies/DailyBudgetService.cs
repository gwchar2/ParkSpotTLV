// Application/Services/VehicleDailyBudgetService.cs
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Contracts.Budget;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Api.Services.Evaluation.Strategies {

    public sealed class DailyBudgetService(AppDbContext db) : IDailyBudgetService {

        private const int DailyAllowanceMinutes = 120;

        /*
         * We ensure that the daily allownace was reset, it only creates the day row if missing.
         */
        public async Task EnsureResetAsync(Guid vehicleId, DateOnly anchorDate, CancellationToken ct) {

            var exists = await db.ParkingDailyBudget.AnyAsync(x => x.VehicleId == vehicleId && x.AnchorDate == anchorDate, ct);

            if (!exists) {
                db.ParkingDailyBudget.Add(new ParkingDailyBudget {
                    VehicleId = vehicleId,
                    AnchorDate = anchorDate,
                    MinutesUsed = 0,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });

                await db.SaveChangesAsync(ct);

            }
        }

        /*
         * Gets the remaining minutes
         */
        public async Task<int> GetRemainingMinutesAsync(Guid vehicleId, DateOnly anchorDate, CancellationToken ct) {
            var row = await db.ParkingDailyBudget
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.VehicleId == vehicleId && x.AnchorDate == anchorDate, ct);

            var used = row?.MinutesUsed ?? 0;
            var remaining = DailyAllowanceMinutes - used;
            return remaining > 0 ? remaining : 0;
        }

        /*
         * Consumes a certain amount of minutes from the daily budget
         */
        public async Task ConsumeAsync(Guid vehicleId, DateTimeOffset startLocal, DateTimeOffset endLocal, CancellationToken ct) {
            if (endLocal <= startLocal) return;

            // Split over the 08:00 boundary if needed
            var startAnchor = ParkingBudgetTimeHandler.AnchorDateFor(startLocal);
            var endAnchor = ParkingBudgetTimeHandler.AnchorDateFor(endLocal);

            if (startAnchor == endAnchor) {
                var minutes = (int)Math.Ceiling((endLocal - startLocal).TotalMinutes);
                await AddMinutesAsync(vehicleId, startAnchor, minutes, ct);
                return;
            }

            // First leg: start -> boundary
            var boundaryLocal = new DateTime(
                startAnchor.Year, startAnchor.Month, startAnchor.Day,
                ParkingBudgetTimeHandler.ResetTime.Hour, ParkingBudgetTimeHandler.ResetTime.Minute, 0,
                DateTimeKind.Unspecified).AddDays(1);
            var boundary = new DateTimeOffset(boundaryLocal, endLocal.Offset); // keep local offset
            var firstLeg = (int)Math.Ceiling((boundary - startLocal).TotalMinutes);
            await AddMinutesAsync(vehicleId, startAnchor, firstLeg, ct);

            // Second leg: boundary -> end
            var secondLeg = (int)Math.Ceiling((endLocal - boundary).TotalMinutes);
            await AddMinutesAsync(vehicleId, endAnchor, secondLeg, ct);
        }

        private async Task AddMinutesAsync(Guid vehicleId, DateOnly anchorDate, int deltaMinutes, CancellationToken ct) {
            // Ensure the row exists
            await EnsureResetAsync(vehicleId, anchorDate, ct);

            // UPDATE the minimum between 120 minutes (default) and the amount of time used
            var now = DateTimeOffset.UtcNow;
            var sql = """
                        UPDATE "daily_budgets"
                        SET "minutes_used" = LEAST(120, "minutes_used" + {2}),
                        "updated_at" = {3}
                        WHERE "vehicle_id" = {0} AND "anchor_date" = {1};
                        """;

            await db.Database.ExecuteSqlRawAsync(sql, [vehicleId, anchorDate, deltaMinutes, now], ct);
        }

        
    }
}