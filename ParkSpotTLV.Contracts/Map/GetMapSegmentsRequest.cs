
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
/*
"activePermitId": "67e3f301-0e00-47d6-8916-e2f890b9dbf4",
  "minLon": 34.7870913,
  "minLat": 32.0919860,
  "maxLon": 34.7935980,
  "maxLat": 32.0952654,
  "centerLon": 34.7905720,
  "centerLat": 32.0938395,
  "now": "2025-10-02T20:30:00+03:00",
  "minparkingTime": 120,
  "showFree": true,
  "showPaid": true,
  "showLimited": true,
  "showAll": false
*/