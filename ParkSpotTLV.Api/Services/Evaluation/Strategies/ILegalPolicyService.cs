using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Services.Evaluation.Strategies {


    /* 
     * Can the segment use a privileged parking spot?
     */

    public interface ILegalPolicyService {

        bool IsLegalNow(ParkingType parkingType, int? segmentZoneCode, PermitSnapshot permitPov, bool ProviligedActiveNow);

    }
}
