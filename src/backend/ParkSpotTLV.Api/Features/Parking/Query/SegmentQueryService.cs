using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ParkSpotTLV.Api.Features.Parking.Models;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Infrastructure;
using System.Linq;


namespace ParkSpotTLV.Api.Features.Parking.Query {


    /*
     * Returns a snapshot of the segments according to current BBOX values
     */
    public sealed class SegmentQueryService(AppDbContext db) : ISegmentQueryService {

        private readonly AppDbContext _db = db;
        private readonly GeometryFactory _gf = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        const int MAX_SEGMENTS = 100;
        public async Task<IReadOnlyList<SegmentSnapshotDto>> GetViewportAsync(
            double minLon, double maxLon, double minLat, double maxLat, double centerLon, double centerLat, CancellationToken ct) {

            // We create an environment 'box', and a coordinate to reprsent the center of persons POV, we dont need these with the raw sql..
            var env = _gf.ToGeometry(new Envelope(minLon, maxLon, minLat, maxLat));
            var center = _gf.CreatePoint(new Coordinate(centerLon, centerLat));

            // Raw SQL KNN without using LINQ.
            var rows = await _db.StreetSegments.FromSqlInterpolated($@"
                        SELECT *
                        FROM ""street_segments""
                        WHERE ST_Intersects(geom, ST_MakeEnvelope({minLon}, {minLat}, {maxLon}, {maxLat}, 4326))
                        ORDER BY geom <-> ST_SetSRID(ST_MakePoint({centerLon}, {centerLat}), 4326)
                        LIMIT {MAX_SEGMENTS}")
                .AsNoTracking().Select(s => new SegmentSnapshotDto(
                    s.Id,
                    s.Zone != null ? s.Zone.Code : -1,
                    s.Zone != null ? s.Zone.Taarif : Tariff.City_Center,
                    s.ParkingType,
                    s.Geom,
                    s.NameEnglish,
                    s.NameHebrew))
                .ToListAsync(ct);


            // Linq with KNN does not return list organized properly... for some reason..........................................
            /*var rows = await _db.StreetSegments
                .Where(s => s.Geom.Intersects(env))              
                .OrderBy(s => EF.Functions.DistanceKnn(s.Geom, center))
                .Take(MAX_SEGMENTS)
                .Select(s => new SegmentSnapshotDto(
                    s.Id,
                    s.Zone != null ? s.Zone.Code : -1,
                    s.Zone != null ? s.Zone.Taarif : Tariff.City_Center,
                    s.ParkingType,
                    s.Geom,
                    s.NameEnglish,
                    s.NameHebrew))
                .ToListAsync(ct);*/


            return rows;
        }
    }
}
