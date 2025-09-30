using ParkSpotTLV.Contracts.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkSpotTLV.Infrastructure.Entities { // ADD NAME
    public class Vehicle {

        /* 
         * Ownership
         */
        public Guid Id { get; set; } = Guid.NewGuid();
        public User Owner { get; set; } = default!;
        public Guid OwnerId { get; set; }

        /* 
         * Defining data
         */
        public string Name { get; set; } = "";
        public VehicleType Type { get; set; } = VehicleType.Car;

        /*
         * Concurrency
         */
        /* Concurrency token EF updates this automatically (So that 2 writes wont be appointed to same variable at once!) */
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public uint Xmin { get; private set; }
        // Permits that are tied specifically to this vehicle
        public ICollection<Permit> Permits { get; set; } = [];
    }
}
