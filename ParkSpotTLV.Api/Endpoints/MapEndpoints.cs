
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.IO;
using ParkSpotTLV.Api.Endpoints.Support;
using ParkSpotTLV.Api.Services.Evaluation.Contracts;
using ParkSpotTLV.Api.Services.Evaluation.Facade;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Map;
using ParkSpotTLV.Infrastructure;
using System.Text.Json;

namespace ParkSpotTLV.Api.Endpoints {
    public static class MapEndpoints {

        public static IEndpointRouteBuilder MapSegments (this IEndpointRouteBuilder routes) {

            var group = routes.MapGroup("/map").RequireAuthorization().WithTags("Map Segment Requests").RequireUser();


            group.MapPost("/segments",
                async ([FromBody] GetMapSegmentsRequest body, HttpContext ctx, AppDbContext db, TimeProvider clock, IMapSegmentsEvaluator evaluator, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    // Check BBOX validation
                    if ((body.MinLon >= body.MaxLon || body.MinLat >= body.MaxLat)
                        || (body.MinLon < -180 || body.MaxLon > 180 || body.MinLat < -90 || body.MaxLat > 90))
                        return Results.Problem(
                            title: "Invalid BBox Data",
                            statusCode: StatusCodes.Status400BadRequest,
                            type: "https://httpstatuses.com/400"
                            );

                    // Set the default time, LimitedThresholdMinutes, and MinDurationMinutes
                    var now = body.Now == default ? clock.GetLocalNow() : body.Now;
                    var minDuration = body.MinParkingTime <= 0 ? 60 : body.MinParkingTime;

                    // Create a permit snapshot
                    var pov = new PermitSnapshot {
                        Type = PermitSnapType.None,
                        ZoneCode = null,
                        VehicleId = null
                    };


                    // We check that the given permit Id is legitimate & create the permit snapshot
                    if (body.ActivePermitId is Guid pid) {
                        var permit = await db.Permits.Include(p => p.Zone).FirstOrDefaultAsync(p => p.Id == pid, ct);

                        if (permit is not null) {
                            if (permit.Type == PermitType.Disability) {
                                pov = new PermitSnapshot {
                                    Type = PermitSnapType.Disability,
                                    ZoneCode = null,
                                    VehicleId = permit.VehicleId
                                };

                            } else if (permit.Type == PermitType.ZoneResident) {

                                if (permit.Zone?.Code is null) return PermitProblems.MissingZoneCode(ctx);

                                pov = new PermitSnapshot {
                                    Type = PermitSnapType.Zone,
                                    ZoneCode = permit.Zone?.Code,
                                    VehicleId = permit.VehicleId
                                };
                            } else {
                                /* NEED TO TEST THIS */
                                pov = new PermitSnapshot {
                                    Type = PermitSnapType.None,
                                    ZoneCode = 0,
                                    VehicleId = permit.VehicleId
                                };
                            }
                        }else {
                            /* NEED TO TEST */
                            pov = new PermitSnapshot {
                                Type = PermitSnapType.None,
                                ZoneCode = 0,
                                VehicleId = null
                            };
                        }
                    }

                    // Create the internal request for the evaluator, and evaluate. Max MinDuration is 12 hours.
                    var internalReq = new MapSegmentsRequest {
                        MinLon = body.MinLon,
                        MaxLon = body.MaxLon,
                        MinLat = body.MinLat,
                        MaxLat = body.MaxLat,
                        CenterLon = body.CenterLon,
                        CenterLat = body.CenterLat,
                        Now = now,
                        Pov = pov,
                        MinParkingTime = minDuration > 720 ? 720 : minDuration
                    };

                    var segments = await evaluator.EvaluateAsync(internalReq, ct);
                    var writer = new GeoJsonWriter();               // Writer so we can extract geo lines as GeoJson text

                    // We create the data transfer object for outputting the segments
                    var dtoList = segments.Select(seg => new SegmentResponseDTO(
                        SegmentId: seg.SegmentId,
                        Tariff: EnumMappings.MapTariff(seg.Tariff),
                        ZoneCode: seg.ZoneCode,
                        NameEnglish: seg.NameEnglish,
                        NameHebrew: seg.NameHebrew,
                        Group: seg.Group,
                        Reason: seg.Reason,
                        ParkingType: EnumMappings.MapParkingType(seg.ParkingType),
                        IsPayNow: seg.IsPayNow,
                        IsPaylater: seg.IsPaylater,
                        AvailableFrom: seg.AvailableFrom,
                        AvailableUntil: seg.AvailableUntil,
                        NextChange: seg.NextChange,
                        FreeBudgetRemaining: seg.FreeBudgetRemaining,
                        Geometry: JsonDocument.Parse(writer.Write(seg.Geom)).RootElement
                    )).ToList();

                    // 7) Return a response built out of the DTOs
                    var response = new GetMapSegmentsResponse {
                        Now = now,
                        PermitId = body.ActivePermitId,
                        MinParkingTime = minDuration,
                        Count = dtoList.Count,
                        Segments = dtoList
                    };

                    return Results.Ok(response);
                })
                .Accepts<GetMapSegmentsRequest>("application/json")
                .Produces<GetMapSegmentsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Get Map Segments")
                .WithDescription("Gets all map segments in certain BBOX");




            return group;

        }

    }
}

