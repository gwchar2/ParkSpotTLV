using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public class LocalZone
{
    [Key]
    public string Id { get; set; } = string.Empty;

    public int? Code { get; set; }

    [MaxLength(64)]
    public string? Name { get; set; }

    [Required]
    public string GeometryJson { get; set; } = string.Empty;

    public DateTime? LastUpdated { get; set; }

    public DateTime CachedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}