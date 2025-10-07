
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Api.Endpoints.Support;
using ParkSpotTLV.Api.Services.Evaluation.Strategies;
using ParkSpotTLV.Contracts.Budget;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Contracts.Parking;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;

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
                async (HttpContext ctx, AppDbContext db, IDailyBudgetService budget, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    var vehicles = await db.Users.Where(u => u.Id == userId).Select(u => u.Vehicles).FirstOrDefaultAsync(ct);
                    if (vehicles is null) 
                        return Results.BadRequest();

                    var vehicleIds = vehicles.Select(v => v.Id).ToList();

                    var active = await db.ParkingSession
                        .AsNoTracking()
                        .Where(s => vehicleIds.Contains(s.VehicleId) && s.StoppedLocal == null)
                        .Select(s => new {
                            SessionId = s.Id,
                            s.VehicleId,
                            StartTime = s.StartedLocal,
                            EndTime = s.PlannedEndLocal
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
                async (Guid id, HttpContext ctx, AppDbContext db, IDailyBudgetService budget, CancellationToken ct) => {

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
                        .Where(s => s.VehicleId == id && s.StoppedLocal == null)
                        .OrderByDescending(s => s.StartedLocal)
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
                        ParkingStarted = activeSession.StartedLocal,
                        ParkingUntil = activeSession.PlannedEndLocal
                    });
                })
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .WithSummary("Parking Status")
                .WithDescription("Checks if there is an existing parking session"); 


            /* Post /start  Start Parking
             * Accepts: StartParkingRequest
             * Returns: 
             *      200 With ParkingResponse.
             *      401 Unauthorized user (Access Token not active).
             *      403 User is not owner or not found vehicle.
             *      409 An active session already exists for this vehicle.
             */
            group.MapPost("/start",
                async ([FromBody] StartParkingRequest body, HttpContext ctx, AppDbContext db, IDailyBudgetService budget, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

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
                        .AnyAsync(s => s.VehicleId == body.VehicleId && s.StoppedLocal == null, ct);

                    if (activeSession)
                        return SessionProblems.Exists(ctx);

                    // Shouldn't happen... but lets check just in case if street is unavailable for parking?
                    var seg = body.Segment;
                    var group = seg.Group?.ToUpperInvariant();
                    if (group is "RESTRICTED")
                        return SessionProblems.Unavailable(ctx);

                    // Get the current time and set it according to correct time zone for table
                    var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem"));
                    var anchor = ParkingBudgetTimeHandler.AnchorDateFor(nowLocal);
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

                    // Check the settings for notification
                    DateTimeOffset? notifyAtUtc = null;
                    var notifyBy = body.NotificationMinutes.GetValueOrDefault();
                    if (notifyBy >= 30 && notifyBy < minParking) {
                        var notifyAtLocal = nowLocal.AddMinutes(minParking - notifyBy);
                        notifyAtUtc = notifyAtLocal.ToUniversalTime();
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
                        NextChange = TimeZoneInfo.ConvertTime((DateTimeOffset)seg.NextChange!, TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem")).ToUniversalTime(),
                        StartedLocal = nowLocal.ToUniversalTime(),
                        StoppedLocal = null,
                        PlannedEndLocal = endParkingTime.ToUniversalTime(),
                        NotificationMinutes = notifyBy,

                        ParkingBudgetUsed = 0,
                        PaidMinutes = 0,
                        Status = ParkingSessionStatus.Active,
                        CreatedAtUtc = DateTimeOffset.UtcNow,
                        UpdatedAtUtc = DateTimeOffset.UtcNow
                    };

                    db.ParkingSession.Add(session);
                    Guid? notificationId = null;
                    if (notifyAtUtc is not null) {
                        var notification = new ParkingNotification {
                            Id = Guid.NewGuid(),
                            SessionId = session.Id,
                            NotifyAt = (DateTimeOffset)notifyAtUtc,
                            NotificationMinutes = notifyBy,
                            IsSent = false,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        db.ParkingNotification.Add(notification);
                    }

                    await db.SaveChangesAsync(ct);
                    return Results.Created($"/parking/{session.Id}", new StartParkingResponse {
                        
                        NameEnglish = seg.NameEnglish is null ? "" : seg.NameEnglish,
                        NameHebrew = seg.NameHebrew is null ? "" : seg.NameHebrew,
                        ZoneCode = seg.ZoneCode,
                        Group = seg.Group!,
                        Tariff = seg.Tariff,
                        FreeBudgetRemaining = remaining,
                        SessionStarted = nowLocal.ToUniversalTime(),
                        SessionEnding = endParkingTime.ToUniversalTime(),
                        NotifyAt = notifyAtUtc,
                        SegmentId = seg.SegmentId,
                        SessionId = session.Id,
                        VehicleId = body.VehicleId,
                        NotificationId = notificationId
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
                async ([FromBody] StopParkingRequest body, HttpContext ctx, AppDbContext db, IDailyBudgetService budget, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    var localTime = DateTimeOffset.Now;

                    // Check vehicle ownership
                    var ownsVehicle = await db.Vehicles.AnyAsync(v => v.OwnerId == userId && v.Id == body.VehicleId, ct);
                    if (!ownsVehicle) 
                        return Results.Forbid();
                    
                    // Check the session exists
                    var session = await db.ParkingSession.SingleOrDefaultAsync(s => s.Id == body.SessionId && s.VehicleId == body.VehicleId && s.StoppedLocal == null, ct);

                    if (session is null)
                        return SessionProblems.NotFound(ctx);

                    // Basic checks that shouldnt even happen in the first case

                    if (session.StartedLocal is null)
                        return SessionProblems.Invalid(ctx);

                    // If the current time is not legal compared to the start time of the session
                    if (localTime <= session.StartedLocal) {
                        session.StoppedLocal = localTime;           
                        session.Status = ParkingSessionStatus.Stopped;
                        session.UpdatedAtUtc = localTime;

                        await CancelFutureNotificationsAsync(db, session.Id, ct);
                        await db.SaveChangesAsync(ct);

                        return Results.Ok(
                            new StopParkingResponse {
                                SessionId = session.Id,
                                VehicleId = session.VehicleId,
                                StartedLocalUtc = session.StartedLocal!.Value,
                                StoppedLocalUtc = session.StoppedLocal!.Value,
                                TotalMinutes = 0,
                                FreeMinutesCharged = 0,
                                PaidMinutes = 0,
                                RemainingToday = await budget.GetRemainingMinutesAsync(session.VehicleId, ParkingBudgetTimeHandler.AnchorDateFor(localTime), ct)
                            });
                    }

                    // Now we calculate for legal sessions, the correct start/stop/consumption
                    DateTimeOffset? consumeStart = null;
                    DateTimeOffset? consumeEnd = localTime;

                    // If free -> dont start consuming at all
                    // If Paid -> if pay now, start consuming from start of session, else start consuming from next change (if it is valid)
                    // Either way, and consumptions end at DataTimeOffset.Now
                    var isFreeGroup = session.Group.Equals("FREE", StringComparison.OrdinalIgnoreCase);
                    var isLimitedGroup = session.Group.Equals("LIMITED", StringComparison.OrdinalIgnoreCase);
                    if (!isFreeGroup) {
                        if (session.IsPayNow) {
                            consumeStart = session.StartedLocal;
                            if (!session.IsPayLater) {
                                consumeEnd = session.NextChange;
                            }
                        } else if (session.IsPayLater && session.NextChange < localTime) {
                            consumeStart = session.NextChange;
                        }
                    }

                    if (isLimitedGroup)
                        consumeEnd = session.NextChange;

                    // The total parking minutes
                    var totalLegalMinutes = (int)Math.Ceiling((localTime - session.StartedLocal.Value).TotalMinutes);
                    var freeMinutesCharged = 0;
                    var remainingToday = 0;

                    // Since the daily budget is 8am-8pm, we need to 'cut' the parking session into 'slices' (before 8am and after)
                    foreach (var (sliceStart, sliceEnd) in ParkingBudgetTimeHandler.SliceByAnchorBoundary((DateTimeOffset)session.StartedLocal, localTime)) {
                        // If we do not start consuming free budget at all, we break
                        if (consumeStart is null || consumeEnd is null) break;

                        // Calculate the correct start and end times, and the amount of eligibile minutes for calculations
                        var start = sliceStart > consumeStart.Value ? sliceStart : consumeStart.Value;
                        var end = sliceEnd < consumeEnd.Value ? sliceEnd : consumeEnd.Value; 
                        if (end <= start) continue;

                        var eligibleMinutes = (int)Math.Ceiling((end - start).TotalMinutes);
                        if (eligibleMinutes <= 0) continue;

                        // Ensure reset (if required) and get the budget remaining
                        var anchor = ParkingBudgetTimeHandler.AnchorDateFor(sliceStart);
                        await budget.EnsureResetAsync(session.VehicleId, anchor, ct);
                        var remaining = await budget.GetRemainingMinutesAsync(session.VehicleId, anchor, ct);

                        // Calculate the consumption
                        var toConsume = Math.Min(remaining, eligibleMinutes);
                        if (toConsume > 0) {
                            // Consume against this anchor day (your service splits correctly if needed)
                            await budget.ConsumeAsync(session.VehicleId, start, start.AddMinutes(toConsume), ct);
                            freeMinutesCharged += toConsume;
                        }

                        // If we finished consuming and the anchor time is today, we reset the remaining today budget
                        if (anchor == ParkingBudgetTimeHandler.AnchorDateFor(localTime))
                            remainingToday = await budget.GetRemainingMinutesAsync(session.VehicleId, anchor, ct);
                    }

                    // Update the session values
                    var paidMinutes = Math.Max(0, totalLegalMinutes - freeMinutesCharged);
                    session.ParkingBudgetUsed += freeMinutesCharged;
                    session.PaidMinutes += paidMinutes;
                    session.StoppedLocal = localTime;           // store UTC
                    session.Status = ParkingSessionStatus.Stopped;
                    session.UpdatedAtUtc = localTime;

                    await CancelFutureNotificationsAsync(db, session.Id, ct);
                    await db.SaveChangesAsync(ct);

                    return Results.Ok(new StopParkingResponse {
                        SessionId = session.Id,
                        VehicleId = session.VehicleId,
                        StartedLocalUtc = session.StartedLocal!.Value,
                        StoppedLocalUtc = session.StoppedLocal!.Value,
                        TotalMinutes = totalLegalMinutes,
                        FreeMinutesCharged = freeMinutesCharged,
                        PaidMinutes = paidMinutes,
                        RemainingToday = remainingToday
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

       

        static async Task CancelFutureNotificationsAsync(AppDbContext db, Guid sessionId, CancellationToken ct) {
            var nowUtc = DateTimeOffset.UtcNow;
            var future = await db.ParkingNotification
                .Where(n => n.SessionId == sessionId && !n.IsSent && n.NotifyAt > nowUtc)
                .ToListAsync(ct);
            if (future.Count > 0) db.ParkingNotification.RemoveRange(future);
        }


    }

}
