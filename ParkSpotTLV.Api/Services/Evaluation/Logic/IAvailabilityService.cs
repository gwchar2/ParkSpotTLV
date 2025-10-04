using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Api.Services.Evaluation.Contracts;

namespace ParkSpotTLV.Api.Services.Evaluation.Logic {
    public interface IAvailabilityService {
        Availability Compute(ParkingType parkingType, int? segmentZoneCode, Tariff tariff, DateTimeOffset now ,PermitSnapshot pov);

    }

    public sealed record Availability(

        DateTimeOffset? AvailableFrom,
        DateTimeOffset? AvailableUntil,
        DateTimeOffset? NextChange

    );
}
