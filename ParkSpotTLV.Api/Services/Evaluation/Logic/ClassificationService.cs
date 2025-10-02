using ParkSpotTLV.Api.Services.Evaluation.Strategies;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Logic {
    /*
     * Classifies a segment 
     */
    public sealed class ClassificationService : IClassificationService{

        public (string Group, string Reason, bool IsLegalNow) Classify(
            ParkingType parkingType, Availability availability, PriceDecision priceDecision, DateTimeOffset now, int limitedThresholdMinutes) {

            // Illegal if currently AvailableFrom > now && no AvailableUntil
            if (availability.AvailableFrom is DateTimeOffset availfrom && availfrom > now
                && availability.AvailableUntil is null)
                return ("Illegal", "PrivilegedRestriction", false);

            // Limited if legal / free now but becomes paid / privileged soon (preference defined times)
            if (priceDecision.Price == PriceNow.Free && 
                availability.NextChange is DateTimeOffset nc) {

                var minutes = (int)Math.Floor((nc - now).TotalMinutes);
                if (minutes >= 0 && minutes <= limitedThresholdMinutes)
                    return ("Limited", "BecomesRestrictedSoon", true);

            }

            return priceDecision.Price == PriceNow.Paid
                ? ("Paid", priceDecision.Reason, true)
                : ("Free", priceDecision.Reason, true);

        }

    }
}
