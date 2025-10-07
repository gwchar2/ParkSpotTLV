using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Maui.Devices.Sensors;
using ParkSpotTLV.Contracts.Vehicles;
using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.Core.Models;
using ParkSpotTLV.App.Data.Services;
namespace ParkSpotTLV.App.Services;

public class MapService
{
    private readonly AuthenticationService _authService;
    private readonly ILocalDataService _localDataService;
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _options;

    public MapService(HttpClient http, AuthenticationService authService,ILocalDataService localDataService, JsonSerializerOptions? options = null)
    {
        _http = http;                            // already has BaseAddress + Authorization
        _authService = authService;
        _localDataService = localDataService;
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    }

    public async Task<GetMapSegmentsResponse?> getSegmentsAsync(Guid activePermit,
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
