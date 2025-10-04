using ParkSpotTLV.Contracts.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkSpotTLV.Infrastructure.Entities {

    public class Permit {

        /* 
         * Owner and dry details 
         */
        public Guid Id { get; set; } = Guid.NewGuid();
        public Vehicle Vehicle { get; set; } = default!;
        public Guid VehicleId { get; set; }

        /* 
         * Permit Type 
         */
        public PermitType Type { get; set; } = PermitType.Default;

        /* 
         * Zone Code for residential permit 
         */
        public int? ZoneCode { get; set; } = 0;
        public Zone? Zone { get; set; } 

        /* 
         * Concurrency 
         */
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] public uint Xmin { get; private set; }
        public DateTimeOffset? LastUpdated { get; set; } = DateTimeOffset.Now;                 // Last update on user
    }
}
