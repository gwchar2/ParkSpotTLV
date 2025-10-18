using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Api.Features.Parking.Models;

namespace ParkSpotTLV.Api.Features.Parking.Services {
    public interface IAvailabilityService {

        /*
         * Checks if a segment is available at this moment in time
         */
        Availability Compute(ParkingType parkingType, int? segmentZoneCode, Tariff tariff, DateTimeOffset now ,PermitSnapshotDto pov);

    }

    /*
     * A record to define an available segment 
     */
    public sealed record Availability(

        DateTimeOffset? AvailableFrom,
        DateTimeOffset? AvailableUntil,
        DateTimeOffset? NextChange

    );
}
