

namespace ParkSpotTLV.Api.Services.Evaluation.Strategies {
    /*
     *  Handles the daily budget of a permit holder
     */
    public sealed class DailyBudgetService : IDailyBudgetService {

        /* MUST IMPLEMENT */
        public Task EnsureResetAsync(Guid userId, DateOnly localDate, CancellationToken ct) => Task.CompletedTask;

        public Task<int> GetRemainingMinutesAsync(Guid userId, DateOnly localDate, CancellationToken ct) => Task.FromResult(120);
    }
}
