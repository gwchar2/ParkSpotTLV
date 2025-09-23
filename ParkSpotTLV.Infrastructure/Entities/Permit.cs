

namespace ParkSpotTLV.Infrastructure.Entities {

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
