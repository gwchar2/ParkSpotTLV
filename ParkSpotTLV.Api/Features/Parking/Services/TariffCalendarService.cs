using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Infrastructure;

namespace ParkSpotTLV.Api.Features.Parking.Services {

    /*
     * Checks if the time given falls in active hours
     */
    public sealed class TariffCalendarService(AppDbContext db) : ITariffCalendarService {

        private readonly AppDbContext _db = db;
        private const int WEEK_DAYS = 7;

        // Gets the current status according to the day of the week
        public TariffCalendarStatus GetStatus(Tariff tariff, DateTimeOffset now) {

            // Gets the current day of week and time
            var dayofweek = now.DayOfWeek;
            var timeofday = TimeOnly.FromDateTime(now.DateTime);

            var today = _db.TariffWindows.AsNoTracking()
                .SingleOrDefault(t => t.Tariff == tariff && t.DayOfWeek == dayofweek);

            // Decides on the tariff status
            if (today is not null && timeofday >= today.StartLocal && timeofday < today.EndLocal) {
                var end = Combine(now, today.EndLocal);
                return new TariffCalendarStatus(true, NextStartForTariff(tariff, now), end);
            }

            // Searches for the next start
            var nextStart = NextStartForTariff(tariff, now);
            DateTimeOffset? nextEnd = null;


            // We check that a next start exists, take its start times, and find its next end time.
            if (nextStart is DateTimeOffset ns) {
                var nsDayOfWeek = ns.DayOfWeek;
                var nsTimeOfDay = TimeOnly.FromDateTime(ns.DateTime);

                var nextDayWindow = _db.TariffWindows
                    .AsNoTracking()
                    .SingleOrDefault(t => t.Tariff == tariff && t.DayOfWeek == nsDayOfWeek);

                if (nextDayWindow is not null && nextDayWindow.StartLocal == nsTimeOfDay)
                    nextEnd = Combine(ns, nextDayWindow.EndLocal);
            }


            return new TariffCalendarStatus(false, nextStart, nextEnd);
        }

        // Combines the next start and next end
        private static DateTimeOffset Combine(DateTimeOffset anchor, TimeOnly t) {
            var d = anchor.Date;
            var dt = new DateTime(d.Year, d.Month, d.Day, t.Hour, t.Minute, t.Second, t.Millisecond, DateTimeKind.Unspecified);
            return new DateTimeOffset(dt, anchor.Offset);
        }

        // Finds the next start
        private DateTimeOffset? NextStartForTariff(Tariff tariff, DateTimeOffset now) {
            for (int i = 0; i < WEEK_DAYS; i++) {
                var day = now.AddDays(i);
                var windows = _db.TariffWindows.AsNoTracking()
                    .Where(t => t.Tariff == tariff && t.DayOfWeek == day.DayOfWeek)
                    .OrderBy(t => t.StartLocal)
                    .ToList();

                if (windows.Count == 0) continue;

                var todayTod = TimeOnly.FromDateTime(now.DateTime);
                foreach (var w in windows) {
                    if (i > 0 || w.StartLocal > todayTod)
                        return Combine(day, w.StartLocal);
                }
            }
            return null;
        }
    }
}
