using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Strategies {
    public sealed class PriceDecisionService(IDailyBudgetService budget) : IPriceDecisionService {

        private readonly IDailyBudgetService _budget = budget;

        public async Task<PriceDecision> DecideAsync(ParkingType parkingType, int? segmentZoneCode, Tariff tariff, DateTimeOffset now, 
            PermitPov pov, bool paidActiveNow, CancellationToken ct) {

            if (parkingType == ParkingType.Free) return new PriceDecision(PriceNow.Free, "AfterHoursOrFreeSegment");

            // Saturday & after hours are handled by paidActiveNow
            if (!paidActiveNow) return new PriceDecision(PriceNow.Free, "AfterHours");

            // Privileged segments: price & legality are same times, outsiders always ilegal.
            if (parkingType == ParkingType.Privileged)
                return new PriceDecision(PriceNow.Free, "PrivilegedResidentOrDisability");

            // Handle payed segments during there active hours
            switch (pov.Type) {

                // Disability permits are always free
                case PermitPovType.Disability:
                    return new PriceDecision(PriceNow.Free, "Disability");

                case PermitPovType.Zone:
                    // Residents dont pay in there own zone
                    if (pov.ZoneCode == segmentZoneCode)
                        return new PriceDecision(PriceNow.Free, "PermitHomeZone");

                    // We check if there is daily free parking budget
                    if (pov.UserId is Guid uid) {
                        var localDate = DateOnly.FromDateTime(now.Date);
                        await _budget.EnsureResetAsync(uid, localDate, ct);

                        var remaining = await _budget.GetRemainingMinutesAsync(uid, localDate, ct);

                        if (remaining > 0)
                            return new PriceDecision(PriceNow.Free, "PermitDailyBudget", remaining);

                        return new PriceDecision(PriceNow.Paid, "TariffPaid");

                    }

                    // no user id or just default...
                    return new PriceDecision(PriceNow.Paid, "TariffPaid");

                default:
                    return new PriceDecision(PriceNow.Paid, "TariffPaid");
            }
        }
    }
}
