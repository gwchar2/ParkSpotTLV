using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Infrastructure.Entities {

    public class User {
        /*
         * Ownership
         */
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required, MaxLength(64)] public string Username { get; set; } = default!;
        [Required, MaxLength(256)] public string PasswordHash { get; set; } = default!;

        /* 
         * Vehicles & Permits 
         */
        public ICollection<Vehicle> Vehicles { get; set; } = [];

        /* 
         * Security
         */
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    }
}
