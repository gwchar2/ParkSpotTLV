
namespace ParkSpotTLV.Infrastructure.Entities {
    public class ParkingDailyBudget {

        public Guid VehicleId { get; set; }
        public DateOnly AnchorDate { get; set; }        // This is a daily window from 8am -> 8am 
        public int MinutesUsed { get; set; } = 0;         // Max 120 minutes
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }

    }
}
