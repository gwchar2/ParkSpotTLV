
/*
 * User definition for the database
*/

using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Infrastructure.Entities {
    public class User {

        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(64)]
        public string Username { get; set; } = default!;


        [Required, MaxLength(256)]
        public string PasswordHash { get; set; } = default!;

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}
