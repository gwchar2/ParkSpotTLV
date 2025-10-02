using ParkSpotTLV.Api.Services.Evaluation.Contracts;

/*
 * Manages everything, query -> availability -> price -> classification -> filtering.
 */

namespace ParkSpotTLV.Api.Services.Evaluation.Facade {
    public interface IMapSegmentsEvaluator {
        Task<IReadOnlyList<SegmentResult>> EvaluateAsync(MapSegmentsRequest request, CancellationToken ct);

    }
}
