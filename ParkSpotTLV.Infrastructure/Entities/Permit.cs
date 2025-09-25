using ParkSpotTLV.Core.Models;

namespace ParkSpotTLV.Infrastructure.Entities {

    public class Permit {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Vehicle? Vehicle { get; set; } = default!;
        public Guid VehicleId { get; set; }
        public PermitType Type { get; set; } = PermitType.Default;
        public int? ZoneCode { get; set; }
        public Zone? Zone { get; set; }
        public DateOnly? ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
