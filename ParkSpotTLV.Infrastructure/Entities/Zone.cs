using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;

/*
 * Zone definition for the database
*/


namespace ParkSpotTLV.Infrastructure.Entities {

    public class Zone {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Short numeric code if you follow the city’s numbering (e.g., 1,2,4,6,7,9,10,12,13)
        public int? Code { get; set; }

        // Human label for maps/UX (e.g., "Zone 6")
        [MaxLength(64)]
        public string? Name { get; set; }

        // MultiPolygon boundary for the zone (use SRID 4326)
        [Required]
        public MultiPolygon Geom { get; set; } = default!;

        // Streets/segments associated to this zone (kept)
        public ICollection<StreetSegment> Segments { get; set; } = new List<StreetSegment>();

        public DateTimeOffset? LastUpdated { get; set; }
    }
}
