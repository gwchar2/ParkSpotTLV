using ParkSpotTLV.Api.Features.Parking.Models;

/*
 * Manages everything, query -> availability -> price -> classification -> filtering.
 */
namespace ParkSpotTLV.Api.Features.Parking.Services {
    public interface ISegmentEvaluationService {
        Task<IReadOnlyList<SegmentResultDto>> EvaluateAsync(MapSegmentsRequestDto request, CancellationToken ct);

    }
}
