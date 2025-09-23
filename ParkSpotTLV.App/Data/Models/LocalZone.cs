using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.App.Data.Models;

public enum Taarif { City_Center = 1, City_Outskirts = 2 }

public class LocalZone
{
    [Key]
    public string Id { get; set; } = string.Empty;

    public int? Code { get; set; }

    [MaxLength(64)]
    public string? Name { get; set; }

    public Taarif Taarif { get; set; }

    [Required]
    public string GeometryJson { get; set; } = string.Empty;

    public ICollection<LocalStreetSegment> Segments { get; set; } = new List<LocalStreetSegment>();

    public DateTimeOffset? LastUpdated { get; set; }

    // Local cache management properties
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}