using ParkSpotTLV.Api.Features.Parking.Models;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Features.Parking.Services {

    /*
     * Can a user park in this segment at this specific time?
     */

    public class LegalPolicyService : ILegalPolicyService {

        public bool IsLegalNow(ParkingType parkingType, int? segmentZoneCode, PermitSnapshot pov, bool ActiveNow) {

            if (parkingType != ParkingType.Privileged)                              return true;            // If parking type is not privileged, continue
            if (!ActiveNow)                                                         return true;            // If parking spot is not active now, it is free for all

            if (pov.Type == PermitSnapType.Disability)                              return true;            // If permit is a disability
            if (pov.Type == PermitSnapType.Zone && pov.ZoneCode == segmentZoneCode) return true;            // If current zone permit == zone code

            return false;

        }
    }
}
