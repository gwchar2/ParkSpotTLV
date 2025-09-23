using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public class LocalRefreshToken
{
    [Key]
    public string Id { get; set; } = string.Empty;

    // Foreign key for User relationship (local convenience for querying)
    public string UserId { get; set; } = string.Empty;
    public LocalUser User { get; set; } = default!;

    [Required, MaxLength(500)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    [MaxLength(500)]
    public string? ReplacedByTokenHash { get; set; }

    // Local cache management properties
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}