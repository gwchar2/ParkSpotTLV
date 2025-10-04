
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Infrastructure.Entities {

    public sealed class TariffWindow {
        public int Id { get; set; }
        public Tariff Tariff { get; set; }           // Which tariff this window belongs to
        public DayOfWeek DayOfWeek { get; set; }    // Local “clock” day and time span when tariff is active
        public TimeOnly StartLocal { get; set; }
        public TimeOnly EndLocal { get; set; }
    }
}
