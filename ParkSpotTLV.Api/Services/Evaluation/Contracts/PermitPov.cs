namespace ParkSpotTLV.Api.Services.Evaluation.Contracts {


    /*
     * Users POV for evaluation
     */

    public enum PermitPovType { None, Zone, Disability }

    public sealed class PermitPov {

        public PermitPovType Type { get; init; }
        public int? ZoneCode { get; init; } 
        public Guid? UserId { get; init; }

    }
}
