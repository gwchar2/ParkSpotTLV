using ParkSpotTLV.Api.Services.Evaluation.Logic;

namespace ParkSpotTLV.Api.Services.Evaluation.Specs {
    /*
     * Checks if parking is restricted
     */
    public interface IRestrictedSpec {

        bool IsRestrictedNow(Availability availability, DateTimeOffset now);

    }
}
