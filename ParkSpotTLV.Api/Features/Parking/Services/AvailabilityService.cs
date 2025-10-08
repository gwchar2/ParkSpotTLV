using ParkSpotTLV.Api.Features.Parking.Models;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Features.Parking.Services {

    /*
     * Combines calendar and privileged legality into availabity window
     */
    public sealed class AvailabilityService(ITariffCalendarService calendar, ILegalPolicyService privileged) : IAvailabilityService{

        private readonly ITariffCalendarService _calendar = calendar;
        private readonly ILegalPolicyService _privileged = privileged;

        public Availability Compute(ParkingType parkingType, int? segmentZoneCode, Tariff tariff, DateTimeOffset now, PermitSnapshot pov) {

            // Get the legality of the parking spot
            var status = _calendar.GetStatus(tariff, now);                                                      // Gets the status of a parking spot at this time (Active/NotActive)
            var legalPriv = _privileged.IsLegalNow(parkingType, segmentZoneCode, pov, status.ActiveNow);        // Can user park in a privileged spot?

            // The user can not park in this segment at this time
            if (!legalPriv)
                return new Availability(AvailableFrom: status.NextEnd, AvailableUntil: null, NextChange: status.NextEnd);  // Next time it changes, he can maybe park there.
            
            // If parking is paid type, it is available from now, without a limit, but the next change starts at NextStart
            if (parkingType == ParkingType.Paid)
                return new Availability(AvailableFrom: now, AvailableUntil:null, NextChange: status.NextStart);

            // If status is not active (parking will be free at the moment, no matter if ParkingType is Paid) & the permit is an outsider, it will become payed
            if (!status.ActiveNow && segmentZoneCode != pov.ZoneCode)
                return new Availability(AvailableFrom: now, AvailableUntil: status.NextStart, NextChange: status.NextStart);

            // If the permit belongs to the same zone, its always free
            if (pov.ZoneCode == segmentZoneCode)
                return new Availability(AvailableFrom: now, AvailableUntil: null, NextChange: status.NextStart);

            // After hours -> legal until next start
            return new Availability(AvailableFrom: now, AvailableUntil: null, NextChange: status.NextStart);

        }

    }
} 