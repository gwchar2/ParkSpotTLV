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
        public int? ZoneCode { get; set; }
        public Zone? Zone { get; set; }

        /* 
         * Parking information - need to add to config if want to use them!
         
        public DateTimeOffset? ParkingStartedAtUtc { get; set; }            // Parking started at 
        public DateTimeOffset? FreeParkingUntilUtc { get; set; }            // Free parking available until
        public TimeSpan FreeParkingBudget { get; set; } = TimeSpan.Zero;    // Total free Parking budget
        public DateOnly? FreeBudgetLastResetDate { get; set; }              // When was the free parking timer last reset
        public bool CurrentlyParking { get; set; } = false;                 // Is user currently parked?
        public bool CurrentlyParkingForFree { get; set; } = false;          // Is user currently parking for free?
        */



        /* 
         * Concurrency 
         */
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)] public uint Xmin { get; private set; }
        public DateTimeOffset? LastUpdated { get; set; } = DateTimeOffset.Now;                 // Last update on user
    }
}
