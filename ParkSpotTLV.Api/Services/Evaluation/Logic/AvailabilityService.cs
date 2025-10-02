using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Api.Services.Evaluation.Strategies;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Logic {

    /*
     * Combines calendar and privileged legality into availabity window
     */
    public sealed class AvailabilityService(ITariffCalendarService calendar, IPrivilegedPolicyService privileged) : IAvailabilityService{

        private readonly ITariffCalendarService _calendar = calendar;
        private readonly IPrivilegedPolicyService _privileged = privileged;

        public Availability Compute(ParkingType parkingType, int? segmentZoneCode, Tariff tariff, DateTimeOffset now, PermitPov pov) {

            // Get the legality
            var status = _calendar.GetStatus(tariff, now);
            var legalPriv = _privileged.IsLegalNow(parkingType, segmentZoneCode, pov, status.ActiveNow);


            // Illegal now if privileged active and snapshot is for outsider -> next legal at window end
            if (!legalPriv)
                return new Availability(status.NextEnd, null, status.NextEnd);

            // Inside active window -> legal until window end
            if (status.ActiveNow)
                return new Availability(null, status.NextEnd, status.NextEnd);

            // After hours -> legal until next start
            return new Availability(null, status.NextStart, status.NextStart);

        }

    }
}
