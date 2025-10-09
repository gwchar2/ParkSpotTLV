using System.Net.Http.Json;
using System.Text.Json;
using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.Contracts.Parking;

namespace ParkSpotTLV.App.Services;

public class ParkingStatusResponse
{
    public bool Status { get; set; }
    public Guid SessionId { get; set; }
    public DateTime ParkingStarted { get; set; }
    public DateTime ParkingUntil { get; set; }
}

// Handles parking-related API operations
public class ParkingService
{
    private readonly AuthenticationService _authService;
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _options;

    public ParkingService(HttpClient http, AuthenticationService authService, JsonSerializerOptions? options = null)
    {
        _http = http;
        _authService = authService;
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<StartParkingResponse> StartParkingAsync(SegmentResponseDTO segResponse, Guid carId, int notificationMinutes, int minParkingTime)
    {
        var startParkingPayload = new StartParkingRequest(
            Segment: segResponse,
            VehicleId: carId,
            NotificationMinutes: notificationMinutes,
            MinParkingTime: minParkingTime
        );


        var response = await _authService.ExecuteWithTokenRefreshAsync(() =>
            _http.PostAsJsonAsync("/parking/start", startParkingPayload, _options));

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to start parking: {(int)response.StatusCode} {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<StartParkingResponse>(_options);
        if (result == null)
        {
            throw new HttpRequestException("Failed to parse parking response");
        }

        return result;
    }

    public async Task StopParkingAsync(Guid sessionId, Guid carId)
    {
        var stopParkingPayload = new
        {
            SessionId = sessionId,
            VehicleId = carId
        };

        var response = await _authService.ExecuteWithTokenRefreshAsync(() =>
            _http.PostAsJsonAsync("/parking/stop", stopParkingPayload, _options));

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to stop parking: {(int)response.StatusCode} {body}");
        }
    }

    public async Task<ParkingStatusResponse?> GetParkingStatusAsync(Guid carId)
    {
        try
        {
            var response = await _authService.ExecuteWithTokenRefreshAsync(() =>
                _http.GetAsync($"/parking/status/{carId}"));

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ParkingStatusResponse>(_options);
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching parking status: {ex.Message}");
            return null;
        }
    }
}
