using ParkSpotTLV.Core.Models;

namespace ParkSpotTLV.Core.Services;

public class CarService
{
    private static CarService? _instance;
    public static CarService Instance => _instance ??= new CarService();

    private readonly AuthenticationService _authService = AuthenticationService.Instance;

    // Dictionary to store cars per user: username -> list of cars
    private readonly Dictionary<string, List<Car>> _userCars = new();

    private CarService()
    {
        InitializeDemoData();
    }

    private void InitializeDemoData()
    {
        // Add default cars for demo users
        _userCars["admin"] = new List<Car>
        {
            new Car
            {
                Name = "My Car",
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
                Name = "My Car",
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
                Name = "My Car",
                Type = CarType.Private,
                HasResidentPermit = false,
                ResidentPermitNumber = 0,
                HasDisabledPermit = false
            }
        };
    }

    public List<Car> GetUserCars()
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUsername == null)
            return new List<Car>();

        var username = _authService.CurrentUsername;
        if (!_userCars.ContainsKey(username))
        {
            _userCars[username] = new List<Car>();
        }

        return _userCars[username];
    }

    public bool AddCar(Car car)
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUsername == null)
            return false;

        var username = _authService.CurrentUsername;
        var userCars = GetUserCars();

        // Check if user already has 5 cars
        if (userCars.Count >= 5)
            return false;

        userCars.Add(car);
        return true;
    }

    public bool RemoveCar(string carId)
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUsername == null)
            return false;

        var userCars = GetUserCars();
        var carToRemove = userCars.FirstOrDefault(c => c.Id == carId);

        if (carToRemove != null)
        {
            userCars.Remove(carToRemove);
            return true;
        }

        return false;
    }

    public Car? GetCar(string carId)
    {
        var userCars = GetUserCars();
        return userCars.FirstOrDefault(c => c.Id == carId);
    }

    public bool UpdateCar(Car updatedCar)
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUsername == null)
            return false;

        var userCars = GetUserCars();
        var existingCarIndex = userCars.FindIndex(c => c.Id == updatedCar.Id);

        if (existingCarIndex >= 0)
        {
            userCars[existingCarIndex] = updatedCar;
            return true;
        }

        return false;
    }

    public void CreateDefaultCarForUser()
    {
        if (!_authService.IsAuthenticated || _authService.CurrentUsername == null)
            return;

        var defaultCar = new Car
        {
            Name = "My Car",
            Type = CarType.Private,
            HasResidentPermit = false,
            ResidentPermitNumber = 0,
            HasDisabledPermit = false
        };

        AddCar(defaultCar);
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