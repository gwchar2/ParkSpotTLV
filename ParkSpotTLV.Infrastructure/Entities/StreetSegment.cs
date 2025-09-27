using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
/* 
*****Paid Street Types******
There are effectively two categories of paid parking (your PaidA and PaidB):

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
    Designed to discourage outsiders from parking long-term in busy districts.
*/


namespace ParkSpotTLV.Infrastructure.Entities {
    public enum ParkingType { 
        Free = 1,
        Paid = 2
    }
    
    public enum SegmentSide { Both = 0, Left = 1, Right = 2 }   

    public class StreetSegment {

        public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string OSMId { get; set; } = "";
        [MaxLength(128)] public string? NameEnglish { get; set; }
        [MaxLength(128)] public string? NameHebrew { get; set; }
        [Required] public LineString Geom { get; set; } = default!;
        public Guid? ZoneId { get; set; }
        public Zone? Zone { get; set; }
        public ParkingType ParkingType { get; set; } = ParkingType.Free;
        public SegmentSide Side { get; set; } = SegmentSide.Both;
        public DateTimeOffset? LastUpdated { get; set; }
    }
}
