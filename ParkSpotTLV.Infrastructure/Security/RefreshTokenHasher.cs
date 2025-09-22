using System.Text;
using System.Security.Cryptography;

namespace ParkSpotTLV.Infrastructure.Security {

    /* This class handles the encryption of tokens. 
     * SHA256Hex -> Encrypts using SHA256 
     * NewRawToken... -> Returns a new raw token 32 bytes long in base 64 
    */

    public static class RefreshTokenHasher {

        /* Returns a 64-char lowercase hex sha-256 */
        public static string Sha256Hex (string rawToken) {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        /* 32 random bytes, Base64URL encoded. Removes +/-= */
        public static string NewRawToken32BytesBase64url() {
            Span<byte> buf = stackalloc byte[32];
            RandomNumberGenerator.Fill(buf);
            var b64 = Convert.ToBase64String(buf);
            return b64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}
