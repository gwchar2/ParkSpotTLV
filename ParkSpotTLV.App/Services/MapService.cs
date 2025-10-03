using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
//using ParkSpotTLV.App.Data.Models;
using ParkSpotTLV.Contracts.Vehicles;
using ParkSpotTLV.Core.Models;
//using Xamarin.Google.Crypto.Tink.Proto;

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

    public async Task<Object> getSegmentsAsync()
    {
        // TODO: Replace with actual API call
        await Task.Delay(100); // Simulate network delay

        var mockGeoJson = new
        {
            type = "FeatureCollection",
            features = new[]
            {
                new
                {
                    type = "Feature",
                    geometry = new
                    {
                        type = "LineString",
                        coordinates = new[]
                        {
                            new[] { 34.7818, 32.0853 }, // Tel Aviv coordinates
                            new[] { 34.7828, 32.0863 }
                        }
                    },
                    properties = new
                    {
                        id = "segment_1",
                        name = "Sample Parking Segment 1"
                    }
                },
                new
                {
                    type = "Feature",
                    geometry = new
                    {
                        type = "LineString",
                        coordinates = new[]
                        {
                            new[] { 34.7738, 32.0753 },
                            new[] { 34.7748, 32.0763 }
                        }
                    },
                    properties = new
                    {
                        id = "segment_2",
                        name = "Sample Parking Segment 2"
                    }
                }
            }
        };

        return mockGeoJson;
    }
}