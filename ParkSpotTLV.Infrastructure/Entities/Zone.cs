using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;

/*
 * Zone definition for the database
*/


namespace ParkSpotTLV.Infrastructure.Entities {
    public class Zone {

        public Guid Id { get; set; } = Guid.NewGuid();

        public int ZonePermit { get; set; } = 0;    // from 0 to 10, 0 = none.

        public MultiPolygon Geom { get; set; } = default!;
        public ICollection<StreetSegment> Segments { get; set; } = new List<StreetSegment>();
    }
}
