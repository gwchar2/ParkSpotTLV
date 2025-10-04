
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Contracts.Vehicles;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;
using ParkSpotTLV.Contracts.Enums;
using System.Security.Claims;



namespace ParkSpotTLV.Api.Endpoints {
    public static class VehicleEndpoints {

        public static IEndpointRouteBuilder MapVehicles (this IEndpointRouteBuilder routes) {
            var group = routes.MapGroup("/vehicles").WithTags("Vehicle Requests").RequireAuthorization();

            /* GET /VEHICLES
             * Accepts: Access Token
             * Returns: 
             *      200 with List<VehicleResponse>.
             *      401 if access token is expired.
             */
            group.MapGet("/", async (
                HttpContext ctx, AppDbContext db, CancellationToken ct) => {

                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Problem(
                            title: "Invalid or expired token.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            type: "https://httpstatuses.com/401"
                            );

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

                    if (vehicles is null)
                        return Results.Problem(
                            title: "Vehicles not found or not owned by user.",
                            statusCode: StatusCodes.Status403Forbidden
                            );

                    return Results.Ok(vehicles);
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

                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Problem(
                            title: "Invalid or expired token.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            type: "https://httpstatuses.com/401"
                            );

                    var ownerId = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == id)
                        .Select(v => v.OwnerId)
                        .SingleOrDefaultAsync(ct);

                    if (ownerId == Guid.Empty) 
                        return Results.Problem(
                            title: "Vehicle not found",
                            statusCode: StatusCodes.Status404NotFound,
                            type: "https://httpstatuses.com/404"
                            );

                    if (ownerId != userId) 
                        return Results.Problem(
                            title: "Vehicle does not belong to user.",
                            statusCode: StatusCodes.Status403Forbidden
                            );

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

                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);                       // Configured in line 113 program.cs & line 38 JwtService.cs
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Problem(
                            title: "Invalid or expired token.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            type: "https://httpstatuses.com/401"
                        );

                    if (await db.Vehicles.AsNoTracking().AnyAsync(z => z.OwnerId == userId && z.Name == body.Name, ct))
                        return Results.Problem(
                            title: "Name already exists",
                            statusCode: StatusCodes.Status400BadRequest);

                    var hasResidency = body.ResidentZoneCode.HasValue;
                    var hasDisability = body.HasDisabledPermit;

                    // If residency provided, validate that Zone.Code exists
                    if (hasResidency) {
                        var exists = await db.Zones.AsNoTracking().AnyAsync(z => z.Code == body.ResidentZoneCode!.Value, ct);

                        if (!exists) {
                            return Results.Problem(
                                title: "Invalid zone code",
                                detail: $"Zone code {body.ResidentZoneCode} does not exist.",
                                statusCode: StatusCodes.Status400BadRequest);
                        }
                    }

                    // Create a new vehicle with a default permit
                    var vehicle = new Vehicle {
                        OwnerId = userId,
                        Type = body.Type,
                        Name = body.Name
                    };
                    var permit = new Permit {
                        Type = PermitType.Default,
                        Vehicle = vehicle,
                        VehicleId = vehicle.Id
                    };
                    db.Permits.Add(permit);

                    if (!hasResidency && !hasDisability) {
                        db.Vehicles.Add(vehicle);
                        await db.SaveChangesAsync(ct);
                    }

                    // If the vehicle has residencypermit, add it.
                    if (hasResidency) {
                        vehicle.Permits.Add(new Permit {
                            Type = PermitType.ZoneResident,
                            ZoneCode = body.ResidentZoneCode!.Value,
                            Vehicle = vehicle
                        });
                    }
                    // Else, If the vehicle has disability permit, add it.
                    if (hasDisability) {
                        vehicle.Permits.Add(new Permit {
                            Type = PermitType.Disability,
                            Vehicle = vehicle
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
                    // Authorize the user
                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Problem(
                            title: "Invalid or expired token.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            type: "https://httpstatuses.com/401"
                            );

                    // Check for concurrency
                    if (string.IsNullOrWhiteSpace(body.RowVersion))
                        return Results.Problem(
                            title: "Missing RowVersion", 
                            statusCode: StatusCodes.Status400BadRequest);

                    uint expectedXmin;
                    try { 
                        expectedXmin = BitConverter.ToUInt32(Convert.FromBase64String(body.RowVersion)); 
                    } catch { 
                        return Results.Problem(
                            title: "Invalid RowVersion format", 
                            statusCode: StatusCodes.Status400BadRequest); 
                    }

                    // Get the vehicle from the DB & check ownership
                    var vehicle = await db.Vehicles.Include(v => v.Permits).SingleOrDefaultAsync(v => v.Id == id, ct);

                    if (vehicle is null) 
                        return Results.Problem(
                            title: "Vehicle not found",
                            statusCode: StatusCodes.Status404NotFound,
                            type: "https://httpstatuses.com/404"
                            );

                    if (vehicle.OwnerId != userId) 
                        return Results.Problem(
                            title: "Vehicle does not belong to user.",
                            statusCode: StatusCodes.Status403Forbidden
                            );


                    if (vehicle.Xmin != expectedXmin)
                        return Results.Problem(
                            title: "Concurrency Error",
                            detail: "The permit was modified by another request. Reload and try again.",
                            statusCode: StatusCodes.Status409Conflict
                            );

                    if (body.Type.HasValue)
                        vehicle.Type = body.Type.Value;

                    if (!string.IsNullOrWhiteSpace(body.Name)) 
                        vehicle.Name = body.Name.Trim();

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

                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Problem(
                            title: "Invalid or expired token.",
                            statusCode: StatusCodes.Status401Unauthorized,
                            type: "https://httpstatuses.com/401"
                        );


                    // We make sure that RowVersion exists and is legal, so there are no race conditions
                    if (string.IsNullOrWhiteSpace(body.RowVersion))
                        return Results.Problem(
                            title: "Missing RowVersion", 
                            statusCode: StatusCodes.Status400BadRequest
                            );

                    uint expectedXmin;
                    try {
                        expectedXmin = BitConverter.ToUInt32(Convert.FromBase64String(body.RowVersion));
                    }
                    catch {
                        return Results.Problem(
                            title: "Invalid rowVersion format", 
                            statusCode: StatusCodes.Status400BadRequest
                            );
                    }

                    // We load the vehicle from the DB
                    var vehicle = await db.Vehicles.SingleOrDefaultAsync(v => v.Id == id, ct);
                    if (vehicle == null)
                        return Results.Problem(
                            title: "Vehicle not found",
                            statusCode: StatusCodes.Status404NotFound,
                            type: "https://httpstatuses.com/404"
                            );

                    // We check if the person requesting is truely the owner
                    if (vehicle.OwnerId != userId)
                        return Results.Problem(
                            title: "Vehicle does not belong to user.",
                            statusCode: StatusCodes.Status403Forbidden
                            );

                    // We check there are no race conditions
                    if (vehicle.Xmin != expectedXmin)
                        return Results.Problem(
                            title: "Concurrency Error",
                            detail: "The permit was modified by another request. Reload and try again.",
                            statusCode: StatusCodes.Status409Conflict
                            );

                    // Delete 
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
