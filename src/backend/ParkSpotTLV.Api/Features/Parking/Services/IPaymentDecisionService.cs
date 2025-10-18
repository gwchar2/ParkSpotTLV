using ParkSpotTLV.Api.Features.Parking.Models;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Features.Parking.Services {

    /*
     *  Determinse whther this segment costs money NOW for the snapshot.
     */
    public interface IPaymentDecisionService {
        Task<PaymentDecision> DecideAsync(ParkingType parkingType, int? segmentZoneCode, Tariff tariff, DateTimeOffset now,
            PermitSnapshotDto pov, bool paidActiveNow, CancellationToken ct );
    }


    public enum PaymentNow { Free, Paid }

    public sealed record PaymentDecision(
        PaymentNow PayNow,
        string Reason,
        int? FreeBudgetRemainingMinutes = null
        );
}
