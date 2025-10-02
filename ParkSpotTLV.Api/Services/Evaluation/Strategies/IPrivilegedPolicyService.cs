using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Strategies {


    /* 
     * Can the segment use a privileged parking spot?
     */

    public interface IPrivilegedPolicyService {

        bool IsLegalNow(ParkingType parkingType, int? segmentZoneCode, PermitPov permitPov, bool ProviligedActiveNow);

    }
}
