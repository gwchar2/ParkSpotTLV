using System.Security.Cryptography;
using System.Text;


/*
 * Helpers for refresh-token generation and hashing.
*/
public static class TokenHashing {
    /* TokenHashing
     * Utilities for refresh tokens:
     *  - GenerateBase64UrlToken(numBytes) → generate cryptographically random token numBytes long (expected 32) (client stores raw)
     *  - HashToHex(raw, secret) → HMAC-SHA256(raw, secret) and store *hex* hash server-side
     *    (Preferred over plain SHA-256 so a DB leak is less useful without the server secret.)
     */


    /* HMAC-SHA256 with secret → lowercase hex */
    public static string HashToHex(string raw, string secret) {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /* Plain SHA256 → lowercase hex */
    public static string Sha256Hex(string raw) {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /* Random Base64Url token */
    public static string GenerateBase64UrlToken(int numBytes) {
        Span<byte> buf = stackalloc byte[numBytes];
        RandomNumberGenerator.Fill(buf);
        var b64 = Convert.ToBase64String(buf);
        return b64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
