
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Features.Parking.Services {
    /*
     * Classifies a segment according to user values 
     */
    public sealed class ClassificationService() : IClassificationService {

        /*
         * Restricted now, future doesnt matter             -> NOPARKING -> RED,   PayOnStart = X,     WillHaveToPayLater?: ILEGAL,   _restricted.IsRestrictedNow()
         * Free now, turns RESTRICTED within Required       -> RESTRICTED -> ORANGE,   PayOnStart = false, WillHaveToPayLater?: FALSE,     
         * PAID now, turns RESTRICTED within Required       -> RESTRICTED -> ORANGE,   PayOnStart = true,  WillHaveToPayLater?: TRUE,      
         * Free now, turns PAID within Required             -> PAID -> GREEN,         PayOnStart = false, WillHaveToPayLater?: !TRUE!,    
         * Free now, stays FREE within required             -> FREE -> GREEN,         PayOnStart = false, WillHaveToPayLater?: FALSE,      
         * PAID now, turns FREE within Required             -> PAID-> GREEN,         PayOnStart = true,  WillHaveToPayLater?: TRUE,     
         * PAID now, stays PAID within Required             -> PAID -> GREEN,         PayOnStart = true,  WillHaveToPayLater?: TRUE,      
         */
        public (string Group, string Reason, bool PayNow, bool PayLater) Classify(ParkingType parkingType, Availability availabilityNow, 
            PaymentDecision paymentDecisionNow, PaymentDecision? decisionAtPaidStart, TariffCalendarStatus calNow, DateTimeOffset now, int MinParkingTime) {


            bool isPayNow = paymentDecisionNow.PayNow == PaymentNow.Paid;                   // Is permit required to pay on start
            var horizonEnd = now.AddMinutes(Math.Min(MinParkingTime, 720));                 // The maximum amount to look ahead at is 12 hours
            DateTimeOffset? paidWindowStart = calNow.ActiveNow                              // The first time the user is required to pay
                ? now : calNow.NextStart is DateTimeOffset nextStart && nextStart > now ? nextStart : null;

            // Restricted now, future doesnt matter -> RESTRICTED -> RED,                   PayOnStart = X,     WillHaveToPayLater?: X,
            if (IsRestrictedNow(availabilityNow, now))
                return ("NOPARKING", "PrivilegedRestriction", PayNow: false, PayLater: false);

            // FREE now branch:      
            if (!isPayNow) {

                // Free now, turns RESTRICTED within required         -> LIMITED -> ORANGE,       PayOnStart = false, WillHaveToPayLater?: FALSE,    
                if (parkingType == ParkingType.Privileged
                    && availabilityNow.NextChange is DateTimeOffset privilegedStart
                    && privilegedStart < horizonEnd
                    && !calNow.ActiveNow
                    && paymentDecisionNow.Reason != "DisabilityPermitHolder"
                    && paymentDecisionNow.Reason != "PermitHomeZone") {
                    return ("RESTRICTED", $"Will become privileged-zone parking at {privilegedStart}", PayNow: false, PayLater: false);
                }

                // If I dont need to pay now because im a disability permit holder or zone permit (of same zone), I wont need to pay later as well.
                if (paymentDecisionNow.Reason is "DisabilityPermitHolder" or "PermitHomeZone") {
                    return ("FREE", $"Stays free parking until {horizonEnd}", PayNow: false, PayLater: false);
                }
                // If there is no paid window within the horizon it stays free forever!
                if (decisionAtPaidStart is null || paidWindowStart is null || paidWindowStart > horizonEnd) {
                    return ("FREE", $"Stays free parking until {horizonEnd}", PayNow: false, PayLater: false);
                }


                if (decisionAtPaidStart.PayNow == PaymentNow.Free ) {
                    // Free now, stays FREE within required             -> OK -> GREEN,         PayOnStart = false, WillHaveToPayLater?: FALSE,  
                    if (decisionAtPaidStart.Reason is "DisabilityPermitHolder" or "PermitHomeZone") {
                        return ("FREE", $"Stays free parking until {horizonEnd}", PayNow: false, PayLater: false);
                    }

                    // Free now, turns PAID within Required             -> OK -> GREEN,         PayOnStart = false, WillHaveToPayLater ?: !TRUE!,
                    if (decisionAtPaidStart.Reason == "RemainingDailyBudget"
                        && decisionAtPaidStart.FreeBudgetRemainingMinutes is int remainingAtStart) {
                        var budgetEnd = paidWindowStart.Value.AddMinutes(remainingAtStart);

                        if (budgetEnd >= horizonEnd) {
                            // Budget covers the whole horizon
                            return ("FREE",  $"Stays free parking until {horizonEnd}",  PayNow: false, PayLater: false);
                        }

                        // Budget ends inside the window -> will pay later at budgetEnd
                        return ("PAID", $"Will become paid parking at {budgetEnd}", PayNow: false, PayLater: true);
                    }

                    // Free now, stays FREE within required             -> OK -> GREEN,         PayOnStart = false, WillHaveToPayLater?: FALSE, 
                    return ("FREE", $"Stays free parking until {horizonEnd}",  PayNow: false, PayLater: false);


                }
                // Free now, turns PAID within Required             -> OK -> GREEN,         PayOnStart = false, WillHaveToPayLater ?: !TRUE!,
                return ("PAID", $"Will become paid parking at {paidWindowStart}", PayNow: false, PayLater: true);
            }// PAID now branch:   
            else {
                // PAID now, turns RESTRICTED later             -> LIMITED->ORANGE,     PayOnStart = true,  WillHaveToPayLater ?: X,  
                // Its deffinetly not a disability permit OR home zone permit... 
                if (availabilityNow.NextChange is DateTimeOffset privilegedStartWhilePaid
                    && privilegedStartWhilePaid <= horizonEnd
                    && parkingType == ParkingType.Privileged
                    && calNow.ActiveNow)
                    return ("RESTRICTED", $"Will become privileged-zone parking at {privilegedStartWhilePaid}", PayNow: true, PayLater: true);

                // PAID now, turns FREE later                   -> OK->GREEN,           PayOnStart = true,  WillHaveToPayLater ?: TRUE,   
                if (calNow.ActiveNow && calNow.NextEnd is DateTimeOffset paidEnd && paidEnd <= horizonEnd)
                    return ("PAID", $"Will become free parking at {paidEnd}", PayNow: true, PayLater: true);

                // PAID now, stays PAID later                   -> OK->GREEN,           PayOnStart = true,  WillHaveToPayLater ?: TRUE,  
                return ("PAID", $"Will remain paid parking until {horizonEnd}", PayNow: true, PayLater: true);
            }
        }

        public bool IsRestrictedNow(Availability availability, DateTimeOffset now) {
            return availability.AvailableFrom is DateTimeOffset availFrom
                && availFrom > now && availability.AvailableUntil is null;
        }
    }

}