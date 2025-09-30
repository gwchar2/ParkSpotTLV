using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.StreetSegments;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Api.Endpoints.SegmentRules {


    /*
     * SegmentRulesEvaluator
     * ---------------------
     * Evaluates a street segment's parking status at a given instant for a given vehicle and user preferences.
     * Data sources:
     *  - StreetSegmentRuleWindow (per-segment overrides: Forbidden or special Paid)
     *  - TariffGroupWindow (group A/B paid windows; outside = Free)
     *
     * Precedence:
     *  1) Segment Forbidden override (any) => Forbidden
     *  2) Segment Paid override (if active) OR Tariff Paid (if active) => Paid
     *  3) Otherwise => Free
     *
     * Permit adjustments:
     *  - Disabled permit => Paid becomes Free (still blocked by Forbidden)
     *  - Resident of same zone => Paid becomes Free within that zone
     *
     * Boundaries:
     *  - NextChangeAtUtc: informational "next status flip" (e.g., end of paid or start of paid/forbidden)
     *  - AvailableUntilUtc: when it becomes ILLEGAL given includePaid (if includePaid=false, entering Paid ends legality)
     */

    public sealed class SegmentRulesEvaluator(AppDbContext db, string? tzId = "Asia/Jerusalem") {
        private readonly AppDbContext _db = db;
        private readonly TimeZoneInfo _tz = TimeZoneInfo.FindSystemTimeZoneById(tzId ?? "Asia/Jerusalem");

        /* Lightweight projection for evaluation (avoid loading geometry) */
        private sealed record SegmentSnapshot(
            Guid Id,
            SegmentSide Side,
            int? ZoneCode,
            Taarif Taarif
        );

        /*
         * EvaluateAsync
         * -------------
         * includePaid: pass the user's "showPaid" preference (true means Paid is acceptable/desired in the result set).
         * hasDisabledPermit / residentZoneCode: permit context for Paid→Free conversions.
         */
        public async Task<SegmentRuleResult> EvaluateAsync(
            Guid segmentId,
            DateTimeOffset timeUtc,
            bool includePaid,
            bool hasDisabledPermit,
            int? residentZoneCode,
            CancellationToken ct = default) {
            // 1) Pull minimal segment data (side + zone code + tariff group)
            var seg = await _db.StreetSegments
                .AsNoTracking()
                .Where(s => s.Id == segmentId)
                .Select(s => new SegmentSnapshot(
                    s.Id,
                    s.Side,
                    s.Zone != null ? s.Zone.Code : null,
                    s.Zone != null ? s.Zone.Taarif : Taarif.City_Center // default if ever null
                ))
                .SingleAsync(ct);

            // 2) Convert reference instant (UTC -> local) for window matching
            var localTime = TimeZoneInfo.ConvertTime(timeUtc, _tz);
            var localDowMask = ToMask(localTime.DayOfWeek);
            var localTod = new TimeOnly(localTime.Hour, localTime.Minute, localTime.Second);

            // 3) Load applicable per-segment overrides for this day (ordered by priority)
            var overrides = await _db.StreetSegmentRuleWindows
                .AsNoTracking()
                .Where(w => w.StreetSegmentId == segmentId && w.Enabled)
                .Where(w => (w.Days & localDowMask) != 0)
                .OrderByDescending(w => w.Priority)
                .ToListAsync(ct);

            // 4) Segment-level FORBIDDEN active now?
            foreach (var w in overrides.Where(w => w.Kind == SegmentWindowKind.Forbidden)) {
                if (AppliesNow(w, seg.Side, localTod)) {
                    // Compute when this forbidden period ends (informational boundary)
                    var nextChangeLocal = NextWindowEndLocal(localTime, w);
                    var nextChangeUtc = nextChangeLocal?.ToUniversalTime();

                    return new(
                        SegmentStatus.Forbidden,
                        nextChangeUtc,
                        /* AvailableUntilUtc: already illegal */ timeUtc,
                        0,
                        "Forbidden by segment override"
                    );
                }
            }

            // 5) Segment-level PAID active now?
            var paidOverrideNow = overrides
                .Where(w => w.Kind == SegmentWindowKind.Paid)
                .FirstOrDefault(w => AppliesNow(w, seg.Side, localTod));

            // 6) Tariff paid now?
            TariffGroupWindow? activeTariff = null;
            if (paidOverrideNow is null) {
                // get all windows for today's DOW, then choose first that applies "now"
                var todaysTariffWindows = await _db.TariffGroupWindows
                    .AsNoTracking()
                    .Where(t => t.Enabled && t.Taarif == seg.Taarif && (t.Days & localDowMask) != 0)
                    .OrderByDescending(t => t.Priority)
                    .ToListAsync(ct);

                activeTariff = todaysTariffWindows.FirstOrDefault(tw => AppliesNow(tw, localTod));
            }

            // 7) Determine raw status (before permits)
            var rawStatus = (paidOverrideNow is not null || activeTariff is not null)
                ? SegmentStatus.Paid
                : SegmentStatus.Free;

            // 8) Apply permit-based conversions: Disabled or Resident in same zone => Paid -> Free
            if (rawStatus == SegmentStatus.Paid) {
                var residentHere = residentZoneCode.HasValue && seg.ZoneCode.HasValue && residentZoneCode == seg.ZoneCode;
                if (hasDisabledPermit || residentHere) {
                    rawStatus = SegmentStatus.Free;
                }
            }

            // 9) Boundaries (informational nextChange + legality cut-off based on includePaid)
            var (nextChangeLocal2, availableUntilLocal) = ComputeBoundaries(
                localTime,
                seg.Side,
                overrides,
                activeTariff,
                includePaid
            );

            var nextChangeUtc2 = nextChangeLocal2?.ToUniversalTime();
            var availableUntilUtc = availableUntilLocal?.ToUniversalTime();

            var minutes = availableUntilUtc.HasValue
                ? (int)Math.Max(0, (availableUntilUtc.Value - timeUtc).TotalMinutes)
                : int.MaxValue;

            var hint = rawStatus == SegmentStatus.Free ? "Free now" : "Paid now";

            return new(
                rawStatus,
                nextChangeUtc2,
                availableUntilUtc,
                minutes,
                hint
            );
        }

        // -------------------------- Helpers --------------------------

        /* Map DayOfWeek to our bitmask for quick window checks */
        private static DaysOfWeekMask ToMask(DayOfWeek d) => d switch {
            DayOfWeek.Sunday => DaysOfWeekMask.Sun,
            DayOfWeek.Monday => DaysOfWeekMask.Mon,
            DayOfWeek.Tuesday => DaysOfWeekMask.Tue,
            DayOfWeek.Wednesday => DaysOfWeekMask.Wed,
            DayOfWeek.Thursday => DaysOfWeekMask.Thu,
            DayOfWeek.Friday => DaysOfWeekMask.Fri,
            DayOfWeek.Saturday => DaysOfWeekMask.Sat,
            _ => DaysOfWeekMask.All
        };

        /* Check if a segment override applies at this instant (by side + time range) */
        private static bool AppliesNow(StreetSegmentRuleWindow w, SegmentSide segSide, TimeOnly tod) {
            if (!w.Enabled) return false;
            if (w.AppliesToSide != SegmentSide.Both && w.AppliesToSide != segSide) return false;
            if (w.IsAllDay) return true;
            if (w.StartLocalTime is null || w.EndLocalTime is null) return false;

            return TimeInRange(tod, w.StartLocalTime.Value, w.EndLocalTime.Value);
        }

        /* Check if a tariff window applies at this instant */
        private static bool AppliesNow(TariffGroupWindow w, TimeOnly tod) {
            if (!w.Enabled) return false;
            if (w.IsAllDay) return true;
            if (w.StartLocalTime is null || w.EndLocalTime is null) return false;

            return TimeInRange(tod, w.StartLocalTime.Value, w.EndLocalTime.Value);
        }

        /* Time range check that supports windows crossing midnight (e.g., 22:00–06:00) */
        private static bool TimeInRange(TimeOnly t, TimeOnly start, TimeOnly end) {
            if (end > start) return t >= start && t < end;
            // crosses midnight
            return t >= start || t < end;
        }

        /*
         * NextWindowEndLocal
         * ------------------
         * For an active (currently-applying) override window, compute when it ends today.
         * If all-day, we return null (meaning: no boundary today).
         */
        private static DateTimeOffset? NextWindowEndLocal(DateTimeOffset nowLocal, StreetSegmentRuleWindow w) {
            if (w.IsAllDay) return null;
            if (w.EndLocalTime is null) return null;

            var end = new DateTimeOffset(nowLocal.Date, nowLocal.Offset)
                .AddHours(w.EndLocalTime.Value.Hour)
                .AddMinutes(w.EndLocalTime.Value.Minute);

            if (end <= nowLocal) end = end.AddDays(1); // if past for today, it's tomorrow
            return end;
        }

        /*
         * ComputeBoundaries
         * -----------------
         * Returns:
         *  - nextChangeLocal: nearest status flip (either end of paid or start of paid/forbidden)
         *  - availableUntilLocal: when it becomes illegal given includePaid
         *      * includePaid=true  -> only Forbidden ends legality
         *      * includePaid=false -> Forbidden or entering Paid ends legality
         */
        private static (DateTimeOffset? nextChangeLocal, DateTimeOffset? availableUntilLocal) ComputeBoundaries(
            DateTimeOffset nowLocal,
            SegmentSide segSide,
            List<StreetSegmentRuleWindow> overrides,
            TariffGroupWindow? activeTariffNow,
            bool includePaid) {
            var nextForbiddenStart = NextForbiddenStartLocal(nowLocal, segSide, overrides);
            var nextPaidChange = NextPaidChangeLocal(nowLocal, segSide, overrides, activeTariffNow);

            var nextChange = MinNonNull(nextForbiddenStart, nextPaidChange);

            var availableUntil = includePaid
                ? nextForbiddenStart
                : MinNonNull(nextForbiddenStart, NextPaidStartLocal(nowLocal, segSide, overrides));

            return (nextChange, availableUntil);
        }

        /* Find the next time a Forbidden override *starts* */
        private static DateTimeOffset? NextForbiddenStartLocal(
            DateTimeOffset nowLocal,
            SegmentSide segSide,
            List<StreetSegmentRuleWindow> overrides) {
            for (int d = 0; d < 8; d++) {
                var date = nowLocal.Date.AddDays(d);
                var dow = ToMask(date.DayOfWeek);

                var sameDay = overrides.Where(o =>
                    o.Enabled &&
                    o.Kind == SegmentWindowKind.Forbidden &&
                    (o.Days & dow) != 0 &&
                    (o.AppliesToSide == SegmentSide.Both || o.AppliesToSide == segSide)
                );

                foreach (var o in sameDay) {
                    if (o.IsAllDay) {
                        var start = new DateTimeOffset(date, nowLocal.Offset);
                        if (start > nowLocal) return start;
                    } else if (o.StartLocalTime is not null) {
                        var start = new DateTimeOffset(date, nowLocal.Offset)
                            .AddHours(o.StartLocalTime.Value.Hour)
                            .AddMinutes(o.StartLocalTime.Value.Minute);
                        if (start > nowLocal) return start;
                    }
                }
            }
            return null;
        }

        /*
         * NextPaidChangeLocal
         * -------------------
         * If currently in a Paid window (segment override or tariff), return its end (today).
         * Otherwise, return the next Paid *start* (override first, then tariff canonical 08:00 on valid days).
         */
        private static DateTimeOffset? NextPaidChangeLocal(
            DateTimeOffset nowLocal,
            SegmentSide segSide,
            List<StreetSegmentRuleWindow> overrides,
            TariffGroupWindow? activeTariffNow) {
            var tod = new TimeOnly(nowLocal.Hour, nowLocal.Minute, nowLocal.Second);

            // Segment Paid override active now?
            var paidNow = overrides
                .Where(o => o.Enabled &&
                            o.Kind == SegmentWindowKind.Paid &&
                            (o.AppliesToSide == SegmentSide.Both || o.AppliesToSide == segSide))
                .FirstOrDefault(o => !o.IsAllDay &&
                                     o.StartLocalTime is not null &&
                                     o.EndLocalTime is not null &&
                                     TimeInRange(tod, o.StartLocalTime.Value, o.EndLocalTime.Value));

            if (paidNow is not null && paidNow.EndLocalTime is not null) {
                var end = new DateTimeOffset(nowLocal.Date, nowLocal.Offset)
                    .AddHours(paidNow.EndLocalTime.Value.Hour)
                    .AddMinutes(paidNow.EndLocalTime.Value.Minute);
                if (end > nowLocal) return end;
            }

            // Tariff Paid active now?
            if (activeTariffNow is not null && activeTariffNow.EndLocalTime is not null) {
                var end = new DateTimeOffset(nowLocal.Date, nowLocal.Offset)
                    .AddHours(activeTariffNow.EndLocalTime.Value.Hour)
                    .AddMinutes(activeTariffNow.EndLocalTime.Value.Minute);
                if (end > nowLocal) return end;
            }

            // Otherwise we are Free now: next change is next Paid start
            return NextPaidStartLocal(nowLocal, segSide, overrides);
        }

        /*
         * NextPaidStartLocal
         * ------------------
         * Looks for the next start of a Paid window:
         *  1) Segment Paid overrides (by priority, by upcoming days)
         *  2) Tariff canonical start based on seeded data:
         *     - Sun–Thu: 08:00
         *     - Fri:     08:00
         *     - Sat:     none (free all day)
         */
        private static DateTimeOffset? NextPaidStartLocal(
            DateTimeOffset nowLocal,
            SegmentSide segSide,
            List<StreetSegmentRuleWindow> overrides) {
            // A) Segment overrides first
            for (int d = 0; d < 8; d++) {
                var date = nowLocal.Date.AddDays(d);
                var dow = ToMask(date.DayOfWeek);

                var sameDay = overrides.Where(o =>
                    o.Enabled &&
                    o.Kind == SegmentWindowKind.Paid &&
                    (o.Days & dow) != 0 &&
                    (o.AppliesToSide == SegmentSide.Both || o.AppliesToSide == segSide)
                );

                foreach (var o in sameDay) {
                    if (!o.IsAllDay && o.StartLocalTime is not null) {
                        var start = new DateTimeOffset(date, nowLocal.Offset)
                            .AddHours(o.StartLocalTime.Value.Hour)
                            .AddMinutes(o.StartLocalTime.Value.Minute);
                        if (start > nowLocal) return start;
                    }
                }
            }

            // B) Tariff canonical start: Sun–Fri at 08:00; Saturday none
            for (int d = 0; d < 8; d++) {
                var date = nowLocal.Date.AddDays(d);
                var dow = date.DayOfWeek;
                if (dow == DayOfWeek.Saturday) continue; // no paid on Saturday

                var start = new DateTimeOffset(date, nowLocal.Offset).AddHours(8);
                if (start > nowLocal) return start;
            }

            return null;
        }

        /* Utility: min of two nullable instants */
        private static DateTimeOffset? MinNonNull(DateTimeOffset? a, DateTimeOffset? b)
            => a is null ? b : (b is null ? a : (a < b ? a : b));
    }
}