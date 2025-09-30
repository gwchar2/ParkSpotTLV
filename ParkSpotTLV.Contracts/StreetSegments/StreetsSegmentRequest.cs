

namespace ParkSpotTLV.Contracts.StreetSegments {
    public sealed record StreetsSegmentRequest(
        Guid VehicleId,                 // So that we can check what permits it has, how much free time left to park, etc
        bool ShowPaidParking,
        bool ShowFreeParking,
        bool RestrictedParking,
        DayOfWeek dayOfWeek,            // What day of the week?
        DateTimeOffset TimeSpan         // Exact Time
    );
}
