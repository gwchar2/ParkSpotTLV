using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.Contracts.Parking;

namespace ParkSpotTLV.App.Services;

public interface IParkingService
{
    Task<StartParkingResponse> StartParkingAsync(SegmentResponseDTO segResponse, Guid carId, int minParkingTime);
    Task<ParkingStatusResponse?> GetParkingStatusAsync(Guid carId);
    Task<StopParkingResponse> StopParkingAsync(Guid sessionId, Guid carId);
    Task<int?> GetParkingBudgetRemainingAsync(Guid carId);
}
