using NetTopologySuite.Geometries; // if you don't need it here, you can remove this using
using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Infrastructure.Entities {
    public enum VehicleType { Car = 1, Truck = 2 }

    // What kind of permit is this?
    public enum PermitType { Default = 0, Disability = 1, ZoneResident = 2}

    public class User {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(64)]
        public string Username { get; set; } = default!;

        [Required, MaxLength(256)]
        public string PasswordHash { get; set; } = default!;

        // A user can add multiple vehicles
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }

    public class Vehicle {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Owner
        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = default!;

        public VehicleType Type { get; set; } = VehicleType.Car;

        // Optional but usually useful for identification
        [MaxLength(20)]
        public string? PlateNumber { get; set; } = "Default";

        // Permits that are tied specifically to this vehicle
        public ICollection<Permit> Permits { get; set; } = new List<Permit>();
    }

    public class Permit {
        public Guid Id { get; set; } = Guid.NewGuid();

        public PermitType Type { get; set; } = PermitType.Default;

        // Optional link to a Zone when Type == ZoneResident (or when relevant)
        public Guid? ZoneId { get; set; }
        public Zone? Zone { get; set; }

        // Ownership 
        public Guid? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        // Validity
        public DateOnly? ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
