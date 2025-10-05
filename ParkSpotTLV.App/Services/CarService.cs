using System.Text.Json;
using System.Net.Http.Json;
using ParkSpotTLV.Contracts.Vehicles;
using ParkSpotTLV.Contracts.Permits;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Core.Models;

namespace ParkSpotTLV.App.Services;

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
        // Parse the string Type to CarType enum
        var carType = vehicleResponse.Type.ToLower() == "truck" ? CarType.Truck : CarType.Private;

        return new Car
        {
            Id = vehicleResponse.Id.ToString(),
            Name = vehicleResponse.Name,
            Type = carType,
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
            // Convert CarType to VehicleType
            var vehicleType = car.Type == CarType.Truck ? VehicleType.Truck : VehicleType.Private;

            var newCarPayload = new VehicleCreateRequest
            (
            Type : vehicleType,
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

    private async Task UpdateResidentPermitAsync(Guid permitId, Car updatedCar)
    {
        // Get current permit to retrieve RowVersion
        var getPermitResponse = await _authService.ExecuteWithTokenRefreshAsync(() => _http.GetAsync($"/permits/{permitId}"));

        if (!getPermitResponse.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to get resident permit: {getPermitResponse.StatusCode}");

        var currentPermit = await getPermitResponse.Content.ReadFromJsonAsync<PermitResponse>(_options);
        if (currentPermit == null)
            throw new InvalidOperationException("Failed to retrieve current permit data");
        
        // Update the permit
        var updatePermitPayload = new PermitUpdateRequest(
            RowVersion: currentPermit.RowVersion,
            Type: PermitType.ZoneResident,
            ZoneCode: updatedCar.HasResidentPermit ? updatedCar.ResidentPermitNumber : null
        );

        var updatePermitResponse = await _authService.ExecuteWithTokenRefreshAsync(() =>
            _http.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/permits/{permitId}")
            {
                Content = JsonContent.Create(updatePermitPayload, options: _options)
            }));

        if (!updatePermitResponse.IsSuccessStatusCode)
        {
            var body = await updatePermitResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to update resident permit: {(int)updatePermitResponse.StatusCode} {body}");
        }
    }

    private async Task AddResidentPermitAsync(Car car)
    {
        var createPermitPayload = new PermitCreateRequest(
            Type: PermitType.ZoneResident,
            VehicleId: Guid.Parse(car.Id),
            HasDisabledPermit: false,
            ResidentZoneCode: car.ResidentPermitNumber
        );

        var response = await _authService.ExecuteWithTokenRefreshAsync(() =>
            _http.PostAsJsonAsync("/permits", createPermitPayload, _options));

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to add resident permit: {(int)response.StatusCode} {body}");
        }
    }

    private async Task RemoveResidentPermitAsync(Guid permitId)
    {
        // Get current permit to retrieve RowVersion
        var getPermitResponse = await _authService.ExecuteWithTokenRefreshAsync(() => _http.GetAsync($"/permits/{permitId}"));

        if (!getPermitResponse.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to get resident permit: {getPermitResponse.StatusCode}");

        var currentPermit = await getPermitResponse.Content.ReadFromJsonAsync<PermitResponse>(_options);
        if (currentPermit == null)
            throw new InvalidOperationException("Failed to retrieve current permit data");

        var deletePermitPayload = new PermitDeleteRequest(
            Id: currentPermit.PermitId,
            RowVersion: currentPermit.RowVersion
        );

        var deleteResponse = await _authService.ExecuteWithTokenRefreshAsync(() =>
            _http.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/permits/{permitId}")
            {
                Content = JsonContent.Create(deletePermitPayload, options: _options)
            }));

        if (!deleteResponse.IsSuccessStatusCode)
        {
            var body = await deleteResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to delete resident permit: {(int)deleteResponse.StatusCode} {body}");
        }
    }

    private async Task AddDisabledPermitAsync(Car car)
    {
        var createPermitPayload = new PermitCreateRequest(
            Type: PermitType.Disability,
            VehicleId: Guid.Parse(car.Id),
            HasDisabledPermit: true,
            ResidentZoneCode: null
        );

        var response = await _authService.ExecuteWithTokenRefreshAsync(() =>
            _http.PostAsJsonAsync("/permits", createPermitPayload, _options));

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to add disabled permit: {(int)response.StatusCode} {body}");
        }
    }

    private async Task RemoveDisabledPermitAsync(Guid permitId)
    {
        // Get current permit to retrieve RowVersion
        var getPermitResponse = await _authService.ExecuteWithTokenRefreshAsync(() => _http.GetAsync($"/permits/{permitId}"));

        if (!getPermitResponse.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to get disabled permit: {getPermitResponse.StatusCode}");

        var currentPermit = await getPermitResponse.Content.ReadFromJsonAsync<PermitResponse>(_options);
        if (currentPermit == null)
            throw new InvalidOperationException("Failed to retrieve current permit data");

        var deletePermitPayload = new PermitDeleteRequest(
            Id: currentPermit.PermitId,
            RowVersion: currentPermit.RowVersion
        );

        var deleteResponse = await _authService.ExecuteWithTokenRefreshAsync(() =>
            _http.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/permits/{permitId}")
            {
                Content = JsonContent.Create(deletePermitPayload, options: _options)
            }));

        if (!deleteResponse.IsSuccessStatusCode)
        {
            var body = await deleteResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to delete disabled permit: {(int)deleteResponse.StatusCode} {body}");
        }
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


            // Convert CarType to VehicleType
            var vehicleType = updatedCar.Type == CarType.Truck ? VehicleType.Truck : VehicleType.Private;

            // Create update request with RowVersion and changes
            var updatePayload = new VehicleUpdateRequest
            (
                RowVersion: currentVehicle.RowVersion,
                Type: vehicleType,
                Name: updatedCar.Name
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

            // Handle resident permit changes
            bool hadResidentPermit = currentVehicle.ResidentZoneCode.HasValue;
            bool hasResidentPermit = updatedCar.HasResidentPermit;
            bool zoneCodeChanged = currentVehicle.ResidentZoneCode != updatedCar.ResidentPermitNumber;

            if (hadResidentPermit && !hasResidentPermit)
            {
                // Resident permit removed - delete it
                if (currentVehicle.ResidencyPermitId.HasValue)
                {
                    await RemoveResidentPermitAsync(currentVehicle.ResidencyPermitId.Value);
                }
            }
            else if (!hadResidentPermit && hasResidentPermit)
            {
                // Resident permit added
                await AddResidentPermitAsync(updatedCar);
            }
            else if (hadResidentPermit && hasResidentPermit && zoneCodeChanged)
            {
                // Zone number changed - update the permit
                if (currentVehicle.ResidencyPermitId.HasValue)
                {
                    await UpdateResidentPermitAsync(currentVehicle.ResidencyPermitId.Value, updatedCar);
                }
            }

            // Handle disabled permit changes
            if (currentVehicle.DisabledPermit != updatedCar.HasDisabledPermit)
            {
                if (updatedCar.HasDisabledPermit)
                {
                    // Disabled permit added
                    await AddDisabledPermitAsync(updatedCar);
                }
                else
                {
                    // Disabled permit removed
                    if (currentVehicle.DisabilityPermitId.HasValue)
                    {
                        await RemoveDisabledPermitAsync(currentVehicle.DisabilityPermitId.Value);
                    }
                }
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