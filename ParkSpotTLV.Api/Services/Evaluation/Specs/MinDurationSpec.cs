using ParkSpotTLV.Api.Services.Evaluation.Logic;

namespace ParkSpotTLV.Api.Services.Evaluation.Specs {

    /*
     * Keeps only segments that remain legal for at least minDuration minutes.
     */

    public class MinDurationSpec : IMinDurationSpec {

        public bool IsSatisfied(Availability availability, DateTimeOffset now, int minDurationMinutes) {

            if (availability.AvailableFrom is DateTimeOffset availfrom && availfrom > now && availability.AvailableUntil is null)
                return false;               // Illegal at the moment

            if (availability.AvailableUntil is null)
                return true;                // unlimited

            var minutes = (int)Math.Floor((availability.AvailableUntil.Value - now).TotalMinutes);
            return minutes >= minDurationMinutes;           // Returns if has more minutes available than required


        }
    }
}
