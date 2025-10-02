using ParkSpotTLV.Api.Services.Evaluation.Logic;

namespace ParkSpotTLV.Api.Services.Evaluation.Specs {
    public sealed class LimitedSpec : ILimitedSpec{

        public bool IsLimited(DateTimeOffset now, Availability availability, int limitedThresholdMinutes) {

            if (availability.NextChange is not DateTimeOffset nextchng) return false;   // Just checking if proper input

            // Limit turns to payed / privileged within the threshold, so calculate if the threshold will be 'broken'
            var minutes = (int)Math.Floor((nextchng - now).TotalMinutes);
            return minutes >= 0 && minutes <= limitedThresholdMinutes;
        }

    }
}
