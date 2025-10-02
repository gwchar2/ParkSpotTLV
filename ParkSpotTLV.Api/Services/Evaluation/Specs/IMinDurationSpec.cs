using ParkSpotTLV.Api.Services.Evaluation.Logic;

namespace ParkSpotTLV.Api.Services.Evaluation.Specs {
    /*
     * Keeps only segments that remain legal for at least minDuration minutes.
     */
    public interface IMinDurationSpec {

        bool IsSatisfied(Availability availability, DateTimeOffset now, int minDurationMinutes);

    }
}
