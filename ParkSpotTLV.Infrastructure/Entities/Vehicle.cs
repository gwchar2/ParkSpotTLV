using ParkSpotTLV.Core.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkSpotTLV.Infrastructure.Entities {
    public class Vehicle {
        public Guid Id { get; set; } = Guid.NewGuid();
        // Owner
        public User Owner { get; set; } = default!;
        public Guid OwnerId { get; set; }

        public VehicleType Type { get; set; } = VehicleType.Car;

        /* Concurrency token EF updates this automatically (So that 2 writes wont be appointed to same variable at once!) */
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public uint Xmin { get; private set; }

        // Permits that are tied specifically to this vehicle
        public ICollection<Permit> Permits { get; set; } = new List<Permit>();
    }
}
