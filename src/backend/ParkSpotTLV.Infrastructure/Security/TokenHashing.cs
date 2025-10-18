using System.Security.Cryptography;
using System.Text;


/*
 * Helpers for refresh-token generation and hashing.
*/
namespace ParkSpotTLV.Infrastructure.Security {
    public static class TokenHashing {

        /* 
         * HMAC-SHA256 with secret
         */
        public static string HashToHex(string raw, string secret) {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /* 
         * Plain SHA256 
         */
        public static string Sha256Hex(string raw) {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /* 
         * Generates a cryptographically random token 32 bytes long  
         */
        public static string GenerateBase64UrlToken(int numBytes) {
            Span<byte> buf = stackalloc byte[numBytes];
            RandomNumberGenerator.Fill(buf);
            var b64 = Convert.ToBase64String(buf);
            return b64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}