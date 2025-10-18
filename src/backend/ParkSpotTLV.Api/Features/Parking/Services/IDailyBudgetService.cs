

using ParkSpotTLV.Infrastructure.Entities;
using ParkSpotTLV.Api.Features.Parking.Models;

namespace ParkSpotTLV.Api.Features.Parking.Services{

    /*
     * Checks budget including 2h free per permit holder
     */
    public interface IDailyBudgetService {

        /*
         * Ensures the daily budget has been reset at time of calculation
         */
        Task EnsureResetAsync(Guid userId, DateOnly localDate, CancellationToken ct);
        /*
         * Retrieves remaining minutes
         */
        Task<int> GetRemainingMinutesAsync(Guid userId, DateOnly localDate, CancellationToken ct);

        /*
         * Consumes a set amount of minutes
         */
        Task ConsumeAsync(Guid vehicleId, DateTimeOffset startLocal, DateTimeOffset endLocal, CancellationToken ct);

        /*
         * Slices the day by boundaries required for calculation
         */
        IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> SliceByAnchorBoundary(DateTimeOffset startLocal, DateTimeOffset endLocal);

        /*
         * Calculates the exact amount of budget used / available
         */
        Task<BudgetCalculationDTO> CalculateAsync(ParkingSession session, CancellationToken ct);

        /*
         * Anchor date
         */
        public DateOnly ToAnchor(DateTimeOffset t);

    }

   
}
