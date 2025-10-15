using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ParkSpotTLV.Infrastructure.Auth.Models;
using ParkSpotTLV.Infrastructure.Auth.Services;
using ParkSpotTLV.Infrastructure.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace ParkSpotTLV.Api.Composition {

    public static class AuthExtensions {
        public static IServiceCollection AddAuthFeature(this IServiceCollection services, IConfiguration config) {

            /* ----------------------------------------------------------------------
             * AUTH OPTIONS (SINGLE SOURCE OF TRUTH)
             * ---------------------------------------------------------------------- */
            var authOpts = config.GetSection("Auth").Get<AuthOptions>() ?? new AuthOptions();

            if (!string.Equals(authOpts.Signing.Type, "HMAC", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("ONLY HMAC IS WIRED AT THE MOMENT - IMPLEMENT RSA LATER");

            if (string.IsNullOrWhiteSpace(authOpts.Signing.HmacSecret)) {
                var bytes = RandomNumberGenerator.GetBytes(32); 
                authOpts.Signing.HmacSecret = WebEncoders.Base64UrlEncode(bytes);
                Console.WriteLine("Auth: no HMAC provided. Generated dev HMAC; tokens will invalidate on restart.");
            }
            services.AddSingleton<IOptions<AuthOptions>>(Options.Create(authOpts));


            /* ----------------------------------------------------------------------
             * PASSWORD HASHING (Argon2id)
             * ---------------------------------------------------------------------- */

            services.Configure<Argon2Options>(config.GetSection("Auth:Argon2"));
            services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

            /* ----------------------------------------------------------------------
             * AUTHENTICATION (JWT Bearer) + AUTHORIZATION
             * ---------------------------------------------------------------------- */
            //var authOpts = config.GetSection("Auth").Get<AuthOptions>();

            if (authOpts!.Signing.Type.Equals("HMAC", StringComparison.OrdinalIgnoreCase)) {
                var keyBytes = Encoding.UTF8.GetBytes(authOpts.Signing.HmacSecret!);
                var signingKey = new SymmetricSecurityKey(keyBytes);

                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options => {
                            options.RequireHttpsMetadata = false;
                            options.TokenValidationParameters = new TokenValidationParameters {
                                ValidateIssuer = true,
                                ValidIssuer = authOpts.Issuer,
                                ValidateAudience = true,
                                ValidAudience = authOpts.Audience,
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = signingKey,
                                RequireSignedTokens = true,
                                ValidateLifetime = true,
                                ClockSkew = TimeSpan.FromMinutes(authOpts.ClockSkewMinutes),
                                NameClaimType = JwtRegisteredClaimNames.Sub  // "sub" claim inside the JWT is the string representation of the user’s Guid. This maps the JWT "sub" claim onto ClaimTypes.NameIdentifier
                            };
                        });
            } else {
                throw new NotSupportedException("ONLY HMAC IS WIRED AT THE MOMENT - IMPLEMENT RSA LATER");
            }

            services.AddAuthorization();
            /* ----------------------------------------------------------------------
             * TOKEN SERVICES (JWT + Refresh)
             * ---------------------------------------------------------------------- */
            services.AddScoped<EfRefreshTokenStore>();
            services.AddSingleton<IJwtService, JwtService>();
            services.AddScoped<IRefreshTokenService, RefreshTokenService>();



            return services;
        }
    }
}