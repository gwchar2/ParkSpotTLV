using ParkSpotTLV.Api.Services.Evaluation.Logic;

namespace ParkSpotTLV.Api.Services.Evaluation.Specs {
    /*
     * Checks if becomes paid/privileged within threshold 
     */
    public interface ILimitedSpec {
        bool IsLimited(DateTimeOffset now, Availability availability, int limitedThresholdMinutes);
    }
}
