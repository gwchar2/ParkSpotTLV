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
    private readonly LocalDataService _localDataService;
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _options;

    public MapService(HttpClient http, AuthenticationService authService,LocalDataService localDataService, JsonSerializerOptions? options = null)
    {
        _http = http;                            // already has BaseAddress + Authorization
        _authService = authService;
        _localDataService = localDataService;
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    }

    public async Task<GetMapSegmentsResponse?> getSegmentsAsync(int minParkingTime, Guid activePermit, Location? center)
    {
        // double centerLon = center.Longitude;
        // double centerLat = center.Latitude;, 
        var request = new GetMapSegmentsRequest(
            ActivePermitId: activePermit,
            MinLon: 34.7799910,         // sw.Longitude
            MinLat: 32.0835150,         // sw.Latitude
            MaxLon: 34.7865450,         // ne.Longitude
            MaxLat: 32.0867230,         // ne.Latitude
            CenterLon: 34.7877809,
            CenterLat: 32.0928775,
            Now: DateTimeOffset.Now,
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
