

namespace ParkSpotTLV.Infrastructure.Entities {


    public class Vehicle {
        public Guid Id { get; set; } = Guid.NewGuid();
        // Owner
        public User Owner { get; set; } = default!;

        public VehicleType Type { get; set; } = VehicleType.Car;

        // Permits that are tied specifically to this vehicle
        public ICollection<Permit> Permits { get; set; } = new List<Permit>();
    }
}
