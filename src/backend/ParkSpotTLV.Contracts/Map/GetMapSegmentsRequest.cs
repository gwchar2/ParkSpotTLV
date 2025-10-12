
using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Contracts.Map {
    public sealed record GetMapSegmentsRequest (

        [Required] Guid ActivePermitId,
        [Required] double MinLon,                   // sw.Longitude
        [Required] double MinLat,                   // sw.Latitude
        [Required] double MaxLon,                   // ne.Longitude
        [Required] double MaxLat,                   // ne.Latitude
        [Required] double CenterLon,
        [Required] double CenterLat,
        DateTimeOffset Now,

        // Preferences 
        int MinParkingTime = 60
        );

}