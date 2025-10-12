using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Budget;
using ParkSpotTLV.Api.Features.Parking.Models;

namespace ParkSpotTLV.Api.Features.Parking.Services {
    /*
     *  Determinse whther this segment costs money NOW for the snapshot.
     */

    public sealed class PaymentDecisionService(IDailyBudgetService budget) : IPaymentDecisionService {

        private readonly IDailyBudgetService _budget = budget;

        public async Task<PaymentDecision> DecideAsync(ParkingType parkingType, int? segmentZoneCode, Tariff tariff, DateTimeOffset now,
            PermitSnapshot pov, bool ActiveNow, CancellationToken ct) {

            // Saturday & after hours are handled by ActiveNow
            if (!ActiveNow) return new PaymentDecision(PaymentNow.Free, "AfterHours");

            // Handle payed segments during there active hours PermitSnapType { None, Zone, Disability }
            switch (pov.Type) {
                // Disability permits are always free
                case PermitSnapType.Disability:
                    return new PaymentDecision(PaymentNow.Free, "DisabilityPermitHolder");

                case PermitSnapType.Zone:
                    // Residents dont pay in there own zone
                    if (pov.ZoneCode == segmentZoneCode)
                        return new PaymentDecision(PaymentNow.Free, "PermitHomeZone");

                    // Else, We check if there is daily free parking budget
                    if (pov.VehicleId is Guid vuid) {
                        var localDate = ParkingBudgetTimeHandler.AnchorDateFor(now);
                        await _budget.EnsureResetAsync(vuid, localDate, ct);

                        var remaining = await _budget.GetRemainingMinutesAsync(vuid, localDate, ct);

                        if (remaining > 0)
                            return new PaymentDecision(PaymentNow.Free, "RemainingDailyBudget", remaining);

                        // If no remaining budget, user must pay
                        return new PaymentDecision(PaymentNow.Paid, "TariffPaid", 0);

                    }

                // no user id or just default...
                    return new PaymentDecision(PaymentNow.Paid, "TariffPaid");

                default:
                    return new PaymentDecision(PaymentNow.Paid, "TariffPaid");
            }
        }
    }
}
