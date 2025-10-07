
namespace ParkSpotTLV.Infrastructure.Entities {
    public class ParkingDailyBudget {

        public Guid VehicleId { get; set; }
        public DateOnly AnchorDate { get; set; }        // This is a daily window from 8am -> 8am 
        public int MinutesUsed { get; set; }            // Max 120 minutes
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

    }
}
