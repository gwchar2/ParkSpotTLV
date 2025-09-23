using Microsoft.EntityFrameworkCore;
using ParkSpotTLV.Infrastructure.Entities;
using ParkSpotTLV.Core.Auth;

namespace ParkSpotTLV.Infrastructure.Security {

    public class EfRefreshTokenStore {

        private readonly AppDbContext _db;
        private readonly TimeProvider _time;
        public sealed record RefreshTokenValidationResult(
            RefreshTokenStatus Status,
            RefreshToken? Token
        );

        public EfRefreshTokenStore(AppDbContext db, TimeProvider time) {
            _db = db;
            _time = time;
        }

        /* Creates a new refresh token */
        public async Task<(string rawToken, RefreshToken record)> CreateAsync(Guid userId, TimeSpan ttl, CancellationToken ct = default) {
            var now = _time.GetUtcNow().UtcDateTime;
            var raw = TokenHashing.GenerateBase64UrlToken(32);
            var hash = TokenHashing.Sha256Hex(raw);

            var rec = new RefreshToken {
                UserId = userId,
                TokenHash = hash,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.Add(ttl),
            };

            _db.RefreshTokens.Add(rec);
            await _db.SaveChangesAsync(ct);
            return (raw, rec);
            
        }

        /* Validates the status of the refresh token (Checks to see if it is expired / renued / active) */
        public async Task<RefreshTokenValidationResult> ValidateAsync(string presentedRawToken, CancellationToken ct = default) {

            var hash = TokenHashing.Sha256Hex(presentedRawToken);
            var rec = await _db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(x => x.TokenHash == hash, ct);

            if (rec is null) return new(RefreshTokenStatus.NotFound, null);

            var now = _time.GetUtcNow().UtcDateTime;

            if (rec.RevokedAtUtc is not null) {
                return rec.ReplacedByTokenHash is not null
                    ? new(RefreshTokenStatus.Reused, rec)
                    : new(RefreshTokenStatus.Revoked, rec);
            }

            if (rec.ExpiresAtUtc <= now) return new(RefreshTokenStatus.Expired, rec);

            return new(RefreshTokenStatus.Active, rec);
        }

        /* Renews an old (not-yet-expired) token */
        public async Task<(string rawToken, RefreshToken newRecord)> RotateAsync(string PresentedRawToken, TimeSpan ttl, CancellationToken ct = default) {
            var now = _time.GetUtcNow().UtcDateTime;
            var oldHash = TokenHashing.Sha256Hex(PresentedRawToken);

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var old = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == oldHash, ct);
            if (old is null) throw new InvalidOperationException("Refresh token not found");
            if (old.RevokedAtUtc is not null) throw new InvalidOperationException("Refresh token already revoked");
            if (old.ExpiresAtUtc <= now) throw new InvalidOperationException("Refresh token expired");

            var newRaw = TokenHashing.GenerateBase64UrlToken(32);
            var newHash = TokenHashing.Sha256Hex(newRaw);

            var @new = new RefreshToken {
                UserId = old.UserId,
                TokenHash = newHash,
                CreatedAtUtc = now,
                ExpiresAtUtc = now.Add(ttl),
            };

            _db.RefreshTokens.Add(@new);

            old.RevokedAtUtc = now;
            old.ReplacedByTokenHash = newHash;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return (newRaw, @new);

        }

        /* Attempts to revoke (cancel) a refresh token */
        public async Task<bool> TryRevokeAsync(string presentedRawToken, CancellationToken ct = default) {
            var hash = TokenHashing.Sha256Hex(presentedRawToken);
            var rec = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);

            if (rec is null) return false;
            if (rec.RevokedAtUtc is not null) return true;

            rec.RevokedAtUtc = _time.GetUtcNow().UtcDateTime;
            await _db.SaveChangesAsync(ct);


            return true;
        }

        /* Revokes (cancels) the entire chain of refresh tokens (When does this happen? => Malicious attempt with an unchained token) */
        public async Task<int> RevokeChainAsync (string startingRawToken, CancellationToken ct = default) {
            var count = 0;
            var hash = TokenHashing.Sha256Hex(startingRawToken);
            var now = _time.GetUtcNow().UtcDateTime;

            while (true) {
                var rec = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);

                if (rec is null) break;

                if (rec.RevokedAtUtc is null) {
                    rec.RevokedAtUtc = now;
                    count++;
                    await _db.SaveChangesAsync(ct);
                }

                if (string.IsNullOrEmpty(rec.ReplacedByTokenHash)) break;

                hash = rec.ReplacedByTokenHash;

            }

            return count;
        }

    }
}
