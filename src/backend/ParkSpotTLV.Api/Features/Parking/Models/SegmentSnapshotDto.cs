using NetTopologySuite.Geometries;
using ParkSpotTLV.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Api.Features.Parking.Models {

    /*
     * Needed for evaluation per segment.
     * Produced by ISegmentQueryService (Query Object)
     */
    public sealed record SegmentSnapshotDto (
        Guid SegmentId,
        int? ZoneCode,
        Tariff Tariff, 
        ParkingType ParkingType,
        LineString Geom,
        string? NameHebrew,
        string? NameEnglish
    );
    
}
