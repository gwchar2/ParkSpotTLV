
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Api.Endpoints.Support;
using ParkSpotTLV.Api.Features.Parking.Services;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Parking;
using ParkSpotTLV.Contracts.Time;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;
using Serilog;

namespace ParkSpotTLV.Api.Endpoints {

    public static class ParkingEndpoints {
        public static IEndpointRouteBuilder MapParking(this IEndpointRouteBuilder routes) {

            var group = routes.MapGroup("/parking").WithTags("Parking Related Requests").RequireAuthorization().RequireUser();


            /* Get /sessions  All Sessions
             * Accepts: User Id
             * Returns: 
             *      200 A list of all active sessions for a specific user
             */
            group.MapGet("/sessions/",
                async (HttpContext ctx, AppDbContext db, IDailyBudgetService budget,IClock clock, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    var vehicles = await db.Users.Where(u => u.Id == userId).Select(u => u.Vehicles).FirstOrDefaultAsync(ct);
                    if (vehicles is null) 
                        return Results.BadRequest();

                    var vehicleIds = vehicles.Select(v => v.Id).ToList();

                    var active = await db.ParkingSession
                        .AsNoTracking()
                        .Where(s => vehicleIds.Contains(s.VehicleId) && s.StoppedUtc == null)
                        .Select(s => new {
                            SessionId = s.Id,
                            s.VehicleId,
                            StartTime = clock.ToLocal(s.StartedUtc),
                            EndTime = clock.ToLocal(s.PlannedEndUtc)
                        })
                        .ToListAsync(ct);

                    if (active is null)
                        return Results.NotFound();

                    return Results.Ok(active);

                })
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Sessions List")
                .WithDescription("Returns all active parking sessions for a user");

            /* Get /status  Checks if has an active parking session
             * Accepts: VehicleId
             * Returns: 
             *      200 Returns True with current 'snapshot' or false if no session.
             *      403 If user is not owner of vehicle or vehicle does not exist.
             */
            group.MapGet("/status/{id:guid}",
                async (Guid id, HttpContext ctx, AppDbContext db, IDailyBudgetService budget, IClock clock, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    // Check ownership of vehicle
                    var ownerId = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == id)
                        .Select(v => v.OwnerId)
                        .SingleOrDefaultAsync(ct);

                    if (ownerId == Guid.Empty || ownerId != userId) return VehicleProblems.Forbidden(ctx);

                    // Check if there is already an active session for this vehicle
                    var activeSession = await db.ParkingSession
                        .AsNoTracking()
                        .Where(s => s.VehicleId == id && s.StoppedUtc == null)
                        .OrderByDescending(s => s.StartedUtc)
                        .FirstOrDefaultAsync(ct);

                    if (activeSession is null) 
                        return Results.Ok(new {
                            Status = false
                        });


                    // 3) Grab minutes used for that vehicle on that anchor day
                    var minutesUsed = await db.ParkingDailyBudget
                        .AsNoTracking()
                        .Where(b => b.VehicleId == id)
                        .Select(b => (int?)b.MinutesUsed)
                        .FirstOrDefaultAsync(ct) ?? 0;

                    // 4) Return payload
                    return Results.Ok(new {
                        Status = true,
                        SessionId = activeSession.Id,
                        ParkingStarted = clock.ToLocal(activeSession.StartedUtc),
                        ParkingUntil = clock.ToLocal(activeSession.PlannedEndUtc)
                    });
                })
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .WithSummary("Parking Status")
                .WithDescription("Checks if there is an existing parking session");


            /* Post /budget-remaining  BUDGET REMAINING
            * Accepts:  TimeRemaining = remaining
            * Returns: 
            *      200 TimeRemaining
            *      401 Unauthorized access - Access token is expired or no such user.
            *      403 User is not owner of vehicle or session.
            *      404 Not Found - Vehicle not found for ID id 
            */
            group.MapGet("/budget-remaining/{id:guid}",
                async (Guid Id, HttpContext ctx, AppDbContext db, IDailyBudgetService budget,IClock clock, CancellationToken ct) => {
                    var userId = ctx.GetUserId();

                    // Check vehicle ownership
                    var ownsVehicle = await db.Vehicles.AnyAsync(v => v.OwnerId == userId && v.Id == Id, ct);
                    if (!ownsVehicle)
                        return Results.Forbid();

                    var session = await db.ParkingSession.SingleOrDefaultAsync(s => s.VehicleId == Id && s.StoppedUtc == null, ct);

                    // daily remaining
                    var nowLocal = clock.LocalNow;
                    var today = DateOnly.FromDateTime(nowLocal.LocalDateTime);
                    var remaining = await budget.GetRemainingMinutesAsync(Id, today, ct);

                    if (session is not null) {
                        var outcome = await budget.CalculateAsync(session, ct);
                        remaining = outcome.RemainingToday;
                    }

                    return Results.Ok(new {
                        TimeRemaining = remaining   // If we are under 0 remaining, remaining is just 0.
                    });

                })
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Free Parking Budget")
                .WithDescription("Retreives a vehicles free parking budget");



            /* Post /start  Start Parking
             * Accepts: StartParkingRequest
             * Returns: 
             *      200 With ParkingResponse.
             *      401 Unauthorized user (Access Token not active).
             *      403 User is not owner or not found vehicle.
             *      409 An active session already exists for this vehicle.
             */
            group.MapPost("/start",
                async ([FromBody] StartParkingRequest body, HttpContext ctx, AppDbContext db, IDailyBudgetService budget, IClock clock, CancellationToken ct) => {

                    var userId = ctx.GetUserId();
                    var nowLocal = clock.LocalNow;

                    // Check ownership of vehicle
                    var ownerId = await db.Vehicles
                        .AsNoTracking()
                        .Where(v => v.Id == body.VehicleId)
                        .Select(v => v.OwnerId)
                        .SingleOrDefaultAsync(ct);

                    if (ownerId == Guid.Empty || ownerId != userId) return VehicleProblems.Forbidden(ctx);

                    // Check if there is already an active session for this vehicle
                    var activeSession = await db.ParkingSession
                        .AsNoTracking()
                        .AnyAsync(s => s.VehicleId == body.VehicleId && s.StoppedUtc == null, ct);

                    if (activeSession)
                        return SessionProblems.Exists(ctx);

                    // Shouldn't happen... but lets check just in case if street is unavailable for parking?
                    var seg = body.Segment;
                    var group = seg.Group?.ToUpperInvariant();
                    if (group is "RESTRICTED")
                        return SessionProblems.Unavailable(ctx);

                    // Get the current time and set it according to correct time zone for table
                    var anchor = budget.ToAnchor(nowLocal);
                    await budget.EnsureResetAsync(body.VehicleId, anchor, ct);

                    // Calculate the minimum parking time compared to input & calculate end parking time
                    var minParking = body.MinParkingTime.GetValueOrDefault(120);
                    if (minParking <= 0) minParking = 120;
                    var endParkingTime = nowLocal.AddMinutes(minParking);

                    // FREE -> DOES NOT CONSUME BUDGET
                    // PAYED -> if isPayNow true  -> starts consuming.
                    // PAYED -> if isPayNow false -> starts consuming after change (NextChange).
                    // LIMITED -> according to isPayNow now. same as PAYED fields, just for limited. Furthermore, if nextChange < endParkingTime, than endParkingTime = nextChange
                    if (group is "LIMITED" && seg.NextChange is not null && seg.NextChange > nowLocal && seg.NextChange < endParkingTime)
                        endParkingTime = (DateTimeOffset)seg.NextChange;

                    DateTimeOffset? budgetTimeLimit = null;
                    var remaining = await budget.GetRemainingMinutesAsync(body.VehicleId, anchor, ct);
                    // If we do not have any remaining budget, we don't have anything to estimate
                    if (remaining > 0) {
                        if (group is "PAID" or "LIMITED") {
                            DateTimeOffset consumeStart = nowLocal;

                            if (seg.IsPayNow is bool isPayNowFlag) {
                                if (!isPayNowFlag)
                                    consumeStart = (DateTimeOffset)seg.NextChange!;
                                else
                                    consumeStart = nowLocal;
                            } 
                            else 
                                consumeStart = nowLocal;

                            if (consumeStart < endParkingTime) {
                                var estimate = consumeStart.AddMinutes(remaining);

                                budgetTimeLimit = estimate <= endParkingTime ? estimate : endParkingTime;
                            }
                        }
                        // group FREE -> no consumption ->  budgetLeft stays null
                    }

                    // Create the sessions
                    var session = new ParkingSession {
                        Id = Guid.NewGuid(),
                        VehicleId = body.VehicleId,
                        SegmentId = seg.SegmentId,

                        Group = seg.Group!,
                        Reason = seg.Reason,
                        ParkingType = Enum.Parse<ParkingType>(seg.ParkingType, ignoreCase: true),
                        ZoneCode = seg.ZoneCode,
                        Tariff = Enum.Parse<Tariff>(seg.Tariff, ignoreCase: true),

                        IsPayNow = seg.IsPayNow,
                        IsPayLater = seg.IsPaylater,
                        NextChangeUtc = clock.ToUtc(seg.NextChange),
                        StartedUtc = clock.UtcNow,
                        StoppedUtc = null,
                        PlannedEndUtc = clock.ToUtc(endParkingTime),

                        ParkingBudgetUsed = 0,
                        PaidMinutes = 0,
                        Status = ParkingSessionStatus.Active,
                        CreatedAtUtc = clock.UtcNow,
                        UpdatedAtUtc = clock.UtcNow
                    };
                    db.ParkingSession.Add(session);

                    await db.SaveChangesAsync(ct);
                    return Results.Created($"/parking/{session.Id}", new StartParkingResponse {

                        NameEnglish = seg.NameEnglish is null ? "" : seg.NameEnglish,
                        NameHebrew = seg.NameHebrew is null ? "" : seg.NameHebrew,
                        ZoneCode = seg.ZoneCode,
                        Group = seg.Group!,
                        Tariff = seg.Tariff,
                        FreeBudgetRemaining = remaining,
                        SessionStarted = nowLocal, 
                        SessionEnding = endParkingTime,
                        SegmentId = seg.SegmentId,
                        SessionId = session.Id,
                        VehicleId = body.VehicleId
                    });
                })
                .Accepts<StartParkingRequest>("application/json")
                .Produces<StartParkingResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .WithSummary("Start Parking")
                .WithDescription("Starts parking at specific segment");


        /* Post /stop  Stop Parking
        * Accepts: Permit ID
        * Returns: 
        *      200 StopParkingResponse - Returns a summary of the session.
        *      401 Unauthorized access - Access token is expired or no such user.
        *      403 User is not owner of vehicle or session.
        *      404 Not Found - Permit not found for ID id 
        */
        group.MapPost("/stop/",
                async ([FromBody] StopParkingRequest body, HttpContext ctx, AppDbContext db, IDailyBudgetService budget,IClock clock, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    // Check vehicle ownership
                    var ownsVehicle = await db.Vehicles.AnyAsync(v => v.OwnerId == userId && v.Id == body.VehicleId, ct);
                    if (!ownsVehicle) 
                        return Results.Forbid();
                    
                    // Check the session exists
                    var session = await db.ParkingSession.SingleOrDefaultAsync(s => s.Id == body.SessionId && s.VehicleId == body.VehicleId && s.StoppedUtc == null, ct);

                    if (session is null)
                        return SessionProblems.NotFound(ctx);

                    DateTimeOffset timeLocal = clock.LocalNow;
                    DateTimeOffset startedLocal = clock.ToLocal(session.StartedUtc);
                    DateTimeOffset nextLocal = clock.ToLocal(session.NextChangeUtc);

                    // If the current time is not legal compared to the start time of the session
                    if (timeLocal <= startedLocal) {
                        session.StoppedUtc = clock.UtcNow;           
                        session.Status = ParkingSessionStatus.Stopped;
                        session.UpdatedAtUtc = clock.UtcNow;
                        await db.SaveChangesAsync(ct);

                        return Results.Ok(
                            new StopParkingResponse {
                                SessionId = session.Id,
                                VehicleId = session.VehicleId,
                                StartedLocal = startedLocal,
                                StoppedLocal = clock.ToLocal(session.StoppedUtc),
                                TotalMinutes = 0,
                                FreeMinutesCharged = 0,
                                PaidMinutes = 0,
                                RemainingBudgetToday = await budget.GetRemainingMinutesAsync(session.VehicleId, budget.ToAnchor(timeLocal), ct)
                            });
                    }

                    var outcome = await budget.CalculateAsync(session, ct);

                    session.ParkingBudgetUsed += outcome.FreeMinutesCharged;
                    session.PaidMinutes += outcome.PaidMinutes;
                    session.StoppedUtc = clock.UtcNow;
                    session.Status = ParkingSessionStatus.Stopped;
                    session.UpdatedAtUtc = clock.UtcNow;

                    await db.SaveChangesAsync(ct);

                    return Results.Ok(new StopParkingResponse {
                        SessionId = session.Id,
                        VehicleId = session.VehicleId,
                        StartedLocal = clock.ToLocal(session.StartedUtc),
                        StoppedLocal = clock.ToLocal(session.StoppedUtc),
                        TotalMinutes = outcome.TotalMinutes,
                        PaidMinutes = outcome.PaidMinutes,
                        FreeMinutes = outcome.FreeMinutes,
                        FreeMinutesCharged = outcome.FreeMinutesCharged,
                        RemainingBudgetToday = outcome.RemainingToday
                    });

                })
                .Accepts<StopParkingRequest>("application/json")
                .Produces<StopParkingResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Stop Parking")
                .WithDescription("Stops a parking session");

            return group;
        }


    }

}
