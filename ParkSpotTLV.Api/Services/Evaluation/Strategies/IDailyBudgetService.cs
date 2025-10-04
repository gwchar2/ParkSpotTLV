
namespace ParkSpotTLV.Api.Services.Evaluation.Strategies {

    /*
     * Checks budget including 2h free per permit holder
     */
    public interface IDailyBudgetService {
        Task EnsureResetAsync(Guid userId, DateOnly localDate, CancellationToken ct);

        Task<int> GetRemainingMinutesAsync(Guid userId, DateOnly localDate, CancellationToken ct);

    }
}
