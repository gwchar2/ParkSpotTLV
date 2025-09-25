using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Contracts.Auth;
using ParkSpotTLV.Core.Auth;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;
using System.Security.Claims;

namespace ParkSpotTLV.Api.Endpoints;

// ADD CHANGE USERNAME CHANGE PASSWORD


public static class AuthEndpoints {
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder routes) {

        var auth = routes.MapGroup("/auth").WithTags("Auth");



        /* REGISTER REQUEST 
         * Accepts: username, password
         * Returns: 
         *      201 with { accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, tokenType: "Bearer" } (or 201 without tokens if you prefer).
         *      409 if username is taken; 400 if policy fails.
         */
        auth.MapPost("/register",
            async ([FromBody] RegisterRequest body,
                   AppDbContext db,
                   IPasswordHasher hasher,
                   IJwtService jwt,
                   IRefreshTokenService refresh,
                   CancellationToken ct) => {
                       if (string.IsNullOrWhiteSpace(body.Username) || string.IsNullOrWhiteSpace(body.Password))
                           return Results.Problem(
                                title: "Username and password are required.",
                                statusCode: StatusCodes.Status400BadRequest,
                                type: "https://httpstatuses.com/400"
                           );
                       var normalized = body.Username.Trim().ToLowerInvariant();

                       var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Username == normalized, ct);
                       if (exists)
                           return Results.Problem(
                               title: "Username already taken.",
                               statusCode: StatusCodes.Status409Conflict,
                               type: "https://httpstatuses.com/409"
                           );

                       var pwdHash = hasher.Hash(body.Password);

                       var user = new User {
                           Id = Guid.NewGuid(),
                           Username = normalized,
                           PasswordHash = pwdHash
                       };

                       db.Users.Add(user);
                       await db.SaveChangesAsync(ct);
                       var access = jwt.IssueAccessToken(user.Id, user.Username);
                       var issued = refresh.Issue(user.Id);             // Issue function already inserts to table!!

                       return Results.Created("/auth/register", new TokenPairResponse(
                           access.AccessToken,
                           access.ExpiresAtUtc,
                           issued.RefreshToken,
                           issued.ExpiresAtUtc,
                           "Bearer"
                       ));
                   })
            .Accepts<RegisterRequest>("application/json")
            .Produces<TokenPairResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .WithSummary("Register new account")
            .WithDescription("Validates username/password policy, creates the user, issues an access token and a refresh token.")
            .WithOpenApi();

        /* LOGIN REQUEST
         * Accepts: username, password
         * Returns: 
         *      200 with token pair + expiries and tokenType.
         *      401 for bad creds; 400 if malformed.
         */
        auth.MapPost("/login",
            async ([FromBody] RegisterRequest body,
                   AppDbContext db,
                   IPasswordHasher hasher,
                   IJwtService jwt,
                   IRefreshTokenService refresh,
                   CancellationToken ct) => {
                       if (string.IsNullOrWhiteSpace(body.Username) || string.IsNullOrWhiteSpace(body.Password))
                           return Results.Problem(
                                title: "Username and password are required.",
                                statusCode: StatusCodes.Status400BadRequest,
                                type: "https://httpstatuses.com/401"
                           );
                       var normalized = body.Username.Trim().ToLowerInvariant();
                       
                       var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(us => us.Username == normalized, ct);
                       if (user is null)
                           return Results.Problem(
                                title: "Invalid credentials.",
                                statusCode: StatusCodes.Status401Unauthorized,
                                type: "https://httpstatuses.com/401"
                           );

                       var confirmation = hasher.Verify(body.Password, user.PasswordHash);

                       if (!confirmation.isValid)
                           return Results.Problem(
                                    title: "Invalid credentials.",
                                    statusCode: StatusCodes.Status401Unauthorized,
                                    type: "https://httpstatuses.com/401"
                                );

                       var access = jwt.IssueAccessToken(user.Id, user.Username);               // Temporary access token
                       var issued = refresh.Issue(user.Id);                                     // We state that a token has been issued

                       var response = new TokenPairResponse(
                         access.AccessToken,
                         access.ExpiresAtUtc,
                         issued.RefreshToken,
                         issued.ExpiresAtUtc,
                         "Bearer"
                        );

                       return Results.Ok(response);
                   })
            .Accepts<LoginRequest>("application/json")
            .Produces<TokenPairResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .WithSummary("Sign in")
            .WithDescription("Verifies credentials and returns a short-lived access token with a new refresh token.")
            .WithOpenApi();


        /* REFRESH REQUEST
         * Accepts: RefreshToken in body
         * Returns: 
         *      200 with new pair (on valid, first use).
         *      401 for invalid/expired/revoked/reused tokens; 400 if malformed.
         */
        auth.MapPost("/refresh",
            ([FromBody] RefreshRequest body,
                   IRefreshTokenService refreshService,
                   CancellationToken ct) => {

                       if (body is null || string.IsNullOrWhiteSpace(body.RefreshToken))
                           return Results.Problem(
                               title: "Refresh token is required",
                               statusCode: StatusCodes.Status400BadRequest,
                               type: "https://httpstatuses.com/400"
                               );

                       try {
                           var result = refreshService.ValidateAndRotate(body.RefreshToken);
                           var response = new TokenPairResponse(
                               AccessToken: result.AccessToken,
                               AccessTokenExpiresAt: result.AccessExpiresAtUtc,
                               RefreshToken: result.RefreshToken,
                               RefreshTokenExpiresAt: result.RefreshExpiresAtUtc,
                               TokenType: "Bearer"
                               );

                           return Results.Ok(response);
                       }
                       catch (UnauthorizedAccessException) {
                           return Results.Problem(
                               title: "Invalid or expired refresh token.",
                               statusCode: StatusCodes.Status401Unauthorized,
                               type: "https://httpstatuses.com/401"
                               );
                       }
                       catch (Exception ex) {
                           return Results.Problem(
                               title: ex.Message,
                               statusCode: StatusCodes.Status500InternalServerError,
                               type: "https://httpstatuses.com/500"
                               );
                       }
                   })
            .Accepts<RefreshRequest>("application/json")
            .Produces<TokenPairResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .WithSummary("Refresh tokens")
            .WithDescription("Validates and rotates the refresh token, returning a new token pair.");

        /* LOGOUT REQUEST
         * Accepts: Access token (Bearer) to identify user; either
         * 1) The current refresh token in the body to revoke just that session, or
         * 2) A flag like allDevices=true to revoke all of the user’s active refresh tokens.
         * Returns: 
         *      204 No Content on success. (Includes revoking an already-revoked token)
         *      401 if the caller has no valid access token (or user disabled).
         */
        auth.MapPost("/logout",
            ([FromBody] LogoutRequest body,
             HttpContext http,
             IRefreshTokenService refreshService) => {

                 if (body is null) 
                     return Results.Problem(
                         title: "Request body is required.",
                         statusCode: StatusCodes.Status400BadRequest,
                         type: "https://httpstatuses.com/400"
                         );


                 /* To remove ALL devices, we require a valid JWT bearer */
                 if (body.AllDevices) {
                     var subscriptions = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                         ?? http.User.FindFirstValue("subscriptions");

                     if (string.IsNullOrWhiteSpace(subscriptions) || !Guid.TryParse(subscriptions, out var userId))
                         return Results.Problem(
                             title: "Authentication required to log out all devices.",
                             statusCode: StatusCodes.Status401Unauthorized,
                             type: "https://httpstatuses.com/401"
                             );


                     try {
                         refreshService.RevokeAllForUser(userId);
                         return Results.NoContent();
                     }
                     catch (Exception) {
                         return Results.Problem(
                             title: "Unexpected error while revoking tokens.",
                             statusCode: StatusCodes.Status500InternalServerError,
                             type: "https://httpstatuses.com/500"
                         );
                     }
                 }

                 /* single-session logout by refresh token (no auth required) */
                 if (string.IsNullOrWhiteSpace(body.RefreshToken)) {
                     return Results.Problem(
                         title: "Either set allDevices=true or provide refreshToken.",
                         statusCode: StatusCodes.Status400BadRequest,
                         type: "https://httpstatuses.com/400"
                     );
                 }

                 try {
                     refreshService.RevokeByRawToken(body.RefreshToken);
                     return Results.NoContent();
                 }
                 catch (UnauthorizedAccessException) {
                     return Results.Problem(
                         title: "Invalid or expired refresh token.",
                         statusCode: StatusCodes.Status401Unauthorized,
                         type: "https://httpstatuses.com/401"
                     );
                 }
                 catch (Exception) {
                     return Results.Problem(
                         title: "Unexpected error while revoking token.",
                         statusCode: StatusCodes.Status500InternalServerError,
                         type: "https://httpstatuses.com/500"
                     );
                 }
             })
            .Accepts<LogoutRequest>("application/json")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .WithSummary("Log out")
            .WithDescription("Revokes the current session's refresh token, or all refresh tokens for the authenticated user.");



        /* GET ME — current authenticated user */
        auth.MapGet("/me", async (
            HttpContext ctx,
            AppDbContext db,
            CancellationToken ct) => {
                var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? ctx.User.FindFirstValue("sub");
                if (!Guid.TryParse(sub, out var userId))
                    return Results.Unauthorized();

                // Load profile
                var user = await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new { u.Id, u.Username })
                    .SingleOrDefaultAsync(ct);

                if (user is null) return Results.NotFound();

                // Count how many vehicles user owns
                var vehiclesCount = await db.Vehicles
                    .AsNoTracking()
                    .CountAsync(v => v.OwnerId == userId, ct);

                return Results.Ok(new UserMeResponse(
                    user.Id,
                    user.Username,
                    vehiclesCount
                    ));
            })
            .RequireAuthorization()
            .Produces<UserMeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Current user")
            .WithDescription("Returns the authenticated user's id, username, and a vehicles count.");

        return routes;
           
    }
}
