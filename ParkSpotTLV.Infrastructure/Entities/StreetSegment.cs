using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;

/*
 * Street segment definition for the database
*/

namespace ParkSpotTLV.Infrastructure.Entities {

    public enum ParkingType { Unknown = 0, Free = 1, Paid = 2, Limited = 3 }
    public enum ParkingHours { Unknown = 0, SpecificHours = 1 }

    public class StreetSegment {

        // ID of street
        public Guid Id { get; set; } = Guid.NewGuid();

        // Name of street 
        [MaxLength(128)]
        public string? Name { get; set; }

        // Geom of street
        public MultiLineString Geom { get; set; } = default!;

        // What zone does this street belong to?
        public Guid? ZoneId { get; set; }
        public Zone? Zone { get; set; }

        public bool CarsOnly { get; set; } = false;

        public ParkingType ParkingType { get; set; } = ParkingType.Unknown;
        public ParkingHours ParkingHours { get; set; } = ParkingHours.Unknown;

    }
}
