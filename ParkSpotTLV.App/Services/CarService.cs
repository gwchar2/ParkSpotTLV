using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using ParkSpotTLV.App.Data.Models;
using ParkSpotTLV.Contracts.Vehicles;

namespace ParkSpotTLV.App.Services;

public class Car
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public CarType Type { get; set; } = CarType.Private;
    public bool HasResidentPermit { get; set; } = false;
    public int ResidentPermitNumber { get; set; } = 0;
    public bool HasDisabledPermit { get; set; } = false;

    public string TypeDisplayName => Type switch
    {
        CarType.Private => "Private",
        CarType.Truck => "Truck",
        _ => "Private"
    };
}

public enum CarType
{
    Private,
    Truck
}

public class CarService
{

    private readonly AuthenticationService _authService;
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _options;

    //private static CarService? _instance;
    //public static CarService Instance => _instance ??= new CarService();

    //private readonly AuthenticationService _authService = AuthenticationService.Instance;

    //private readonly HttpClient _http = new() { BaseAddress = new Uri("http://10.0.2.2:8080/") };

    // private readonly JsonSerializerOptions _options = new() {
    //     PropertyNameCaseInsensitive = true
    // };


    // Dictionary to store cars per user: username -> list of cars
    private readonly Dictionary<string, List<Car>> _userCars = new();

    // private CarService()
    // {
    //     InitializeDemoData();
    // }

    public CarService(HttpClient http, AuthenticationService authService, JsonSerializerOptions? options = null)
    {
        _http = http;                            // already has BaseAddress + Authorization
        _authService = authService;
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        InitializeDemoData();
    }

    private void InitializeDemoData()
    {
        // Add default cars for demo users
        _userCars["admin"] = new List<Car>
        {
            new Car
            {
                // Name = "My Car",
                Type = CarType.Private,
                HasResidentPermit = false,
                ResidentPermitNumber = 0,
                HasDisabledPermit = false
            }
        };

        _userCars["test"] = new List<Car>
        {
            new Car
            {
                // Name = "My Car",
                Type = CarType.Private,
                HasResidentPermit = false,
                ResidentPermitNumber = 0,
                HasDisabledPermit = false
            }
        };

        _userCars["john_doe"] = new List<Car>
        {
            new Car
            {
                // Name = "My Car",
                Type = CarType.Private,
                HasResidentPermit = false,
                ResidentPermitNumber = 0,
                HasDisabledPermit = false
            }
        };
    }

    public async Task<List<Car>> GetUserCarsAsync()
    {
        try
        {
            var response = await _http.GetAsync("/vehicles");
            if (response.IsSuccessStatusCode)
            {
                var cars = await response.Content.ReadFromJsonAsync<List<Car>>(_options);
                return cars ?? new List<Car>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching cars: {ex.Message}");
        }

        // // Fallback to local data
        // if (_authService.IsAuthenticated && _authService.CurrentUsername != null)
        // {
        //     var username = _authService.CurrentUsername;
        //     return _userCars.TryGetValue(username, out var cars) ? cars : new List<Car>();
        // }

        return new List<Car>();
    }

    public async Task<bool> AddCarAsync(Car car)
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUsername == null)
            return false;

        var username = _authService.CurrentUsername;
        var userCars = await GetUserCarsAsync();

        // Check if user already has 5 cars
        if (userCars.Count >= 5)
            return false;

        userCars.Add(car);

        // Update local storage
        if (!_userCars.ContainsKey(username))
            _userCars[username] = new List<Car>();
        _userCars[username] = userCars;

        return true;
    }

    public async Task<bool> RemoveCarAsync(string carId)
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUsername == null)
            return false;

        var userCars = await GetUserCarsAsync();
        var carToRemove = userCars.FirstOrDefault(c => c.Id == carId);

        if (carToRemove != null)
        {
            userCars.Remove(carToRemove);

            // Update local storage
            var username = _authService.CurrentUsername;
            if (_userCars.ContainsKey(username))
                _userCars[username] = userCars;

            return true;
        }

        return false;
    }

    public async Task<Car?> GetCarAsync(string carId)
    {
        var userCars = await GetUserCarsAsync();
        return userCars.FirstOrDefault(c => c.Id == carId);
    }

    public async Task<bool> UpdateCarAsync(Car updatedCar)
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUsername == null)
            return false;

        var userCars = await GetUserCarsAsync();
        var existingCarIndex = userCars.FindIndex(c => c.Id == updatedCar.Id);

        if (existingCarIndex >= 0)
        {
            userCars[existingCarIndex] = updatedCar;

            // Update local storage
            var username = _authService.CurrentUsername;
            if (_userCars.ContainsKey(username))
                _userCars[username] = userCars;

            return true;
        }

        return false;
    }

    public async Task<Car> CreateDefaultCarForUserAsync()
    {
    var me = await _authService.AuthMeAsync();
    System.Diagnostics.Debug.WriteLine($"Adding car for {me.Username}");

    var defaultCarPayload = new VehicleCreateRequest
    (
        Type : Core.Models.VehicleType.Car,
        Name : "def name",
        ResidentZoneCode : null,
        HasDisabledPermit : false
    );

    var response = await _http.PostAsJsonAsync("vehicles", defaultCarPayload, _options);

    if (!response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Failed to create default car: {(int)response.StatusCode} {body}");
    }

    var created = await response.Content.ReadFromJsonAsync<Car>(_options);
    if (created is null)
        throw new InvalidOperationException("Vehicle created but response body was empty.");

    return created;
}


    public void ClearUserCars()
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUsername == null)
            return;

        var username = _authService.CurrentUsername;
        if (_userCars.ContainsKey(username))
        {
            _userCars[username].Clear();
        }
    }

    // Test helper method
    public void Reset()
    {
        _userCars.Clear();
        InitializeDemoData();
    }
}