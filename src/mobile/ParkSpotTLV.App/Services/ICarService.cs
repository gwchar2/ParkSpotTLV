using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Services;

public interface ICarService
{
    Task<List<Car>> GetUserCarsAsync();
    Task<Car?> GetCarAsync(string carId);
    Task<Guid?> GetPermitAsync(string? carId, int opt);
    Task<Car?> AddCarAsync(Car car);
    Task<bool> UpdateCarAsync(Car updatedCar);
    Task<bool> RemoveCarAsync(string carId);
}
