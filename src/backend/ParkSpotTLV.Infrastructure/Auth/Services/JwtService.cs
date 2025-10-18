using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ParkSpotTLV.Infrastructure.Auth.Models;

namespace ParkSpotTLV.Infrastructure.Auth.Services {
    
    /* 
     * Builds short-lived access tokens (JWT) signed with HMAC-SHA256.
     */
    public sealed class JwtService : IJwtService {
        private readonly AuthOptions _opts;
        private readonly ILogger<JwtService> _logger;
        private readonly SigningCredentials _creds;
        private readonly JwtSecurityTokenHandler _handler = new();

        public JwtService(IOptions<AuthOptions> opts, ILogger<JwtService> logger) {
            _opts = opts.Value;
            _logger = logger;

            // Validate secret
            var secret = _opts.Signing?.HmacSecret;
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
                throw new InvalidOperationException("Auth:Signing:HmacSecret is missing or too short (>= 32 chars required).");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            _creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        public JwtIssueResult IssueAccessToken(Guid userId, string username) {
            var now = DateTimeOffset.UtcNow;
            var expires = now.AddMinutes(_opts.AccessTokenMinutes);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("name", username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: _opts.Issuer,
                audience: _opts.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expires.UtcDateTime,
                signingCredentials: _creds
            );

            string jwt = _handler.WriteToken(token);
            _logger.LogDebug("Issued JWT for {UserId}", userId);

            return new JwtIssueResult(jwt, expires);
        }
    }
}
