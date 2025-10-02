using ParkSpotTLV.Api.Services.Evaluation.Logic;

namespace ParkSpotTLV.Api.Services.Evaluation.Specs {
    /*
     * Checks if illegal due to privileged restriction right now
     */
    public sealed class PrivilegedIllegalSpec {
        public bool IsIllegalNow(Availability availability, DateTimeOffset now) {


            return availability.AvailableFrom is DateTimeOffset availfrom 
                && availfrom > now && availability.AvailableUntil is null;
        }

    }
}
