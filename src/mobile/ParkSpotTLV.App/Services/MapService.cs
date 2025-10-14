using System.Net.Http.Json;
using System.Text.Json;
using ParkSpotTLV.Contracts.Map;

namespace ParkSpotTLV.App.Services;

/*
* Handles map-related API operations including fetching parking segments.
* Communicates with backend API for map data with automatic token refresh.
*/
public class MapService : IMapService
{
    private readonly HttpClient _http;
    private readonly IAuthenticationService _authService;
    private readonly ILocalDataService _localDataService;
    private readonly JsonSerializerOptions _options;

    /*
    * Initializes the map service with HTTP client and authentication.
    */
    public MapService(HttpClient http, IAuthenticationService authService, ILocalDataService localDataService, JsonSerializerOptions? options = null)
    {
        _http = http;
        _authService = authService;
        _localDataService = localDataService;
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /*
    * Fetches parking segments from API for specified map bounds and parameters.
    * Returns segments response with parking availability data, null on error.
    */
    public async Task<GetMapSegmentsResponse?> GetSegmentsAsync(Guid activePermit,
                                                                 double minLon,
                                                                 double minLat,
                                                                 double maxLon,
                                                                 double maxLat,
                                                                 double centerLon,
                                                                 double centerLat,
                                                                 DateTimeOffset dateTime,
                                                                 int minParkingTime)
    {
        var request = new GetMapSegmentsRequest(
            ActivePermitId: activePermit,
            MinLon: minLon,
            MinLat: minLat,
            MaxLon: maxLon,
            MaxLat: maxLat,
            CenterLon: centerLon,
            CenterLat: centerLat,
            Now: dateTime,
            MinParkingTime: minParkingTime
        );

        try
        {
            var response = await _authService.ExecuteWithTokenRefreshAsync(() =>
                _http.PostAsJsonAsync("/map/segments", request, _options));

            if (response.IsSuccessStatusCode)
            {
                var getMapSegmentsResponse = await response.Content.ReadFromJsonAsync<GetMapSegmentsResponse>(_options);
                return getMapSegmentsResponse;
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching segments: {ex.Message}");
        }
        return null;
    }
}
