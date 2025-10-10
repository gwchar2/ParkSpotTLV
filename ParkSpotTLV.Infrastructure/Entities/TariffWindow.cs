
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Infrastructure.Entities {

    public class TariffWindow {
        public int Id { get; set; }
        public Tariff Tariff { get; set; }                  // Which tariff this window belongs to
        public DayOfWeek DayOfWeek { get; set; }            // Local “clock” day and time span when tariff is active
        public TimeOnly StartLocal { get; set; }            // The times here are considered LOCAL as in ASIA/JERUSALEM TIMES!
        public TimeOnly EndLocal { get; set; }              // The times here are considered LOCAL as in ASIA/JERUSALEM TIMES!
    }
}
