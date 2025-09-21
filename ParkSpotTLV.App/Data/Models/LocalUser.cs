using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public class LocalUser
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AuthToken { get; set; }

    [MaxLength(500)]
    public string? RefreshToken { get; set; }

    public DateTime? TokenExpiry { get; set; }

    public bool IsLoggedIn { get; set; } = false;

    public DateTime LastLogin { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSyncAt { get; set; } = DateTime.UtcNow;
}