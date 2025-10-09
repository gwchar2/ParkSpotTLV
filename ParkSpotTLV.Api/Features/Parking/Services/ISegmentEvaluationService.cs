using ParkSpotTLV.Api.Features.Parking.Models;

/*
 * Manages everything, query -> availability -> price -> classification -> filtering.
 */

namespace ParkSpotTLV.Api.Features.Parking.Services {
    public interface ISegmentEvaluationService {
        Task<IReadOnlyList<SegmentResult>> EvaluateAsync(MapSegmentsRequest request, CancellationToken ct);

    }
}
