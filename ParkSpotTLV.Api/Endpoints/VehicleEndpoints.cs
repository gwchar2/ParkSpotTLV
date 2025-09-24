using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Contracts.Vehicles;
using ParkSpotTLV.Core.Models;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;
using System.Security.Claims;



namespace ParkSpotTLV.Api.Endpoints {
    public static class VehicleEndpoints {

        public static IEndpointRouteBuilder MapVehicles (this IEndpointRouteBuilder routes) {
            var group = routes.MapGroup("/vehicles").WithTags("Vehicles").RequireAuthorization();


            /* GET /vehicles -> Returns only vehicles owned by the authenticated user (OwnerId == userId). */
            group.MapGet("/", async (
                HttpContext ctx,
                AppDbContext db,
                CancellationToken ct) => {
                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    Console.WriteLine($"sub={sub}");
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Unauthorized();

                    var items = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.OwnerId == userId)
                        .Include(v => v.Permits)
                        .Select(v => new VehicleResponse {
                            Id = v.Id,
                            Type = v.Type,
                            ResidentZoneCode = v.Permits
                                .Where(p => p.IsActive && p.Type == PermitType.ZoneResident)
                                .Select(p => p.ZoneCode)
                                .FirstOrDefault(),
                            DisabledPermit = v.Permits.Any(p => p.IsActive && p.Type == PermitType.Disability),
                            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(v.Xmin))
                        })
                        .ToListAsync(ct);
                    return Results.Ok(items);
                })
                .WithSummary("List Vehicles")
                .WithDescription("List caller's vehicles.")
                .Produces<List<VehicleResponse>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized);

            /* GET /vehicles/{id} —> Gets a single vehicle  */
            group.MapGet("/{id:guid}", async (
                Guid id,
                HttpContext ctx,
                AppDbContext db,
                CancellationToken ct) => {
                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Unauthorized();

                    var ownerId = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == id)
                        .Select(v => v.OwnerId)
                        .SingleOrDefaultAsync(ct);

                    if (ownerId == Guid.Empty) return Results.NotFound();
                    if (ownerId != userId) return Results.StatusCode(StatusCodes.Status403Forbidden);

                    var dto = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == id)
                        .Include(v => v.Permits)
                        .Select(v => new VehicleResponse {
                            Id = v.Id,
                            Type = v.Type,
                            ResidentZoneCode = v.Permits
                                .Where(p => p.IsActive && p.Type == PermitType.ZoneResident)
                                .Select(p => p.ZoneCode)
                                .FirstOrDefault(),
                            DisabledPermit = v.Permits.Any(p => p.IsActive && p.Type == PermitType.Disability),
                            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(v.Xmin))
                        })
                        .SingleAsync(ct);

                    return Results.Ok(dto);
                })
                .Accepts<Guid>("application/json")
                .Produces<VehicleResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Get Vehicle")
                .WithDescription("Get a single vehicle owned by the caller.");

            /* POST /vehicles -> Creates a vehicle! */
            group.MapPost("/", async (
                VehicleCreateRequest body,
                HttpContext ctx,
                AppDbContext db,
                CancellationToken ct) => {

                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);           // Configured in line 113 program.cs & line 38 JwtService.cs
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Unauthorized();

                    var hasResidency = body.ResidentZoneCode.HasValue;
                    var hasDisability = body.HasDisabledPermit;

                    // If residency provided, validate that Zone.Code exists
                    if (hasResidency) {
                        var exists = await db.Zones
                            .AsNoTracking()
                            .AnyAsync(z => z.Code == body.ResidentZoneCode!.Value, ct);

                        if (!exists) {
                            return Results.Problem(
                                title: "Invalid zone code",
                                detail: $"Zone code {body.ResidentZoneCode} does not exist.",
                                statusCode: StatusCodes.Status400BadRequest);
                        }
                    }

                    // Create a new vehicle with a list of permits
                    var vehicle = new Vehicle {
                        OwnerId = userId,
                        Type = body.Type,
                        Permits = new List<Permit>()
                    };

                    // If the vehicle has residencypermit, add it.
                    if (hasResidency) {
                        vehicle.Permits.Add(new Permit {
                            Type = PermitType.ZoneResident,
                            ZoneCode = body.ResidentZoneCode!.Value,
                            IsActive = true,
                            Vehicle = vehicle
                        });
                    }

                    // If the vehicle has disability permit, add it.
                    if (hasDisability) {
                        vehicle.Permits.Add(new Permit {
                            Type = PermitType.Disability,
                            IsActive = true,
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
                            Type = v.Type,
                            ResidentZoneCode = v.Permits
                                .Where(p => p.IsActive && p.Type == PermitType.ZoneResident)
                                .Select(p => p.ZoneCode)
                                .FirstOrDefault(),
                            DisabledPermit = v.Permits.Any(p => p.IsActive && p.Type == PermitType.Disability),
                            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(v.Xmin))
                        })
                        .SingleAsync(ct);

                    return Results.Created($"/vehicles/{dto.Id}", dto);
                })
                .Accepts<VehicleCreateRequest>("application/json")
                .Produces<VehicleResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .WithSummary("Add Vehicle")
                .WithDescription("Create a vehicle (permits optional; zero permitted).");


            /* PATCH /vehicles/{id} -> Updates a vehicle. */
            group.MapPatch("/{id:guid}", async (
                Guid id,
                VehicleUpdateRequest body,
                HttpContext ctx,
                AppDbContext db,
                CancellationToken ct) => {
                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Unauthorized();

                    // Get the row version before updating a vehicle (making sure there isnt race conditions)
                    if (string.IsNullOrWhiteSpace(body.RowVersion))
                        return Results.Problem(
                            title: "Missing rowVersion",
                            statusCode: StatusCodes.Status400BadRequest);


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

                    // Get the vehicle we want to update from the DB with its permits
                    var vehicle = await db.Vehicles
                        .Include(v => v.Permits)
                        .SingleOrDefaultAsync(v => v.Id == id, ct);

                    if (vehicle == null)
                        return Results.NotFound();

                    if (vehicle.OwnerId != userId)
                        return Results.StatusCode(StatusCodes.Status403Forbidden);

                    // Check that there is no race condition conflicts
                    if (vehicle.Xmin != expectedXmin)
                        return Results.Conflict(new {
                            message = "The vehicle was modified by another request. Reload and try again."
                        });

                    // Apply updates
                    if (body.Type.HasValue)
                        vehicle.Type = body.Type.Value;

                    // Residency permit handling for update
                    // - Not sent      -> no change
                    // - Sent = null   -> remove residency permit (if any); never leave a permit without a code
                    // - Sent = int    -> validate zone exists; add or update residency permit with that code
                    if (body.ResidentZoneCode is not null) // property included in PATCH
                    {
                        var resident = vehicle.Permits.FirstOrDefault(p => p.IsActive && p.Type == PermitType.ZoneResident);

                        if (body.ResidentZoneCode is null) {
                            // If the residence permit is NOT null, remove it. (If receive body.ResidentZoneCode == null && resident != null -> Removes anyways!)
                            if (resident != null) vehicle.Permits.Remove(resident);
                        } else {
                            var zoneCode = body.ResidentZoneCode.Value;

                            var exists = await db.Zones.AsNoTracking()
                                .AnyAsync(z => z.Code == zoneCode, ct);
                            if (!exists) {
                                return Results.Problem(
                                    title: "Invalid zone code",
                                    detail: $"Zone code {zoneCode} does not exist.",
                                    statusCode: StatusCodes.Status400BadRequest);
                            }

                            if (resident == null) {
                                vehicle.Permits.Add(new Permit {
                                    Type = PermitType.ZoneResident,
                                    ZoneCode = zoneCode,
                                    IsActive = true,
                                    Vehicle = vehicle
                                });
                            } else resident.ZoneCode = zoneCode;

                        }
                    }

                    // Disability permit
                    if (body.DisabledPermit.HasValue) {
                        var disabilityPermit = vehicle.Permits.FirstOrDefault(p => p.Type == PermitType.Disability && p.IsActive);
                        if (body.DisabledPermit.Value) {
                            if (disabilityPermit == null) {
                                vehicle.Permits.Add(new Permit {
                                    Type = PermitType.Disability,
                                    IsActive = true,
                                    Vehicle = vehicle
                                });
                            }
                        } else
                            if (disabilityPermit != null) vehicle.Permits.Remove(disabilityPermit);
                    }

                    // Save our changes
                    await db.SaveChangesAsync(ct);

                    // Return OK with updated vehicle
                    var dto = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == id)
                        .Include(v => v.Permits)
                        .Select(v => new VehicleResponse {
                            Id = v.Id,
                            Type = v.Type,
                            ResidentZoneCode = v.Permits
                                .Where(p => p.IsActive && p.Type == PermitType.ZoneResident)
                                .Select(p => p.ZoneCode)
                                .FirstOrDefault(),
                            DisabledPermit = v.Permits.Any(p => p.IsActive && p.Type == PermitType.Disability),
                            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(v.Xmin))
                        })
                        .SingleAsync(ct);

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

            /* DELETE /vehicles/{id} — Delete a vehicle */
            group.MapDelete("/{id:guid}", async (
                Guid id,
                [FromBody] VehicleDeleteRequest body,
                HttpContext ctx,
                AppDbContext db,
                CancellationToken ct) => {
                    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!Guid.TryParse(sub, out var userId))
                        return Results.Unauthorized();

                    // We make sure that RowVersion exists and is legal, so there are no race conditions
                    if (string.IsNullOrWhiteSpace(body.RowVersion))
                        return Results.Problem(title: "Missing rowVersion", statusCode: StatusCodes.Status400BadRequest);

                    uint expectedXmin;
                    try {
                        expectedXmin = BitConverter.ToUInt32(Convert.FromBase64String(body.RowVersion));
                    }
                    catch {
                        return Results.Problem(title: "Invalid rowVersion format", statusCode: StatusCodes.Status400BadRequest);
                    }

                    // We load the vehicle from the DB
                    var vehicle = await db.Vehicles.SingleOrDefaultAsync(v => v.Id == id, ct);
                    if (vehicle == null)
                        return Results.NotFound();

                    // We check if the person requesting is truely the owner
                    if (vehicle.OwnerId != userId)
                        return Results.StatusCode(StatusCodes.Status403Forbidden);

                    // We check there are no race conditions
                    if (vehicle.Xmin != expectedXmin)
                        return Results.Conflict(new {
                            message = "The vehicle was modified by another request. Reload and try again."
                        });

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

/*
 * {
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzZTZlZTQ1YS0yYmFlLTQ3MTctOTY5ZC0xOGFiZmUwMzJhY2MiLCJuYW1lIjoiYXR0ZW1wdDI0IiwianRpIjoiMWY4ZWQ0NjYtMzg3NC00ZDdhLWJmNzUtNjhhNGFmMWNmYTRhIiwiaWF0IjoxNzU4Njc2MzY3LCJuYmYiOjE3NTg2NzYzNjcsImV4cCI6MTc1ODY3Njk2NywiaXNzIjoiUGFya1Nwb3RUTFYiLCJhdWQiOiJQYXJrU3BvdFRMVi5BcHAifQ.jQ_W_fsj0zSA3IcL7-WaLwdxlBAjGD8JMrVT9fD5zeo",
  "accessTokenExpiresAt": "2025-09-24T01:22:47.6842383+00:00",
  "refreshToken": "kSCk8YKZJl22T2nByIJgf0XMw8se_8hUrmbe-mwuTQI",
  "refreshTokenExpiresAt": "2025-10-08T01:12:47.6959366+00:00",
  "tokenType": "Bearer"
}*/