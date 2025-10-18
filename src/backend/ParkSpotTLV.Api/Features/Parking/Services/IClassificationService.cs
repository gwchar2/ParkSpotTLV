
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Features.Parking.Services {
    public interface IClassificationService {
        /*
         *  Classifies the segment according to current values
         */
        (string Group, string Reason,bool PayNow, bool PayLater) Classify(ParkingType parkingType, Availability availabilityNow, 
            PaymentDecision paymenDecisionNow, PaymentDecision? decisionAtPaidStart, TariffCalendarStatus calNow, DateTimeOffset now, int MinParkingTime);

        /*
         * Checks if a segment is restricted at this moment
         */
        bool IsRestrictedNow(Availability availability, DateTimeOffset now);
    }
}
