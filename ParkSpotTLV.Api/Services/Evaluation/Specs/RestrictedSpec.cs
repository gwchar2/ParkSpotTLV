using ParkSpotTLV.Api.Services.Evaluation.Logic;

namespace ParkSpotTLV.Api.Services.Evaluation.Specs {
    /*
     * Checks if illegal due to privileged restriction right now
     */
    public sealed class RestrictedSpec : IRestrictedSpec {
        public bool IsRestrictedNow(Availability availability, DateTimeOffset now) {
            return availability.AvailableFrom is DateTimeOffset availFrom 
                && availFrom > now && availability.AvailableUntil is null;
        }

    }
}
