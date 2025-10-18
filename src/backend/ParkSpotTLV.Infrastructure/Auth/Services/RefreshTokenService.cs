using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParkSpotTLV.Infrastructure.Entities;
using ParkSpotTLV.Infrastructure.Security;
using ParkSpotTLV.Infrastructure.Auth.Models;
using ParkSpotTLV.Contracts.Time;

namespace ParkSpotTLV.Infrastructure.Auth.Services {
    
    /* 
     * RefreshTokenService
     *   Refresh token lifecycle:
     *   Issue(userId) -> new token (raw to client, hash to DB)
     *   ValidateAndRotate(raw) -> atomically revoke old, create new, issue new JWT
     *   RevokeByRawToken(raw) -> single-session logout
     *   RevokeAllForUser(userId) -> global logout for the user
     *
     *  Reuse detection:
     *   If a rotated token is seen again (row.ReplacedByTokenHash != null), revoke all.
     */

    public sealed class RefreshTokenService : IRefreshTokenService {
        
        private readonly AppDbContext _db;                  // Database context (EF Core)
        private readonly IJwtService _jwt;              // Service that issues JWT access tokens
        private readonly ILogger<RefreshTokenService> _log;         // Logger for debugging and auditing
        private readonly AuthOptions _opts;             // Auth configuration (expiry, signing, etc.)
        private readonly IClock _clock;

        public RefreshTokenService(AppDbContext db, IJwtService jwt, IOptions<AuthOptions> options, ILogger<RefreshTokenService> log, IClock clock) {
            _db = db;
            _jwt = jwt;
            _opts = options.Value;
            _log = log;
            _clock = clock;

            /* Ensure HMAC secret exists and is sufficiently strong */
            if (string.IsNullOrWhiteSpace(_opts.Signing?.HmacSecret) || _opts.Signing?.HmacSecret.Length < 32)
                throw new InvalidOperationException("AuthOptions.HmacSecret are missing or to short");
        }

        /* 
         * Issues a new refresh token for the given user 
         */
        public RefreshIssueResult Issue(Guid userId) {
            var now = _clock.UtcNow;
            var expires = now.AddDays(_opts.RefreshTokenDays);
            var raw = TokenHashing.GenerateBase64UrlToken(32);
            var hash = TokenHashing.HashToHex(raw, _opts.Signing.HmacSecret);

            var row = new RefreshToken {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = hash,
                CreatedAtUtc = now,
                ExpiresAtUtc = expires,
                RevokedAtUtc = null,
                ReplacedByTokenHash = null,
            };

            _db.RefreshTokens.Add(row);
            _db.SaveChanges();

            _log.LogDebug("Issued refresh token for {UserId}", userId);
            return new RefreshIssueResult(raw, expires);
        }

        /* Validates and rotates a refresh token:
         * - Ensures it’s active, not expired, not reused
         * - Revokes if reused
         * - Creates and links a new refresh token
         * - Issues a new JWT access token
         */
        public RefreshRotateResult ValidateAndRotate(string rawRefreshToken) {
            var now = _clock.UtcNow;
            var hash = TokenHashing.HashToHex(rawRefreshToken, _opts.Signing.HmacSecret);
            // Look for matching active token in DB
            var row = _db.RefreshTokens
                .AsTracking()
                .FirstOrDefault(x => x.TokenHash == hash);

            if (row is null || row.ExpiresAtUtc <= now || row.RevokedAtUtc != null) {

                // If row is present, but already rotated, and ReplacedByTokenHash is not null:
                if (row is not null && row.ReplacedByTokenHash != null)
                    RevokeAllForUser(row.UserId);

                throw new UnauthorizedAccessException("Invalid refresh token!");
            }

            // If already rotated, we revoke everything for safety.
            if (row.ReplacedByTokenHash != null) {
                RevokeAllForUser(row.UserId);
                throw new UnauthorizedAccessException("Refresh token reuse detected.");
            }

            // Rotate token (create new + link it + revoke the old token), then issue a JWT
            using var transaction = _db.Database.BeginTransaction();
            var newRaw = TokenHashing.GenerateBase64UrlToken(32);
            var newHash = TokenHashing.HashToHex(newRaw, _opts.Signing.HmacSecret);
            var newExpires = now.AddDays(_opts.RefreshTokenDays);
            var newRow = new RefreshToken {
                Id = Guid.NewGuid(),
                UserId = row.UserId,
                TokenHash = newHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = newExpires,
            };
            // Add the new row (new token) to the DB, and present data for revoking old token
            _db.RefreshTokens.Add(newRow);
            row.RevokedAtUtc = now;
            row.ReplacedByTokenHash = newHash;
            _db.SaveChanges();
            transaction.Commit();

            // We look up the username and make a new access token for it 
            var jwt = _jwt.IssueAccessToken(row.UserId, GetUsername(row.UserId));
            _log.LogDebug("Rotated refresh token for {UserId}", row.UserId);

            return new RefreshRotateResult(jwt.AccessToken, jwt.ExpiresAtUtc,
                                           newRaw, newExpires);

        }

        /* 
         * Revokes a single refresh token by its raw value 
         */
        public void RevokeByRawToken(string rawRefreshToken) {
            var now = _clock.UtcNow;
            var hash = TokenHashing.HashToHex(rawRefreshToken, _opts.Signing.HmacSecret);
            var row = _db.RefreshTokens.AsTracking().FirstOrDefault(x => x.TokenHash == hash);
            
            if (row is null) return;

            if (row.RevokedAtUtc == null) {
                row.RevokedAtUtc = now;
                _db.SaveChanges();
                _log.LogDebug("Revoked refresh token for {UserId}", row.UserId);
            }

        }

        /* 
         * Revokes all active refresh tokens for a given user 
         */
        public void RevokeAllForUser(Guid userId) {
            var now = _clock.UtcNow;
            var active = _db.RefreshTokens.AsTracking()
                .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
                .ToList();

            foreach (var r in active) r.RevokedAtUtc = now;
            if (active.Count > 0) {
                _db.SaveChanges();
                _log.LogWarning("Revoked ALL refresh tokens for {UserId}", userId);
            }
        }

        /* 
         * Fetch username from DB for logging/JWT claims 
         */
        private string GetUsername(Guid userId) {
            var username = _db.Users.Where(u => u.Id == userId).Select(u => u.Username).FirstOrDefault();
            return username ?? string.Empty;
        }

    }
}
