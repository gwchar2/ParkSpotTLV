using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Maui.Devices.Sensors;
using ParkSpotTLV.Contracts.Vehicles;
using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.Core.Models;

namespace ParkSpotTLV.App.Services;

public class MapService
{
    private readonly AuthenticationService _authService;
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _options;

    public MapService(HttpClient http, AuthenticationService authService, JsonSerializerOptions? options = null)
    {
        _http = http;                            // already has BaseAddress + Authorization
        _authService = authService;
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    }

    public async Task<GetMapSegmentsResponse?> getSegmentsAsync(int minParkingTime, Guid activePermit, Location? center)
    {
        // double centerLon = center.Longitude;
        // double centerLat = center.Latitude;

        var request = new GetMapSegmentsRequest(
            ActivePermitId: activePermit,
            MinLon: 34.7841160,
            MinLat: 32.0914800,
            MaxLon: 34.7908710,
            MaxLat: 32.0950490,
            CenterLon: 34.7877809,
            CenterLat: 32.0928775,
            Now: DateTimeOffset.Now,
            MinParkingTime: minParkingTime,
            ShowFree: true,
            ShowPaid: true,
            ShowLimited: true,
            ShowAll: false
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
