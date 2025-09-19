
/*
 * Vehicle Type definition for the database
*/

using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Infrastructure.Entities {

    public enum VehicleType { Car = 0, Truck = 1 }
    public class Vehicle {

        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = default!;

        public VehicleType Type { get; set; } = VehicleType.Car;
        public bool HasDisabledPermit { get; set; } = false;

        public int ZonePermit { get; set; } = 0;    // from 0 to 10, 0 = none.
    }
}
