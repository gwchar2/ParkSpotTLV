
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Infrastructure.Entities {

    public class TariffWindow {
        public int Id { get; set; }
        public Tariff Tariff { get; set; }                  // Which tariff this window belongs to
        public DayOfWeek DayOfWeek { get; set; }            
        public TimeOnly StartLocal { get; set; }           
        public TimeOnly EndLocal { get; set; }             
    }
}
