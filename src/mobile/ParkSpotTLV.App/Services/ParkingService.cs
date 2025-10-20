using System.Net.Http.Json;
using System.Text.Json;
using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.Contracts.Parking;

namespace ParkSpotTLV.App.Services;

public class ParkingStatusResponse
{
    public bool Status { get; set; }
    public Guid SessionId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Name { get; set; } = "";
}

// Handles parking-related API operations
public class ParkingService : IParkingService
{
    private readonly HttpClient _http;
    private readonly IAuthenticationService _authService;
    private readonly JsonSerializerOptions _options;


    private record BudgetRemainingResponse(int TimeRemaining);

    /*
    * Initializes the parking service with logging, HTTP client and authentication.
    */
    public ParkingService(HttpClient http, IAuthenticationService authService, JsonSerializerOptions? options = null)
    {
        _http = http;
        _authService = authService;
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /*
    * Starts a parking session for specified car at specified segment.
    * Returns parking session details including session ID.
    */
    public async Task<StartParkingResponse> StartParkingAsync(SegmentResponseDTO segResponse, Guid carId, int minParkingTime)
    {
        var startParkingPayload = new StartParkingRequest(
            Segment: segResponse,
            VehicleId: carId,
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

    /*
    * Gets current parking status for specified car.
    * Returns status including whether car is currently parked and session details.
    */
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

    /*
    * Stops an active parking session.
    * Ends parking for specified session and car.
    */
    public async Task<StopParkingResponse> StopParkingAsync(Guid sessionId, Guid carId)
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

        var dto = await response.Content.ReadFromJsonAsync<StopParkingResponse>(_options);
        return dto is null ? throw new HttpRequestException("Failed to stop parking: empty response body.") : dto;
    }

    /*
    * Gets remaining free parking budget in minutes for residential permit.
    * Returns minutes remaining for the day, null on error.
    */
    public async Task<int?> GetParkingBudgetRemainingAsync(Guid carId)
    {
        var response = await _authService.ExecuteWithTokenRefreshAsync(() =>
            _http.GetAsync($"/parking/budget-remaining/{carId}"));

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to get budget remaining: {(int)response.StatusCode} {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<BudgetRemainingResponse>(_options);
        return result?.TimeRemaining;
    }

    
}
