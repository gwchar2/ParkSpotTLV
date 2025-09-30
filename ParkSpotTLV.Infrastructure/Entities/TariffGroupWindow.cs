using ParkSpotTLV.Contracts.Enums;


/*
 * TariffGroupWindow: defines PAID windows shared by all zones
 * that carry the same Taarif (A/B). Outside these windows is FREE.
 * Example:
 * - Group A: Sun–Thu 08:00–19:00, Fri 08:00–17:00, Sat none.
 * - Group B: Sun–Thu 08:00–21:00, Fri 08:00–17:00, Sat none.
 *****Paid Street Types******
There are effectively two categories of paid parking (your PaidA and PaidB):
For BOTH categories -> If you have a zone permit -> You have 2 free daily hours to park in ANY ZONE in Tel Aviv. Afterwords you start paying discounted rate.
All zones cost money during the day in their regulated areas. In addition, some streets inside each zone are marked as “מועדפת”, and those are permit-only during the signed hours.
In all zones, after the official hours of payment/restriction end, the streets are free for all drivers.

PaidA (Standard Paid Parking – most zones) Zones 1,2,4,12,13 - 08:00–19:00 (Mon–Thu), 08:00–17:00 (Fri) 7 ₪ / 4.90 ₪ per hour
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
    public class TariffGroupWindow {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Taarif Taarif { get; set; }                                      // Matches Zone.Taarif (your existing enum)
        public DaysOfWeekMask Days { get; set; } = DaysOfWeekMask.Weekdays;     // Weekly schedule
        public bool IsAllDay { get; set; } = false;                             // Paid window times in local TZ (Asia/Jerusalem).
        public TimeOnly? StartLocalTime { get; set; }                           // e.g., 08:00
        public TimeOnly? EndLocalTime { get; set; }                             // e.g., 19:00 (can cross midnight if End < Start)
        public int Priority { get; set; } = 0;                                  // keep for future overlaps; higher wins
        public bool Enabled { get; set; } = true;
        public string? Note { get; set; }
    }
}