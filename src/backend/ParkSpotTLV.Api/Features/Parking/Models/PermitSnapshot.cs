namespace ParkSpotTLV.Api.Features.Parking.Models {


    /*
     * Users POV for evaluation
     */

    public enum PermitSnapType { None, Zone, Disability }

    public sealed class PermitSnapshot {

        public PermitSnapType Type { get; init; }
        public int? ZoneCode { get; init; } = -1;
        public Guid? VehicleId { get; init; }

    }
}
