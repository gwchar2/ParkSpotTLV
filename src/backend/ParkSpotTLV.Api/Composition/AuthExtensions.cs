using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ParkSpotTLV.Infrastructure.Auth.Services;
using ParkSpotTLV.Infrastructure.Auth.Models;
using ParkSpotTLV.Infrastructure.Security;

namespace ParkSpotTLV.Api.Composition {

    public static class AuthExtensions {
        public static IServiceCollection AddAuthFeature(this IServiceCollection services, IConfiguration config) {

            /* ----------------------------------------------------------------------
             * AUTH OPTIONS (SINGLE SOURCE OF TRUTH)
             * ---------------------------------------------------------------------- */
            services.AddOptions<AuthOptions>()
                .Bind(config.GetSection("Auth"))
                .ValidateDataAnnotations()
                .Validate(o => o.Signing.Type != "HMAC" || !string.IsNullOrWhiteSpace(o.Signing.HmacSecret),
                    "HMAC Selected but Auth:Signing:HmacSecret is missing!");

            /* ----------------------------------------------------------------------
             * PASSWORD HASHING (Argon2id)
             * ---------------------------------------------------------------------- */

            services.Configure<Argon2Options>(config.GetSection("Auth:Argon2"));
            services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();

            /* ----------------------------------------------------------------------
             * AUTHENTICATION (JWT Bearer) + AUTHORIZATION
             * ---------------------------------------------------------------------- */
            var authOpts = config.GetSection("Auth").Get<AuthOptions>();

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