using ParkSpotTLV.Api.Services.Evaluation.Contracts;

namespace ParkSpotTLV.Api.Services.Evaluation.Query {


    /*
     * This interface returns SNAPSHOTS for segments intersecting with the given BBOX from front end.
     * Only fields that are required for evaluation are loaded! 
     */
    public interface ISegmentQueryService {

        Task<IReadOnlyList<SegmentSnapshot>> GetViewportAsync (
            double minLon, double minLat, double maxLon, double maxLat, double centerLon, double centerLat, CancellationToken cancellationToken);

    }
}