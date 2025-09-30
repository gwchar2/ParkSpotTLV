using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using ParkSpotTLV.Core.Models;
/* 
www.openstreetmap.org/
https://overpass-turbo.eu/index.html

*****Paid Street Types******
There are effectively two categories of paid parking (your PaidA and PaidB):
For BOTH categories -> If you have a zone permit -> You have 2 free daily hours to park in ANY ZONE in Tel Aviv. Afterwords you start paying discounted rate.
All zones cost money during the day in their regulated areas. In addition, some streets inside each zone are marked as “מועדפת”, and those are permit-only during the signed hours.
In all zones, after the official hours of payment/restriction end, the streets are free for all drivers.

PaidA (Standard Paid Parking – most zones) Zones 1,2,4,12,13 - 08:00–17:00 (Mon–Thu), 08:00–17:00 (Fri) 7 ₪ / 4.90 ₪ per hour
    Payment required Sunday–Thursday 08:00–19:00.
    Fridays and holiday eves 08:00–17:00.
    Price: 7 ₪ per hour if no permit at all for Tel-Aviv. If have SOME zone permit -> 4.90 ₪
    Residents of the zone do not pay.
    Applies to zones 1–7, except where special rules exist.

PaidB (Extended Paid Parking – Central areas) - Zones 6,7,9,10 - 08:00–21:00 (Mon–Thu), 08:00–17:00 (Fri) 12.40 ₪ / 8.68 ₪ per hour
    Payment required Sunday–Thursday 08:00–21:00.
    Fridays and holiday eves 08:00–17:00.
    Price: 12.40 ₪ per hour if no permit at all for Tel-Aviv. If have SOME zone permit -> 8.68 ₪
    Residents of the zone do not pay.

תחום חניה מועדפת (Preferred parking)
Blue-white curb where only vehicles with a permit for that specific zone may park during the specified times!
*/


namespace ParkSpotTLV.Infrastructure.Entities {  
    public enum SegmentSide { Both = 0, Left = 1, Right = 2 }   

    public class StreetSegment {
        /*
         * Ownership
         */
        public Guid Id { get; set; } = Guid.NewGuid();                  // Database ID

        /*
         * Geometric Data
         */
        [Required] public LineString Geom { get; set; } = default!;

        /*
         * Data
         */
        [Required] public string OSMId { get; set; } = "";              // ID on Opensourcemap @way ... 
        [MaxLength(128)] public string? NameEnglish { get; set; }
        [MaxLength(128)] public string? NameHebrew { get; set; }
        public Guid? ZoneId { get; set; }
        public Zone? Zone { get; set; }
        public ParkingType ParkingType { get; set; } = ParkingType.Free;
        public SegmentSide Side { get; set; } = SegmentSide.Both;
        public bool PrivilegedParking { get; set; } = false;            //  "restriction:conditional" : "Parking only for zone permit holders" / "parking:side:zone" :"*"
    }
}
