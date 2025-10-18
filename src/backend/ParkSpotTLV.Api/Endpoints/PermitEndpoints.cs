
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Api.Endpoints.Support;
using ParkSpotTLV.Api.Endpoints.Support.Errors;
using ParkSpotTLV.Api.Endpoints.Support.EndpointFilters;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Permits;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;
using ParkSpotTLV.Contracts.Time;

namespace ParkSpotTLV.Api.Endpoints {
    public static class PermitEndpoints {

        public static IEndpointRouteBuilder MapPermits(this IEndpointRouteBuilder routes) {

            var group = routes.MapGroup("/permits").RequireAuthorization().WithTags("Permit Requests").RequireUser();


            /* Post /   Creates a Permit and attaches it to a specific vehicle
             * Accepts: PermitCreateRequest + VehicleID + Access Token
             * Returns: 
             *      201 Permit Created (Specific vehicle).
             *      400 Bad Request.
             *      401 if access token is expired.
             *      404 if no such vehicle exists.
             */
            group.MapPost("/",
                async ([FromBody] PermitCreateRequest body, HttpContext ctx, AppDbContext db,IClock clock, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    // If the vehicle is not found, return not found vehicle
                    var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == body.VehicleId, ct);
                    if (vehicle is null) return VehicleErrors.NotFound(ctx);

                    // Check if there is a maximum amount of permits on the vehicle already
                    if (await db.Permits.CountAsync(p => p.VehicleId == vehicle.Id, ct) == 3)
                        return PermitErrors.MaxHit(ctx);

                    // Create the new permit
                    var permit = new Permit { };

                    if (body.Type == PermitType.ZoneResident) {

                        var hasResident = vehicle.Permits.FirstOrDefault(p => p.Type == PermitType.ZoneResident);
                        if (hasResident is not null) return PermitErrors.MaxOne(ctx);
                        if (!body.ResidentZoneCode.HasValue) return PermitErrors.MissingZone(ctx);

                        var zone = await db.Zones.FirstOrDefaultAsync(z => z.Code == body.ResidentZoneCode, ct);
                        if (zone is null) return GeneralErrors.InvalidZoneCode(ctx);

                        permit.Type = body.Type;
                        permit.ZoneCode = body.ResidentZoneCode;
                        permit.Vehicle = vehicle;
                        permit.VehicleId = body.VehicleId;
                        db.Permits.Add(permit);



                    } else if (body.Type == PermitType.Disability) {
                        var hasDisability = vehicle.Permits.FirstOrDefault(p => p.Type == PermitType.Disability);
                        if (hasDisability is not null) return PermitErrors.MaxOne(ctx);

                        permit.Type = body.Type;
                        permit.Vehicle = vehicle;
                        permit.VehicleId = body.VehicleId;
                        db.Permits.Add(permit);
                    }
                    else if (body.Type == PermitType.Default) {
                        var hasDefault = vehicle.Permits.FirstOrDefault(p => p.Type == PermitType.Default);
                        if (hasDefault is not null) return PermitErrors.MaxOne(ctx);

                        permit.Type = body.Type;
                        permit.Vehicle = vehicle;
                        permit.VehicleId = body.VehicleId;
                        db.Permits.Add(permit);
                    } else {
                        return PermitErrors.ChooseType(ctx);
                    }

                    await db.SaveChangesAsync(ct);

                    // Return the new permit
                    var dto = await db.Permits.AsNoTracking().Where(p => p.Id == permit.Id).Select(p => new PermitResponse {
                        PermitId = p.Id,
                        VehicleId = p.VehicleId,
                        PermitType = EnumMappings.MapPermitType(p.Type),
                        ResidentZoneCode = p.ZoneCode,
                        LastUpdated = clock.ToLocal(p.LastUpdatedUtc)
                    }).SingleAsync(ct);


                    return Results.Created($"/permits/{dto.PermitId}", dto);
                })
                .Accepts<PermitCreateRequest>("application/json")
                .EnforceJsonContent()
                .Produces<PermitResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Add Permit")
                .WithDescription("Create a permit.");


            /* GET /id Returns a specific permit
             * Accepts: Permit ID + Access Token
             * Returns: 
             *      200 with PermitResponse (Specific permit).
             *      401 if access token is expired.
             *      403 if permit exists but not for this user.
             *      404 if no such permit exists.
             */
            group.MapGet("/{id:guid}",
                async (Guid id, HttpContext ctx, AppDbContext db,IClock clock, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    var permit = await db.Permits.AsNoTracking().Include(p => p.Vehicle).SingleOrDefaultAsync(p => p.Id == id, ct);

                    if (permit is null || permit.Vehicle.OwnerId != userId) return PermitErrors.Forbidden(ctx);

                    var dto = new PermitResponse {
                        PermitId = id,
                        VehicleId = permit.VehicleId,
                        PermitType = EnumMappings.MapPermitType(permit.Type),
                        ResidentZoneCode = permit.ZoneCode,
                        LastUpdated = clock.ToLocal(permit.LastUpdatedUtc),
                        RowVersion = Convert.ToBase64String(BitConverter.GetBytes(permit.Xmin))
                    };

                    return Results.Ok(dto);
                })
                .Produces<PermitResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Get Permit")
                .WithDescription("Retuns a specific permit.");


            /* Patch /id    Updates a permit
             * Accepts: PermitUpdateRequest & Permit ID + Access Token
             * Returns: 
             *      200 PermitResponse (Updated Permit)
             *      400 Bad Request 
             *      401 Unauthorized - Ilegal Access Token
             *      403 Forbidden - Permit does not belong to user
             *      404 Not Found - Permit ID was not found
             *      409 Conflict - Race condition
             */
            group.MapPatch("/{id:guid}",
                async (Guid id, [FromBody] PermitUpdateRequest body, HttpContext ctx, AppDbContext db,IClock clock,CancellationToken ct) => {

                    var userId = ctx.GetUserId();
                    var expectedXmin = ctx.GetXmin();

                    // Get the permit from DB, check for rights
                    var permit = await db.Permits.Where(p => p.Id == id).Include(p => p.Vehicle).ThenInclude(v => v.Permits).SingleOrDefaultAsync(ct); //

                    if (permit is null || permit.Vehicle.OwnerId != userId) //
                        return PermitErrors.Forbidden(ctx);

                    // Check if the vehicle already has a permit of the 'want-to-change-to' type
                    if (permit.Type != body.Type && (permit.Vehicle.Permits.Any(p => p.Type == body.Type))) //
                        return PermitErrors.MaxOne(ctx);

                    // Check for concurrency
                    if (permit.Xmin != expectedXmin) return GeneralErrors.ConcurrencyError(ctx);

                    // Implement the change (If its zone permit -> MUST HAVE zone code)
                    if (body.Type == PermitType.ZoneResident && !body.ZoneCode.HasValue)
                        return PermitErrors.MissingZoneCode(ctx);

                    if (body.Type == PermitType.Disability && body.ZoneCode.HasValue)
                        permit.ZoneCode = null;
                    else if (body.ZoneCode is int code) {
                        if (code == 0 || !await db.Zones.AsNoTracking().AnyAsync(z => z.Code == code, ct)) {
                            return PermitErrors.BadZone(ctx);
                        }
                        permit.ZoneCode = body.ZoneCode;
                    }


                    permit.Type = body.Type;
                    permit.LastUpdatedUtc = clock.UtcNow;


                    await db.SaveChangesAsync(ct);

                    // Return the new permit 
                    var dto = await db.Permits.AsNoTracking().Where(p => p.Id == id).Select(p => new PermitResponse {
                        PermitId = p.Id,
                        VehicleId = p.VehicleId,
                        PermitType = EnumMappings.MapPermitType(p.Type),
                        ResidentZoneCode = p.ZoneCode,
                        LastUpdated = clock.LocalNow,
                        RowVersion = Convert.ToBase64String(BitConverter.GetBytes(p.Xmin))
                    }).SingleAsync(ct);

                    return Results.Ok(dto);
                })
                .EnforceJsonContent()
                .RequireRowVersion()
                .Produces<PermitResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithSummary("Update Permit")
                .WithDescription("Update a permit (partial; requires rowVersion).");


            /* Delete /ID    Deletes a permit
             * Accepts: PermitDeleteRequest & Permit ID + Access Token
             * Returns: 
             *      204 OK without content
             *      401 Unauthorized - Ilegal Access Token
             *      403 Forbidden - Permit does not belong to user
             *      404 Not Found - Permit ID was not found
             *      409 Conflict - Race condition
             */
            group.MapDelete("/{id:guid}",
                async (Guid id, [FromBody] PermitDeleteRequest body, HttpContext ctx, AppDbContext db, CancellationToken ct) => {

                    var userId = ctx.GetUserId();
                    var expectedXmin = ctx.GetXmin();

                    // We need to make sure the permit belongs to specified user
                    var permit = await db.Permits.AsNoTracking().Where(p => p.Id == id && p.Vehicle.OwnerId == userId).SingleOrDefaultAsync(ct);

                    if (permit is null)  return PermitErrors.Forbidden(ctx);

                    if (permit.Xmin != expectedXmin) return GeneralErrors.ConcurrencyError(ctx);

                    // Cant remove default permit!!! Can hide it on UI if want.
                    if (permit.Type == PermitType.Default) return PermitErrors.CantRemoveDef(ctx);

                    // Delete the permit
                    db.Permits.Remove(permit);
                    await db.SaveChangesAsync(ct);

                    return Results.NoContent();
                })
                .EnforceJsonContent()
                .RequireRowVersion()
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithSummary("Delete Permit")
                .WithDescription("Delete a permit (owner only; requires rowVersion).");




            return group;
        }
    }
}
