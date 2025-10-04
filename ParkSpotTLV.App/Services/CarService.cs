using System.Text.Json;
using System.Net.Http.Json;
//using ParkSpotTLV.App.Data.Models;
using ParkSpotTLV.Contracts.Vehicles;
using ParkSpotTLV.Core.Models;
//using Xamarin.Google.Crypto.Tink.Proto;

namespace ParkSpotTLV.App.Services;

public class Car
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public VehicleType Type { get; set; } = VehicleType.Private;
    public bool HasResidentPermit { get; set; } = false;
    public int ResidentPermitNumber { get; set; } = 0;
    public bool HasDisabledPermit { get; set; } = false;

    public string TypeDisplayName => Type switch
    {
        VehicleType.Private => "Private",
        VehicleType.Truck => "Truck",
        _ => "Private"
    };
}


public class CarService
{

    private readonly AuthenticationService _authService;
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _options;
    private readonly Dictionary<string, List<Car>> _userCars = new();

    public CarService(HttpClient http, AuthenticationService authService, JsonSerializerOptions? options = null)
    {
        _http = http;                            // already has BaseAddress + Authorization
        _authService = authService;
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    }

    private static Car MapVehicleResponseToCar(VehicleResponse vehicleResponse)
    {
        return new Car
        {
            Id = vehicleResponse.Id.ToString(),
            Name = vehicleResponse.Name,
            Type = vehicleResponse.Type,
            HasResidentPermit = vehicleResponse.ResidentZoneCode.HasValue,
            ResidentPermitNumber = vehicleResponse.ResidentZoneCode ?? 0,
            HasDisabledPermit = vehicleResponse.DisabledPermit
        };
    }

    public async Task<List<Car>> GetUserCarsAsync()
    {
        try
        {
            var response = await _authService.ExecuteWithTokenRefreshAsync(() => _http.GetAsync("/vehicles"));
            if (response.IsSuccessStatusCode)
            {
                var vehicleResponses = await response.Content.ReadFromJsonAsync<List<VehicleResponse>>(_options);
                return vehicleResponses?.Select(MapVehicleResponseToCar).ToList() ?? new List<Car>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching cars: {ex.Message}");
        }

        return new List<Car>();
    }

    public async Task<Car?> AddCarAsync(Car car)
    {
        try
        {
            var newCarPayload = new VehicleCreateRequest
            (
            Type : car.Type, // Core.Models.VehicleType.(car.Type),
            Name : car.Name,
            ResidentZoneCode : car.HasResidentPermit && car.ResidentPermitNumber != 0 ? car.ResidentPermitNumber : null,
            HasDisabledPermit : car.HasDisabledPermit
            );

            var userCars = await GetUserCarsAsync();

            var response = await _authService.ExecuteWithTokenRefreshAsync(() => _http.PostAsJsonAsync("vehicles", newCarPayload, _options));

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to create vehicle: {(int)response.StatusCode} {body}");
            }

            var createdResponse = await response.Content.ReadFromJsonAsync<VehicleResponse>(_options);
            if (createdResponse is null)
                throw new InvalidOperationException("Vehicle created but response body was empty.");

            return MapVehicleResponseToCar(createdResponse);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error adding car: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> RemoveCarAsync(string carId)
    {
        var me = await _authService.AuthMeAsync();

        // First, get the vehicle to retrieve its RowVersion
        var getResponse = await _authService.ExecuteWithTokenRefreshAsync(() => _http.GetAsync($"vehicles/{carId}"));

        if (!getResponse.IsSuccessStatusCode)
        {
            var body = await getResponse.Content.ReadAsStringAsync();
            return false;
        }

        var vehicle = await getResponse.Content.ReadFromJsonAsync<VehicleResponse>(_options);
        if (vehicle is null)
            return false;

        var deletePayload = new VehicleDeleteRequest
        (
            RowVersion: vehicle.RowVersion
        );

        var response = await _authService.ExecuteWithTokenRefreshAsync(() => _http.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"vehicles/{carId}")
        {
            Content = JsonContent.Create(deletePayload, options: _options)
        }));

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            return false;
        }

        return true;
    }

    public async Task<Car?> GetCarAsync(string carId)
    {
        try
        {
            var response = await _authService.ExecuteWithTokenRefreshAsync(() => _http.GetAsync($"/vehicles/{carId}"));
            if (response.IsSuccessStatusCode)
            {
                var vehicleResponse = await response.Content.ReadFromJsonAsync<VehicleResponse>(_options);
                return vehicleResponse != null ? MapVehicleResponseToCar(vehicleResponse) : null;
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching car: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> UpdateCarAsync(Car updatedCar)
    {
        try
        {
            // First, get the current vehicle to retrieve its RowVersion
            var getResponse = await _authService.ExecuteWithTokenRefreshAsync(() => _http.GetAsync($"/vehicles/{updatedCar.Id}"));

            if (!getResponse.IsSuccessStatusCode)
                return false;

            var currentVehicle = await getResponse.Content.ReadFromJsonAsync<VehicleResponse>(_options);
            if (currentVehicle == null)
                return false;

            // Create update request with RowVersion and changes
            var updatePayload = new VehicleUpdateRequest
            (
                RowVersion: currentVehicle.RowVersion,
                Type: updatedCar.Type,
                Name: updatedCar.Name,
                ResidentZoneCode: updatedCar.HasResidentPermit && updatedCar.ResidentPermitNumber != 0 ? updatedCar.ResidentPermitNumber : null,
                DisabledPermit: updatedCar.HasDisabledPermit
            );

            // Send PATCH request to update the vehicle
            var response = await _authService.ExecuteWithTokenRefreshAsync(() => _http.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/vehicles/{updatedCar.Id}")
            {
                Content = JsonContent.Create(updatePayload, options: _options)
            }));

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to update vehicle: {(int)response.StatusCode} {body}");
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating car: {ex.Message}");
            throw; // Re-throw to let the UI handle the specific error
        }
    }


}