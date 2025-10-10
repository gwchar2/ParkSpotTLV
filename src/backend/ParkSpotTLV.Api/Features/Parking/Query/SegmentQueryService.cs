using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ParkSpotTLV.Api.Features.Parking.Models;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Infrastructure;


namespace ParkSpotTLV.Api.Features.Parking.Query {


    /*
     * Returns a snapshot of the segments according to current BBOX values
     */
    public sealed class SegmentQueryService(AppDbContext db) : ISegmentQueryService {

        private readonly AppDbContext _db = db;
        private readonly GeometryFactory _gf = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        const int MAX_SEGMENTS = 100;
        public async Task<IReadOnlyList<SegmentSnapshot>> GetViewportAsync(
            double minLon, double maxLon, double minLat, double maxLat, double centerLon, double centerLat, CancellationToken ct) {

            // We create an environment 'box', and a coordinate to reprsent the center of persons POV
            var env = _gf.ToGeometry(new Envelope(minLon, maxLon, minLat, maxLat));
            var center = _gf.CreatePoint(new Coordinate(centerLon, centerLat));


            // We grab a max of MAX_SEGMENTS rows from the street segments table, which are closes to the center.
            var rows = await _db.StreetSegments
                .AsNoTracking()
                .Where(s => s.Geom.Intersects(env))
                .OrderBy(s => s.Geom.Distance(center))
                .Take(MAX_SEGMENTS)
                .Select(s => new SegmentSnapshot(
                    s.Id,
                    s.Zone != null ? s.Zone.Code : -1,
                    s.Zone != null ? s.Zone.Taarif : Tariff.City_Center,
                    s.ParkingType,
                    s.Geom,
                    s.NameEnglish,
                    s.NameHebrew
                ))
                .ToListAsync(ct);

            return rows;
        }
    }
}
