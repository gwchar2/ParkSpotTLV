
using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Api.Services.Evaluation.Strategies;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Logic {
    public interface IClassificationService {
        (string Group, string Reason,bool PayNow, bool PayLater) Classify(ParkingType parkingType, Availability availabilityNow, 
            PaymentDecision paymenDecisionNow, PaymentDecision? decisionAtPaidStart, TariffCalendarStatus calNow, DateTimeOffset now, int MinParkingTime);
    }
}
