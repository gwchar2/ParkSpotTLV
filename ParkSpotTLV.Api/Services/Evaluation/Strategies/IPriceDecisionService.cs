using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Strategies {

    /*
     *  Determinse whther this segment costs money now for the snapshot.
     */
    public interface IPriceDecisionService {

        Task<PriceDecision> DecideAsync(ParkingType parkingType, int? segmentZoneCode, Tariff tariff, DateTimeOffset now,
            PermitPov pov, bool paidActiveNow, CancellationToken ct );

    }
    public enum PriceNow { Free, Paid }

    public sealed record PriceDecision(
        PriceNow Price,
        string Reason,
        int? FreeBudgetRemainingMinutes = null
        );
}
