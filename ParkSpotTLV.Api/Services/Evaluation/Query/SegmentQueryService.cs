using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Contracts.Enums;


namespace ParkSpotTLV.Api.Services.Evaluation.Query {


    /*
     * Returns a snapshot of the segments according to current BBOX values
     */
    public sealed class SegmentQueryService(AppDbContext db) : ISegmentQueryService {

        private readonly AppDbContext _db = db;
        private readonly GeometryFactory _gf = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public async Task<IReadOnlyList<SegmentSnapshot>> GetViewportAsync(
            double minLon, double maxLon, double minLat, double maxLat, CancellationToken ct) {


            var env = _gf.ToGeometry(new Envelope(minLon, maxLon, minLat, maxLat));

            var rows = await _db.StreetSegments
                .AsNoTracking()
                .Where(s => s.Geom.Intersects(env))
                .Select(s => new SegmentSnapshot(
                    s.Id,
                    s.Zone != null ? s.Zone.Code : 0,
                    s.Zone != null ? s.Zone.Taarif : Tariff.City_Center,
                    s.ParkingType
                ))
                .ToListAsync(ct);

            return rows;
        }
    }
}
