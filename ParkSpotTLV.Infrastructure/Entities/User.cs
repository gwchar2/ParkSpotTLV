using NetTopologySuite.Geometries; // if you don't need it here, you can remove this using
using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Infrastructure.Entities {
    public enum VehicleType { Car = 1, Truck = 2 }

    // What kind of permit is this?
    public enum PermitType { Default = 0, Disability = 1, ZoneResident = 2}

    public class User {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required, MaxLength(64)] public string Username { get; set; } = default!;
        [Required, MaxLength(256)] public string PasswordHash { get; set; } = default!;
        // A user can add multiple vehicles
        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
        public ICollection<RefreshToken> RefreshToken { get; set; } = new List<RefreshToken>();
    }

    public class Vehicle {
        public Guid Id { get; set; } = Guid.NewGuid();
        // Owner
        public User Owner { get; set; } = default!;

        public VehicleType Type { get; set; } = VehicleType.Car;

        // Permits that are tied specifically to this vehicle
        public ICollection<Permit> Permits { get; set; } = new List<Permit>();
    }

    public class Permit {
        public Guid Id { get; set; } = Guid.NewGuid();
        // Ownership 
        public Vehicle? Vehicle { get; set; }
        public PermitType Type { get; set; } = PermitType.Default;
        // Optional link to a Zone when Type == ZoneResident (or when relevant)
        public int? ZoneCode { get; set; }
        public Zone? Zone { get; set; }
        // Validity
        public DateOnly? ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
