using ParkSpotTLV.Api.Features.Parking.Models;
using ParkSpotTLV.Contracts.Enums;

namespace ParkSpotTLV.Api.Features.Parking.Services {


    /* 
     * Can the segment use a privileged parking spot?
     */

    public interface ILegalPolicyService {

        bool IsLegalNow(ParkingType parkingType, int? segmentZoneCode, PermitSnapshot permitPov, bool ProviligedActiveNow);

    }
}
