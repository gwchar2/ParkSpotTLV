using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Contracts.Auth;
using ParkSpotTLV.Core.Auth;
using ParkSpotTLV.Contracts.Enums;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ParkSpotTLV.Api.Endpoints.Support;

namespace ParkSpotTLV.Api.Endpoints {
    public static class AuthEndpoints {
        public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder routes) {

            var auth = routes.MapGroup("/auth").WithTags("Auth");

            /* REGISTER REQUEST 
             * Accepts: username, password
             * Returns: 
             *      201 with { accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt, tokenType: "Bearer" } (or 201 without tokens if you prefer).
             *      400 - Missing information
             *      409 if username is taken; 400 if policy fails.
             */
            auth.MapPost("/register",
                async ([FromBody] RegisterRequest body,HttpContext ctx, AppDbContext db,IPasswordHasher hasher,IJwtService jwt,IRefreshTokenService refresh,CancellationToken ct) => {

                    if (string.IsNullOrWhiteSpace(body.Username) || string.IsNullOrWhiteSpace(body.Password))
                        return AuthProblems.MissingInfo(ctx);
                    
                    var normalized = body.Username.Trim().ToLowerInvariant();
                    
                    var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Username == normalized, ct);
                    
                    if (exists) return AuthProblems.UsernameTaken(ctx);

                    var pwdHash = hasher.Hash(body.Password);
                    
                    var user = new User {
                        Id = Guid.NewGuid(),
                        Username = normalized,
                        PasswordHash = pwdHash
                    };
                    
                    db.Users.Add(user);
                    await db.SaveChangesAsync(ct);
                    
                    var access = jwt.IssueAccessToken(user.Id, user.Username);
                    
                    var issued = refresh.Issue(user.Id);                                 // Issue function already inserts to table!!
                    
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
             *      400 - Missing information
             *      401 for bad creds; 400 if malformed.
             */
            auth.MapPost("/login",
                async ([FromBody] RegisterRequest body, HttpContext ctx, AppDbContext db,IPasswordHasher hasher,IJwtService jwt,IRefreshTokenService refresh,CancellationToken ct) => {
                           
                    if (string.IsNullOrWhiteSpace(body.Username) || string.IsNullOrWhiteSpace(body.Password))
                        return AuthProblems.MissingInfo(ctx);

                    var normalized = body.Username.Trim().ToLowerInvariant();
                    
                    var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(us => us.Username == normalized, ct);
                    if (user is null)
                        return AuthProblems.InvalidCreds(ctx);

                    var (isValid, needsRehash) = hasher.Verify(body.Password, user.PasswordHash);
                    
                    if (!isValid) return AuthProblems.InvalidCreds(ctx);

                    await db.SaveChangesAsync(ct);
                    
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
                .WithDescription("Verifies credentials and returns a short-lived access token with a new refresh token.");

            /* REFRESH REQUEST
             * Accepts: RefreshToken in body
             * Returns: 
             *      200 with new pair (on valid, first use).
             *      400 - Missing information
             *      401 for invalid/expired/revoked/reused tokens; 400 if malformed.
             */
            auth.MapPost("/refresh",
                ([FromBody] RefreshRequest body,HttpContext ctx, AppDbContext db, IRefreshTokenService refreshService, CancellationToken ct) => {

                    if (body is null || string.IsNullOrWhiteSpace(body.RefreshToken))
                        return GeneralProblems.MissingRefresh(ctx);

                    try {
                        var result = refreshService.ValidateAndRotate(body.RefreshToken);

                        // We need to grab the permits from the user according to the refresh token
                        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
                        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == ClaimTypes.NameIdentifier)?.Value;

                        var response = new TokenPairResponse(
                            AccessToken: result.AccessToken,
                            AccessTokenExpiresAt: result.AccessExpiresAtUtc,
                            RefreshToken: result.RefreshToken,
                            RefreshTokenExpiresAt: result.RefreshExpiresAtUtc,
                            TokenType: "Bearer");

                        return Results.Ok(response);
                    }
                    catch (UnauthorizedAccessException) {
                        return GeneralProblems.ExpiredToken(ctx);
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
                ([FromBody] LogoutRequest body,HttpContext ctx,IRefreshTokenService refreshService) => {

                    if (body is null) return GeneralProblems.MissingBody(ctx);
                    
                    /* To remove ALL devices, we require a valid JWT bearer */
                    if (body.AllDevices) {
                        var subscriptions = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                            ?? ctx.User.FindFirstValue("subscriptions");

                        if (string.IsNullOrWhiteSpace(subscriptions) || !Guid.TryParse(subscriptions, out var userId))
                            return AuthProblems.AuthentRequired(ctx);

                        try {
                            refreshService.RevokeAllForUser(userId);
                            return Results.NoContent();
                        }
                        catch (Exception) {
                            return GeneralProblems.Unexpected(ctx);
                        }
                    }
                    /* single-session logout by refresh token (no auth required) */
                    if (string.IsNullOrWhiteSpace(body.RefreshToken)) return AuthProblems.SingleSession(ctx); 

                    try {
                        refreshService.RevokeByRawToken(body.RefreshToken);
                        return Results.NoContent();
                    }
                    catch (UnauthorizedAccessException) {
                        return GeneralProblems.ExpiredToken(ctx);
                    }
                    catch (Exception) {
                        return GeneralProblems.Unexpected(ctx);
                    }
                })
                .Accepts<LogoutRequest>("application/json")
                .Produces(StatusCodes.Status204NoContent)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
                .WithSummary("Log out")
                .WithDescription("Revokes the current session's refresh token, or all refresh tokens for the authenticated user.");

            /* GET ME — current authenticated user */
            auth.MapGet("/me", 
                async (HttpContext ctx,AppDbContext db,CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    // Load profile
                    var user = await db.Users
                        .AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => new { u.Id, u.Username })
                        .SingleOrDefaultAsync(ct);

                    if (user is null) return GeneralProblems.NotFound(ctx);

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
                .RequireUser()
                .Produces<UserMeResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .WithSummary("Current user")
                .WithDescription("Returns the authenticated user's id, username, and a vehicles count.");

            /* CHANGE PASSWORD REQUEST
             * Accepts: Access token (Bearer) to identify user & old password
             * Returns: 
             *      200 Successfully changed password
             *      400 Invalid fields
             *      401 If the caller has no valid access token (or user disabled).
             */
            auth.MapPost("/change-password",
                async ([FromBody] UpdatePasswordRequest body,HttpContext ctx,AppDbContext db, IPasswordHasher hasher, CancellationToken ct) => {

                    var userId = ctx.GetUserId();

                    if (string.IsNullOrWhiteSpace(body.OldPassword) || string.IsNullOrWhiteSpace(body.NewPassword))
                        return AuthProblems.OldInfo(ctx);

                    var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
                    
                    if (user is null) return Results.Unauthorized();
                    
                    var (isValid, needsRehash) = hasher.Verify(body.OldPassword, user.PasswordHash);

                    if (!isValid) return AuthProblems.InvalidOldPass(ctx);


                    user.PasswordHash = hasher.Hash(body.NewPassword);
                    
                    await db.SaveChangesAsync(ct);
                    return Results.Ok(new {
                        message = "Password updated successfully."
                    });
                })
                .RequireAuthorization()
                .RequireUser()
                .Produces<string>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .WithSummary("Change Password")
                .WithDescription("Changes password for current user");

            return routes;
        }
           
    }
}
