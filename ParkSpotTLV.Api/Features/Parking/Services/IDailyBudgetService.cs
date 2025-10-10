

using ParkSpotTLV.Infrastructure.Entities;
using ParkSpotTLV.Api.Features.Parking.Models;

namespace ParkSpotTLV.Api.Features.Parking.Services{

    /*
     * Checks budget including 2h free per permit holder
     */
    public interface IDailyBudgetService {
        Task EnsureResetAsync(Guid userId, DateOnly localDate, CancellationToken ct);
        Task<int> GetRemainingMinutesAsync(Guid userId, DateOnly localDate, CancellationToken ct);
        Task ConsumeAsync(Guid vehicleId, DateTimeOffset startLocal, DateTimeOffset endLocal, CancellationToken ct);
        IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> SliceByAnchorBoundary(DateTimeOffset startLocal, DateTimeOffset endLocal);
        Task<BudgetCalculationDTO> CalculateAsync(ParkingSession session, CancellationToken ct);
        public DateOnly ToAnchor(DateTimeOffset t);

    }

   
}
