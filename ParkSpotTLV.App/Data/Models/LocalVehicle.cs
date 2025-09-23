using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public enum VehicleType { Car = 1, Truck = 2 }

public class LocalVehicle
{
    [Key]
    public string Id { get; set; } = string.Empty;

    // Foreign key for Owner relationship (local convenience for querying)
    public string UserId { get; set; } = string.Empty;
    public LocalUser Owner { get; set; } = default!;

    public VehicleType Type { get; set; } = VehicleType.Car;

    public ICollection<LocalPermit> Permits { get; set; } = new List<LocalPermit>();

    // Local cache management properties
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}