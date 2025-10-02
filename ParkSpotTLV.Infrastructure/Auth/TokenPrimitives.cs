

namespace ParkSpotTLV.Infrastructure.Auth {

    /* TokenPrimitives: small models used between services (password hash triple, issued tokens). */

    public sealed record PasswordHashTriple(string Hash, string Salt, string ParamsJson);
    public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAtUtc);
    public sealed record IssuedRefreshToken(string Raw, string TokenHash, DateTimeOffset ExpiresAtUtc);

}
