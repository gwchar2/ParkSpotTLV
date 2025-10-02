using ParkSpotTLV.Api.Services.Evaluation.Strategies;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Logic {
    public interface IClassificationService {
        (string Group, string Reason, bool IsLegalNow) Classify(
            ParkingType parkingType, Availability availability, PriceDecision priceDecision, DateTimeOffset now, int limitedThresholdMinutes);
    }
}
