using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;

/*
 * Zone definition for the database
*/


namespace ParkSpotTLV.Infrastructure.Entities {
    public enum Taarif { City_Center = 1, City_Outskirts = 2 }

    public class Zone {
        /* 
         * Ownership 
         */
        public Guid Id { get; set; } = Guid.NewGuid();

        /* 
         * Zone Data
         */
        public int? Code { get; set; }
        [Required] public Taarif Taarif { get; set; } = Taarif.City_Center;
        [MaxLength(64)] public string? Name { get; set; } // E.g. "Zone 6"

        /*
         * Geometric data
         */
        [Required] public MultiPolygon Geom { get; set; } = default!; // MultiPolygon boundary for the zone (use SRID 4326)
        public ICollection<StreetSegment> Segments { get; set; } = []; // Streets/segments associated to this zone (kept)

        /*
         * Concurrency
         */
        public DateTimeOffset? LastUpdated { get; set; }
    }
}
