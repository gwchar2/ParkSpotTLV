
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Contracts.Permits;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;
using System.Security.Claims;

namespace ParkSpotTLV.Api.Endpoints {
    public static class PermitEndpoints {

        public static IEndpointRouteBuilder MapPermits(this IEndpointRouteBuilder routes) {

            var group = routes.MapGroup("/permits").RequireAuthorization().WithTags("Permit Requests");
            /* GET PERMITS -> Use GET VEHICLES */


            /* Post /   Creates a Permit and attaches it to a specific vehicle
             * Accepts: PermitCreateRequest + VehicleID + Access Token
             * Returns: 
             *      201 Permit Created (Specific vehicle).
             *      400 Bad Request.
             *      401 if access token is expired.
             *      404 if no such vehicle exists.
             */
            group.MapPost("/",
                async ([FromBody] PermitCreateRequest body, HttpContext ctx, AppDbContext db, CancellationToken ct) => {

                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Problem(
                            title: "Invalid or expired token.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            type: "https://httpstatuses.com/401"
                            );

                    // If the vehicle is not found, return not found vehicle
                    var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.Id == body.VehicleId, ct);
                    if (vehicle is null)
                        return Results.Problem(
                            title: "Vehicle was not found.",
                            statusCode: StatusCodes.Status404NotFound,
                            type: "https://httpstatuses.com/404"
                            );

                    // Check if there is a maximum amount of permits on the vehicle already
                    if (await db.Permits.CountAsync(p => p.VehicleId == vehicle.Id, ct) == 2)
                        return Results.Problem(
                            title: "Maximum of 2 permits per vehicle", 
                            statusCode: StatusCodes.Status400BadRequest
                            );

                    // Create the new permit
                    var permit = new Permit { };

                    if (body.Type == PermitType.ZoneResident) {

                        var hasResident = vehicle.Permits.FirstOrDefault(p => p.Type == PermitType.ZoneResident);
                        if (hasResident is not null)
                            return Results.Problem(
                                title: "Vehicle can not have more than 1 permit of same type", 
                                statusCode: StatusCodes.Status409Conflict
                                );

                        if (!body.ResidentZoneCode.HasValue)
                            return Results.Problem(
                                title: "Please add a zone code!", 
                                statusCode: StatusCodes.Status400BadRequest
                                );

                        var zone = await db.Zones.FirstOrDefaultAsync(z => z.Code == body.ResidentZoneCode, ct);
                        if (zone is null)
                            return Results.Problem(
                                title: "Invalid zone code",
                                detail: $"Zone code {body.ResidentZoneCode} does not exist.", 
                                statusCode: StatusCodes.Status404NotFound
                                );

                        permit.Type = body.Type;
                        permit.ZoneCode = body.ResidentZoneCode;
                        permit.Vehicle = vehicle;
                        permit.VehicleId = body.VehicleId;
                        db.Permits.Add(permit);



                    } else if (body.Type == PermitType.ZoneResident) {
                        var hasResident = vehicle.Permits.FirstOrDefault(p => p.Type == PermitType.Disability);
                        if (hasResident is not null)
                            return Results.Problem(
                                title: "Vehicle can not have more than 1 permit of same type", 
                                statusCode: StatusCodes.Status409Conflict
                                );

                        permit.Type = body.Type;
                        permit.Vehicle = vehicle;
                        permit.VehicleId = body.VehicleId;
                        db.Permits.Add(permit);
                    } else {
                        return Results.Problem(
                            title: "Please choose type of permit", 
                            statusCode: StatusCodes.Status400BadRequest
                            );
                    }

                    await db.SaveChangesAsync(ct);

                    // Return the new permit
                    var dto = await db.Permits.AsNoTracking().Where(p => p.Id == permit.Id).Select(p => new PermitResponse {
                        PermitId = p.Id,
                        VehicleId = p.VehicleId,
                        Type = p.Type,
                        ResidentZoneCode = p.ZoneCode,
                        LastUpdated = p.LastUpdated
                    }).SingleAsync(ct);


                    return Results.Created($"/permits/{dto.PermitId}", dto);
                })
                .Accepts<PermitCreateRequest>("application/json")
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
                async (Guid id, HttpContext ctx, AppDbContext db, CancellationToken ct) => {

                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Problem(
                            title: "Invalid or expired token.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            type: "https://httpstatuses.com/401"
                            );

                    var permit = await db.Permits.AsNoTracking().Include(p => p.Vehicle).SingleOrDefaultAsync(p => p.Id == id, ct);

                    if (permit is null) return Results.Problem(
                            title: "Permit not found",
                            statusCode: StatusCodes.Status404NotFound,
                            type: "https://httpstatuses.com/404"
                            );

                    if (permit.Vehicle.OwnerId != userId) 
                        return Results.Problem(
                            title: "Permit does not belong to this user.",
                            statusCode: StatusCodes.Status403Forbidden,
                            type: "https://httpstatuses.com/403"
                            );

                    var dto = new PermitResponse {
                        PermitId = id,
                        VehicleId = permit.VehicleId,
                        Type = permit.Type,
                        ResidentZoneCode = permit.ZoneCode,
                        LastUpdated = permit.LastUpdated
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
                async (Guid id, [FromBody] PermitUpdateRequest body, HttpContext ctx, AppDbContext db, CancellationToken ct) => {


                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Problem(
                            title: "Invalid or expired token.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            type: "https://httpstatuses.com/401"
                        );

                    if (string.IsNullOrWhiteSpace(body.RowVersion))
                        return Results.Problem(
                            title: "Missing rowVersion", 
                            statusCode: StatusCodes.Status400BadRequest
                            );

                    uint expectedXmin;
                    try { 
                        expectedXmin = BitConverter.ToUInt32(Convert.FromBase64String(body.RowVersion)); 
                    }
                    catch { 
                        return Results.Problem(
                            title: "Invalid RowVersion format", 
                            statusCode: StatusCodes.Status400BadRequest
                            ); 
                    }

                    // Get the permit from DB, check for rights
                    var permit = await db.Permits.Include(p => p.Id == id).SingleOrDefaultAsync(ct);
                    if (permit is null)
                        return Results.Problem(
                            title: "Permit not found.",
                            statusCode: StatusCodes.Status404NotFound,
                            type: "https://httpstatuses.com/404"
                            );

                    if (permit.Vehicle.Id != userId)
                        return Results.Problem(
                            title: "Vehicle does not belong to current user.",
                            statusCode: StatusCodes.Status403Forbidden,
                            type: "https://httpstatuses.com/403"
                            );

                    // Check if the vehicle already has a permit of the 'want-to-change-to' type
                    if (permit.Type != body.Type && (permit.Vehicle.Permits.Select(p => p.Type == body.Type) is not null)) 
                        return Results.Problem(
                            title: "Vehicle already has permit.",
                            detail: $"Vehicle {permit.VehicleId} already has a permit of type {body.Type}.", 
                            statusCode: StatusCodes.Status409Conflict
                            );

                    // Check for concurrency
                    if (permit.Xmin != expectedXmin)
                        return Results.Problem(
                            title: "Concurrency Error",
                            detail: "The permit was modified by another request. Reload and try again.",
                            statusCode: StatusCodes.Status409Conflict
                            );

                    // Implement the change (If its zone permit -> MUST HAVE zone code)
                    if (body.Type == PermitType.ZoneResident && !body.ZoneCode.HasValue)
                            return Results.Problem(
                                title: "Zone permit must include a zone.", 
                                statusCode: StatusCodes.Status400BadRequest
                                );

                    permit.Type = body.Type;
                    permit.ZoneCode = body.ZoneCode;
                    permit.LastUpdated = DateTimeOffset.Now;


                    await db.SaveChangesAsync(ct);

                    // Return the new permit 
                    var dto = await db.Permits.AsNoTracking().Where(p => p.Id == id).Select(p => new PermitResponse {
                        PermitId = p.Id,
                        VehicleId = p.VehicleId,
                        Type = p.Type,
                        ResidentZoneCode = p.ZoneCode,
                        LastUpdated = p.LastUpdated
                    }).SingleAsync(ct);

                    return Results.Ok(dto);
                })
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

                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Problem(
                            title: "Invalid or expired token.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            type: "https://httpstatuses.com/401"
                            );

                    // We check for concurrency
                    if (string.IsNullOrWhiteSpace(body.RowVersion))
                        return Results.Problem(
                            title: "Missing RowVersion",
                            statusCode: StatusCodes.Status400BadRequest
                            );

                    uint expectedXmin;
                    try {
                        expectedXmin = BitConverter.ToUInt32(Convert.FromBase64String(body.RowVersion));
                    } catch {
                        return Results.Problem(
                            title: "Invalid RowVersion format",
                            statusCode: StatusCodes.Status400BadRequest
                            );
                    }

                    // We need to make sure the permit belongs to specified user
                    var permit = await db.Permits.AsNoTracking().Include(p => p.Vehicle).SingleOrDefaultAsync(p => p.Id == id, ct);

                    if (permit is null) 
                        return Results.Problem(
                            title: "Permit not found.",
                            statusCode: StatusCodes.Status404NotFound
                            );

                    if (permit.Vehicle.OwnerId != userId) 
                        return Results.Problem(
                            title: "Permit does not belong to user.",
                            statusCode: StatusCodes.Status403Forbidden
                            );

                    if (permit.Xmin != expectedXmin)
                        return Results.Problem(
                            title: "Concurrency Error",
                            detail: "The permit was modified by another request. Reload and try again.",
                            statusCode: StatusCodes.Status409Conflict
                            );

                    // Check if we leave the vehicle with 0 permits (NOT POSSIBLE)
                    if (permit.Vehicle.Permits.Count == 0)
                        return Results.Problem(
                            title: "Max permits for user reached.",
                            detail: $"Can not remove permit, Vehicle {permit.VehicleId} will have 0 permits.",
                            statusCode: StatusCodes.Status409Conflict
                            );

                    // Delete the permit
                    db.Permits.Remove(permit);
                    await db.SaveChangesAsync(ct);

                    return Results.NoContent();
                })
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
