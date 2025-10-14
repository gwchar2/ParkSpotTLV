
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Api.Endpoints.Support;
using ParkSpotTLV.Api.Endpoints.Support.Errors;
using ParkSpotTLV.Api.Endpoints.Support.EndpointFilters;
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

                    return vehicles is null ? VehicleErrors.Forbidden(ctx) : Results.Ok(vehicles);

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

                    var dto = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == id)
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
                            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(v.Xmin)) })
                        .SingleOrDefaultAsync(ct);

                    if (dto is null) return VehicleErrors.Forbidden(ctx);

                    return Results.Ok(dto);
                })
                .RequireVehicleOwner()
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
                        return VehicleErrors.NameExists(ctx);

                    var hasResidency = body.ResidentZoneCode.HasValue;
                    var hasDisability = body.HasDisabledPermit;

                    // If residency provided, validate that Zone.Code exists
                    if (hasResidency) {
                        var exists = await db.Zones.AsNoTracking().AnyAsync(z => z.Code == body.ResidentZoneCode!.Value, ct);
                        if (!exists)  return GeneralErrors.InvalidZoneCode(ctx);
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
                .EnforceJsonContent()
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
                    var expectedXmin = ctx.GetXmin();

                    // Get the vehicle from the DB & check ownership
                    var vehicle = await db.Vehicles.Include(v => v.Permits).SingleOrDefaultAsync(v => v.Id == id, ct);

                    if (vehicle is null) return VehicleErrors.NotFound(ctx); 

                    if (vehicle.Xmin != expectedXmin) return GeneralErrors.ConcurrencyError(ctx);

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
                .EnforceJsonContent()
                .RequireRowVersion()
                .RequireVehicleOwner()
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
                    var expectedXmin = ctx.GetXmin();

                    // We need to make sure the permit belongs to specified user
                    var vehicle = await db.Vehicles.SingleOrDefaultAsync(v => v.Id == id, ct);

                    if (vehicle is null)    return VehicleErrors.Forbidden(ctx);

                    if (vehicle.Xmin != expectedXmin)   return GeneralErrors.ConcurrencyError(ctx);

                    var userVehicleCount = await db.Vehicles.AsNoTracking().CountAsync(v => v.OwnerId == userId, ct);

                    if (userVehicleCount <= 1)  return VehicleErrors.CantRemove(ctx);


                    db.Vehicles.Remove(vehicle);
                    await db.SaveChangesAsync(ct);

                    return Results.NoContent();
                })
                .EnforceJsonContent()
                .RequireRowVersion()
                .RequireUser()
                .RequireVehicleOwner()
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
