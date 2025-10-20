
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Api.Features.Parking.Models;
using ParkSpotTLV.Contracts.Budget;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Time;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;


namespace ParkSpotTLV.Api.Features.Parking.Services {

    /*
     * This class is in charge of managing anything related to daily budget
     */
    public sealed class DailyBudgetService(AppDbContext db, IClock clock, ITariffCalendarService calendar) : IDailyBudgetService {
        
        private readonly ITariffCalendarService _calendar = calendar;
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
                    CreatedAtUtc = clock.UtcNow,
                    UpdatedAtUtc = clock.UtcNow
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
            var boundary = new DateTimeOffset(boundaryLocal, endLocal.Offset); 
            var firstLeg = (int)Math.Ceiling((boundary - startLocal).TotalMinutes);
            await AddMinutesAsync(vehicleId, startAnchor, firstLeg, ct);

            // Second leg: boundary -> end
            var secondLeg = (int)Math.Ceiling((endLocal - boundary).TotalMinutes);
            await AddMinutesAsync(vehicleId, endAnchor, secondLeg, ct);
        }

        /*
         * Adds minutes to total consumed budget minutes table
         */
        private async Task AddMinutesAsync(Guid vehicleId, DateOnly anchorDate, int deltaMinutes, CancellationToken ct) {
            // Ensure the row exists
            await EnsureResetAsync(vehicleId, anchorDate, ct);

            // UPDATE the minimum between 120 minutes (default) and the amount of time used
            var now = clock.UtcNow;
            await db.ParkingDailyBudget
                    .Where(p => p.VehicleId == vehicleId && p.AnchorDate == anchorDate)
                    .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.MinutesUsed, p => (p.MinutesUsed + deltaMinutes) >= 120
                                    ? 120
                                    : (p.MinutesUsed + deltaMinutes))
                    .SetProperty(p => p.UpdatedAtUtc, _ => now), ct);
        }

        /*
         * Slices the day by boundaries so we can evaluate properly
         */
        public IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> SliceByAnchorBoundary(DateTimeOffset startLocal, DateTimeOffset endLocal) {

            var reset = new TimeSpan(8, 0, 0);
            var current = startLocal;

            while (true) {
                var baseDate = current.TimeOfDay >= reset
                    ? DateOnly.FromDateTime(current.Date)
                    : DateOnly.FromDateTime(current.Date).AddDays(-1);

                var boundaryLocal = new DateTimeOffset(
                    baseDate.AddDays(1).ToDateTime(ParkingBudgetTimeHandler.ResetTime),
                    current.Offset);

                var sliceEnd = boundaryLocal < endLocal ? boundaryLocal : endLocal;
                yield return (current, sliceEnd);

                if (sliceEnd >= endLocal) break;
                current = sliceEnd;
            }
        }

        /*
         * Calculates the amount of budget / free time / paid time used & remaining values
         */
        public async Task<BudgetCalculationDTO> CalculateAsync(ParkingSession session, CancellationToken ct) {
            DateTimeOffset timeLocal = clock.LocalNow;
            DateTimeOffset startedLocal = clock.ToLocal(session.StartedUtc);
            DateTimeOffset nextLocal = clock.ToLocal(session.NextChangeUtc);

            DateTimeOffset? consumeStart = null;
            DateTimeOffset? consumeEnd = timeLocal;

            var isFreeGroup = session.Group.Equals("FREE", StringComparison.OrdinalIgnoreCase);
            var isLimitedGroup = session.Group.Equals("LIMITED", StringComparison.OrdinalIgnoreCase);
            var isPaidGroup = session.Group.Equals("PAID", StringComparison.OrdinalIgnoreCase);

            /* 
             * If its a paid group, configure the start / stop times of payment
             */
            if (isPaidGroup) {
                if (session.IsPayNow) {
                    consumeStart = startedLocal;
                    if (!session.IsPayLater) {
                        consumeEnd = nextLocal;
                    }
                } else if (session.IsPayLater && nextLocal < timeLocal) {
                    consumeStart = nextLocal;
                }
            }
            /*
             * If its a limited group, the end time is always the time it turns to restricted
             */
            if (isLimitedGroup)
                consumeEnd = nextLocal;
            /*
             * If its a free group, we charge budget ONLY if parking street is of PAID type, and we are in ACTIVE hours! (According to tariff zone, either 8-19 or 8-21)
             */
            if (isFreeGroup && session.ParkingType == ParkingType.Paid) {
                var calNow = _calendar.GetStatus(session.Tariff, startedLocal);
                if (calNow.ActiveNow)
                    consumeStart = startedLocal;
            }
            var totalLegalMinutes = (int)Math.Ceiling((timeLocal - startedLocal).TotalMinutes);
            var freeMinutesCharged = 0;
            var remainingToday = 0;

            foreach (var (sliceStart, sliceEnd) in SliceByAnchorBoundary(startedLocal, timeLocal)) {
                if (consumeStart is null || consumeEnd is null) continue;

                var start = sliceStart >= consumeStart ? sliceStart : consumeStart.Value;
                var end = sliceEnd <= consumeEnd ? sliceEnd : consumeEnd.Value;
                if (end <= start) continue;

                var eligible = (int)Math.Ceiling((end - start).TotalMinutes);
                if (eligible <= 0) continue;

                var anchor = ToAnchor(sliceStart);
                await EnsureResetAsync(session.VehicleId, anchor, ct);
                var remaining = await GetRemainingMinutesAsync(session.VehicleId, anchor, ct);

                var toConsume = Math.Min(remaining, eligible);
                if (toConsume > 0) {
                    await ConsumeAsync(session.VehicleId, start, start.AddMinutes(toConsume), ct);
                    freeMinutesCharged += toConsume;
                }

                if (anchor == ToAnchor(timeLocal))
                    remainingToday = await GetRemainingMinutesAsync(session.VehicleId, anchor, ct);
            }

            var freeMinutes = 0;
            if (!isFreeGroup && !session.IsPayLater && session.NextChangeUtc is not null && nextLocal < timeLocal) {
                freeMinutes = (int)Math.Ceiling((timeLocal - nextLocal).TotalMinutes);
            }

            freeMinutes = isFreeGroup ? totalLegalMinutes : freeMinutes;
            var paidMinutes = isFreeGroup ? 0 : Math.Max(0, totalLegalMinutes - freeMinutesCharged - freeMinutes); 
            
            return new BudgetCalculationDTO(
                TotalMinutes: totalLegalMinutes,
                PaidMinutes: paidMinutes,
                FreeMinutes: freeMinutes,
                FreeMinutesCharged: freeMinutesCharged,
                RemainingToday: remainingToday
            );
        }


        /*
         * Transfers to anchor date
         */
        public DateOnly ToAnchor(DateTimeOffset t)
            => ParkingBudgetTimeHandler.AnchorDateFor(t);
    }
}