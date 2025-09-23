using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public class LocalUser
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<LocalVehicle> Vehicles { get; set; } = new List<LocalVehicle>();

    public ICollection<LocalRefreshToken> RefreshTokens { get; set; } = new List<LocalRefreshToken>();

    // Local-only properties for cache management
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;

    // Essential local session properties
    public bool IsLoggedIn { get; set; } = false;

    public DateTime LastSyncAt { get; set; } = DateTime.UtcNow;

    // Local authentication token (not in global model)
    [MaxLength(500)]
    public string? AuthToken { get; set; }
}