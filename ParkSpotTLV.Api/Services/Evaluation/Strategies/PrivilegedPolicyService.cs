using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Strategies {

    /*
     * Can segment use privliged parking spot ?
     */

    public class PrivilegedPolicyService : IPrivilegedPolicyService {

        public bool IsLegalNow(ParkingType parkingType, int? segmentZoneCode, PermitPov pov, bool priviledgeActiveNow) {

            if (parkingType != ParkingType.Privileged)                              return true;            // If parking type is not privileged, continue
            if (!priviledgeActiveNow)                                               return true;            // If parking is not privileged hours (free for all)

            if (pov.Type == PermitPovType.Disability)                               return true;            // If permit is a disability
            if (pov.Type == PermitPovType.Zone && pov.ZoneCode == segmentZoneCode)  return true;            // If current zone permit == zone code

            return false;

        }
    }
}
