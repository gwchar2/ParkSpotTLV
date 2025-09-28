using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Contracts.Parking;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;
using System.Security.Claims;


namespace ParkSpotTLV.Api.Endpoints {
    public static class ParkingEndpoints {
        public static IEndpointRouteBuilder MapParking(this IEndpointRouteBuilder routes) {

            var group = routes.MapGroup("/parking").WithTags("Parking Time Requests").RequireAuthorization();

            /* Get / parking -> Returns only the amount of free parking left for user(OwnerId == userId) */
            group.MapGet("/left", async(HttpContext ctx, AppDbContext db, CancellationToken ct) => {
                var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!Guid.TryParse(sub, out var userId))
                    return Results.Unauthorized();

                // Grab the parking budget & start time from the user
                var user = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new {
                        u.FreeParkingBudget,
                        u.ParkingStartedAtUtc
                    })
                    .SingleOrDefaultAsync(ct);

                // If user is null -> no such user
                if (user is null)
                    return Results.Unauthorized();

                // calculate the difference if there is currently a parking session
                if (user.ParkingStartedAtUtc is not null) {
                    var elapsed = DateTime.UtcNow - user.ParkingStartedAtUtc.Value;
                    var remaining = user.FreeParkingBudget - elapsed;
                    return Results.Ok(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
                }
                // Else, just return the parking budget
                else
                    return Results.Ok(user.FreeParkingBudget);

            })
                .WithSummary("Parking Budget")
                .WithDescription("Returns the budget left for free parking.")
                .Produces<IQueryable<TimeSpan>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized);

            /* Post / start -> Sets a free parking timer */
            group.MapPost("/start", async (HttpContext ctx, AppDbContext db, CancellationToken ct) => {
                var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(sub, out var userId))
                    return Results.Unauthorized();

                // If the user is null -> Unauthorized to make a change
                var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
                if (user == null) return Results.Unauthorized();


                // We update everything according to the current time
                var now = DateTimeOffset.UtcNow;
                user.ParkingStartedAtUtc = now;
                user.FreeParkingUntilUtc = now + user.FreeParkingBudget; 
                user.LastUpdated = now;

                await db.SaveChangesAsync(ct);

                var remaining = (int)Math.Max(0, (user.FreeParkingUntilUtc!.Value - now).TotalSeconds);

                return Results.Ok(new ParkStartRequest(
                    serverUtc : now,
                    startedAtUtc : user.ParkingStartedAtUtc,
                    freeParkingUntilUtc : user.FreeParkingUntilUtc,
                    remainingSeconds : remaining,
                    budgetMinutes : (int)user.FreeParkingBudget.TotalMinutes
                ));
            })
                .WithSummary("Start Parking")
                .WithDescription("Starts the current parking session.")
                .Produces<ParkStartRequest>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized);

            /* POST /parking/end -> Ends free-parking timer for the current user */
            group.MapPost("/end", async (HttpContext ctx, AppDbContext db, CancellationToken ct) =>
            {
                var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(sub, out var userId))
                    return Results.Unauthorized();

                var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
                if (user is null) return Results.Unauthorized();

                var now = DateTimeOffset.UtcNow;

                // Calculate how much was consumed
                if (user.ParkingStartedAtUtc is not null) {
                    var duration = now - user.ParkingStartedAtUtc.Value;
                    if (duration > TimeSpan.Zero) {
                        // Deduct from budget, clamp at zero
                        user.FreeParkingBudget = user.FreeParkingBudget - duration;
                        if (user.FreeParkingBudget < TimeSpan.Zero)
                            user.FreeParkingBudget = TimeSpan.Zero;
                    }
                }

                // Reset the data
                user.ParkingStartedAtUtc = null;
                user.FreeParkingUntilUtc = null;
                user.LastUpdated = now;

                await db.SaveChangesAsync(ct);

                return Results.Ok(new {
                    remainingMinutes = (int)user.FreeParkingBudget.TotalMinutes
                });
            })
                .WithSummary("End Parking")
                .WithDescription("Ends the current parking session and returns remaining free parking time.")
                .Produces<IQueryable<TimeSpan>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized);

            return group;
        }
    }
}
