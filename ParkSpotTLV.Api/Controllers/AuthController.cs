using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Core.Auth;
using ParkSpotTLV.Infrastructure;
using ParkSpotTLV.Infrastructure.Entities;
using ParkSpotTLV.Contracts.Auth;

namespace ParkSpotTLV.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase {
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    private readonly IRefreshTokenService _refresh;

    public AuthController(AppDbContext db, IPasswordHasher hasher, IJwtService jwt, IRefreshTokenService refresh) {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
        _refresh = refresh;
    }

    /* Sends a register request */
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest body, CancellationToken ct) {
        if (string.IsNullOrWhiteSpace(body.Username) || string.IsNullOrWhiteSpace(body.Password))
            return BadRequest(new ProblemDetails { 
                Title = "Username and password are required.", 
                Status = 400 
            });

        /* Username is trimmed and transfered to lowercase BEFORE inputting into the table */
        var normalized = body.Username.Trim().ToLowerInvariant();


        /* Checks to see if username already exists in DB */
        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Username == normalized, ct);
        if (exists)
            return Conflict(new ProblemDetails { 
                Title = "Username already taken.", 
                Status = 409 
            });


        /* Creates new user and adds to DB */
        var hash = _hasher.Hash(body.Password);
        var user = new User {
            Id = Guid.NewGuid(),
            Username = normalized,
            PasswordHash = hash
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        /* Creates a new access token */
        var access = _jwt.IssueAccessToken(user.Id, user.Username);
        var refresh = _refresh.Issue(user.Id);

        /* Returns response to front */
        var response = new {
            AccessToken = access.AccessToken,
            AccessTokenExpiresAt = access.ExpiresAtUtc,
            RefreshToken = refresh.RefreshToken,
            RefreshTokenExpiresAt = refresh.ExpiresAtUtc,
            TokenType = "Bearer"
        };

        return StatusCode(201, response);
    }
}
