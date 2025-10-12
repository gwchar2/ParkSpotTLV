
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Api.Endpoints.Support;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Vehicles;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;

namespace ParkSpotTLV.Api.Endpoints {
    public static class VehicleEndpoints {

        public static IEndpointRouteBuilder MapVehicles (this IEndpointRouteBuilder routes) {
            var group = routes.MapGroup("/vehicles").WithTags("Vehicle Requests").RequireAuthorization().RequireUser();


            /* GET /VEHICLES
             * Accepts: Access Token
             * Returns: 
             *      200 with List<VehicleResponse>.
             *      401 if access token is expired.
             */
            group.MapGet("/", async (
                HttpContext ctx, AppDbContext db, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    var vehicles = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.OwnerId == userId)
                        .Include(v => v.Permits)
                        .Select(v => new VehicleResponse {
                            Id = v.Id,
                            Type = EnumMappings.MapVehicleType(v.Type),
                            Name = v.Name,
                            ResidencyPermitId = v.Permits.Where(p => p.Type == PermitType.ZoneResident).Select(p => p.Id).FirstOrDefault(),
                            ResidentZoneCode = v.Permits
                                .Where(p => p.Type == PermitType.ZoneResident)
                                .Select(p => p.ZoneCode)
                                .FirstOrDefault(),
                            DisabilityPermitId = v.Permits.Where(p => p.Type == PermitType.Disability).Select(p => p.Id).FirstOrDefault(),
                            DisabledPermit = v.Permits.Any(p => p.Type == PermitType.Disability),
                            DefaultPermitId = v.Permits.Where(p => p.Type == PermitType.Default).Select(p => p.Id).FirstOrDefault(),
                            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(v.Xmin))
                        })
                        .ToListAsync(ct);

                    return vehicles is null ? VehicleProblems.Forbidden(ctx) : Results.Ok(vehicles);

                })
                .Produces<List<VehicleResponse>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .WithSummary("List Vehicles")
                .WithDescription("List caller's vehicles.");

            /* GET /id Gets a specific vehicle
             * Accepts: Vehicle ID + Access Token
             * Returns: 
             *      200 with VehicleResponse (Specific vehicle).
             *      401 if access token is expired.
             *      403 if vehicle exists but not for this user.
             *      404 if no such vehicle exists.
             */
            group.MapGet("/{id:guid}", async (
                Guid id, HttpContext ctx, AppDbContext db, CancellationToken ct) => {

                    var userId = ctx.GetUserId();
                    
                    var ownerId = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == id)
                        .Select(v => v.OwnerId)
                        .SingleOrDefaultAsync(ct);

                    if (ownerId == Guid.Empty || ownerId != userId) return VehicleProblems.Forbidden(ctx);

                    var dto = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == id)
                        .Include(v => v.Permits)
                        .Select(v => new VehicleResponse {
                            Id = v.Id,
                            Type = EnumMappings.MapVehicleType(v.Type),
                            Name = v.Name,
                            ResidencyPermitId = v.Permits.Where(p => p.Type == PermitType.ZoneResident).Select(p => p.Id).FirstOrDefault(),
                            ResidentZoneCode = v.Permits
                                .Where(p => p.Type == PermitType.ZoneResident)
                                .Select(p => p.ZoneCode)
                                .FirstOrDefault(),
                            DisabilityPermitId = v.Permits.Where(p => p.Type == PermitType.Disability).Select(p => p.Id).FirstOrDefault(),
                            DisabledPermit = v.Permits.Any(p => p.Type == PermitType.Disability),
                            DefaultPermitId = v.Permits.Where(p => p.Type == PermitType.Default).Select(p => p.Id).FirstOrDefault(),
                            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(v.Xmin)) })
                        .SingleAsync(ct);

                    return Results.Ok(dto);
                })
                .Produces<VehicleResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Get Vehicle")
                .WithDescription("Get a single vehicle owned by the caller.");

            /* Post /   Creates a vehicle with a permit
             * Accepts: VehicleCreateRequest + Access Token
             * Returns: 
             *      201 Vehicle Created (Specific vehicle).
             *      400 Bad Request.
             *      401 if access token is expired.
             */
            group.MapPost("/", async (
                [FromBody] VehicleCreateRequest body, HttpContext ctx, AppDbContext db, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    if (await db.Vehicles.AsNoTracking().AnyAsync(z => z.OwnerId == userId && z.Name == body.Name, ct))
                        return VehicleProblems.NameExists(ctx);

                    var hasResidency = body.ResidentZoneCode.HasValue;
                    var hasDisability = body.HasDisabledPermit;

                    // If residency provided, validate that Zone.Code exists
                    if (hasResidency) {
                        var exists = await db.Zones.AsNoTracking().AnyAsync(z => z.Code == body.ResidentZoneCode!.Value, ct);
                        if (!exists)  return GeneralProblems.InvalidZoneCode(ctx);
                    }

                    // Create a new vehicle with a default permit
                    var vehicle = new Vehicle {
                        OwnerId = userId,
                        Type = body.Type,
                        Name = body.Name,
                        Permits = []
                    };
                    vehicle.Permits.Add(new Permit { 
                        Type = PermitType.Default 
                    });

                    // If the vehicle has residencypermit, add it.
                    if (hasResidency) {
                        vehicle.Permits.Add(new Permit {
                            Type = PermitType.ZoneResident,
                            ZoneCode = body.ResidentZoneCode!.Value,
                        });
                    }
                    // If the vehicle has disability permit, add it.
                    if (hasDisability) {
                        vehicle.Permits.Add(new Permit {
                            Type = PermitType.Disability,
                        });
                    }

                    // Add the vehicle to the DB
                    db.Vehicles.Add(vehicle);
                    await db.SaveChangesAsync(ct);

                    // We return status code 201 with the new created vehicle
                    var dto = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == vehicle.Id)
                        .Include(v => v.Permits)
                        .Select(v => new VehicleResponse {
                            Id = v.Id,
                            Type = EnumMappings.MapVehicleType(v.Type),
                            Name = v.Name,
                            ResidencyPermitId = v.Permits.Where(p => p.Type == PermitType.ZoneResident).Select(p => p.Id).FirstOrDefault(),
                            ResidentZoneCode = v.Permits.Where(p => p.Type == PermitType.ZoneResident).Select(p => p.ZoneCode).FirstOrDefault(),
                            DisabilityPermitId = v.Permits.Where(p => p.Type == PermitType.Disability).Select(p => p.Id).FirstOrDefault(),
                            DisabledPermit = v.Permits.Any(p => p.Type == PermitType.Disability),
                            DefaultPermitId = v.Permits.Where(p => p.Type == PermitType.Default).Select(p => p.Id).FirstOrDefault(),
                            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(v.Xmin))})
                        .SingleAsync(ct);

                    return Results.Created($"/vehicles/{dto.Id}", dto);
                })
                .Accepts<VehicleCreateRequest>("application/json")
                .Produces<VehicleResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .WithSummary("Add Vehicle")
                .WithDescription("Create a vehicle (permits optional; zero permitted).");

            /* Patch /id    Updates a vehicle
             * Accepts: VehicleCreateRequest & Vehicle ID + Access Token
             * Returns: 
             *      200 VehicleResponse (Updated Vehicle)
             *      400 Bad Request 
             *      401 Unauthorized - Ilegal Access Token
             *      403 Forbidden - Vehicle does not belong to user
             *      404 Not Found - Vehicle ID was not found
             *      409 Conflict - Race condition
             */
            group.MapPatch("/{id:guid}",
                async (Guid id,[FromBody] VehicleUpdateRequest body, HttpContext ctx, AppDbContext db, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    // Check for concurrency
                    if (string.IsNullOrWhiteSpace(body.RowVersion)) return GeneralProblems.MissingRowVersion(ctx);

                    if (!Guards.TryBase64ToUInt32(body.RowVersion, out uint expectedXmin))
                        return GeneralProblems.InvalidRowVersion(ctx);

                    // Get the vehicle from the DB & check ownership
                    var vehicle = await db.Vehicles.Include(v => v.Permits).SingleOrDefaultAsync(v => v.Id == id, ct);

                    if (vehicle is null || vehicle.OwnerId != userId) return VehicleProblems.Forbidden(ctx);

                    if (vehicle.Xmin != expectedXmin) return GeneralProblems.ConcurrencyError(ctx);

                    if (body.Type.HasValue) vehicle.Type = body.Type.Value;

                    if (!string.IsNullOrWhiteSpace(body.Name))  vehicle.Name = body.Name.Trim();

                    await db.SaveChangesAsync(ct);

                    // Return updated vehicle
                    var dto = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == id)
                        .Select(v => new VehicleResponse {
                            Id = v.Id,
                            Type = EnumMappings.MapVehicleType(v.Type),
                            Name = v.Name,
                            ResidencyPermitId = v.Permits.Where(p => p.Type == PermitType.ZoneResident).Select(p => p.Id).FirstOrDefault(),
                            ResidentZoneCode = v.Permits
                                .Where(p => p.Type == PermitType.ZoneResident)
                                .Select(p => p.ZoneCode)
                                .FirstOrDefault(),
                            DisabilityPermitId = v.Permits.Where(p => p.Type == PermitType.Disability).Select(p => p.Id).FirstOrDefault(),
                            DisabledPermit = v.Permits.Any(p => p.Type == PermitType.Disability),
                            DefaultPermitId = v.Permits.Where(p => p.Type == PermitType.Default).Select(p => p.Id).FirstOrDefault(),
                            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(v.Xmin))})
                        .ToListAsync(ct);

                    return Results.Ok(dto);
                })
                .Produces<VehicleResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithSummary("Update Vehicle")
                .WithDescription("Update a vehicle (partial; requires rowVersion).");


            /* Delete /ID    Deletes a vehicle
             * Accepts: VehicleDeleteRequest & Vehicle ID + Access Token
             * Returns: 
             *      204 OK without content
             *      401 Unauthorized - Ilegal Access Token
             *      403 Forbidden - Vehicle does not belong to user
             *      404 Not Found - Vehicle ID was not found
             *      409 Conflict - Race condition
             */
            group.MapDelete("/{id:guid}", 
                async (Guid id, [FromBody] VehicleDeleteRequest body, HttpContext ctx, AppDbContext db, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    // We make sure that RowVersion exists and is legal, so there are no race conditions
                    if (string.IsNullOrWhiteSpace(body.RowVersion)) return GeneralProblems.MissingRowVersion(ctx);
                    if (!Guards.TryBase64ToUInt32(body.RowVersion, out uint expectedXmin))
                        return GeneralProblems.InvalidRowVersion(ctx);

                    // We need to make sure the permit belongs to specified user
                    var vehicle = await db.Vehicles.AsNoTracking().Where(p => p.Id == id && p.OwnerId == userId).SingleOrDefaultAsync(ct);

                    if (vehicle is null || vehicle.OwnerId != userId) return VehicleProblems.Forbidden(ctx);

                    if (vehicle.Xmin != expectedXmin) return GeneralProblems.ConcurrencyError(ctx);

                    var userVehicles = await db.Vehicles.AsNoTracking().Where(p => p.OwnerId == userId).ToListAsync(ct);

                    // Check if we leave the vehicle with 0 permits (NOT POSSIBLE)
                    if (userVehicles is not null && userVehicles.Count <= 1) return VehicleProblems.CantRemove(ctx);


                    db.Vehicles.Remove(vehicle);
                    await db.SaveChangesAsync(ct);

                    return Results.NoContent();
                })
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithSummary("Delete Vehicle")
                .WithDescription("Delete a vehicle (owner only; requires rowVersion).");


            return group;
        }
    }
}
