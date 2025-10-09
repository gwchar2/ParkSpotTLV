
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Features.Parking.Services {
    public interface IClassificationService {
        (string Group, string Reason,bool PayNow, bool PayLater) Classify(ParkingType parkingType, Availability availabilityNow, 
            PaymentDecision paymenDecisionNow, PaymentDecision? decisionAtPaidStart, TariffCalendarStatus calNow, DateTimeOffset now, int MinParkingTime);

        bool IsRestrictedNow(Availability availability, DateTimeOffset now);
    }
}
