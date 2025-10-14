using ParkSpotTLV.Contracts.Map;

namespace ParkSpotTLV.App.Services;

public interface IMapService
{
    Task<GetMapSegmentsResponse?> GetSegmentsAsync(Guid activePermit,
                                                    double minLon,
                                                    double minLat,
                                                    double maxLon,
                                                    double maxLat,
                                                    double centerLon,
                                                    double centerLat,
                                                    DateTimeOffset dateTime,
                                                    int minParkingTime);
}
