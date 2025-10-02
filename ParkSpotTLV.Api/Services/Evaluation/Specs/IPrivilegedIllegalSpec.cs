using ParkSpotTLV.Api.Services.Evaluation.Logic;

namespace ParkSpotTLV.Api.Services.Evaluation.Specs {
    /*
     * Checks if illegal due to privileged restriction right now
     */
    public interface IPrivilegedIllegalSpec {

        bool IsIllegalNow(Availability availability, DateTimeOffset now);

    }
}
