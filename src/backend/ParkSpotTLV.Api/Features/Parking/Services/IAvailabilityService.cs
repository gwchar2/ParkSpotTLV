using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Api.Features.Parking.Models;

namespace ParkSpotTLV.Api.Features.Parking.Services {
    public interface IAvailabilityService {
        Availability Compute(ParkingType parkingType, int? segmentZoneCode, Tariff tariff, DateTimeOffset now ,PermitSnapshot pov);

    }

    public sealed record Availability(

        DateTimeOffset? AvailableFrom,
        DateTimeOffset? AvailableUntil,
        DateTimeOffset? NextChange

    );
}
