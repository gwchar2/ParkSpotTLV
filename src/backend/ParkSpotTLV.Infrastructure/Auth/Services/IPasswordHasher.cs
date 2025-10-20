

namespace ParkSpotTLV.Infrastructure.Auth.Services {
    
    /* 
     * Contract for hashing and verifying user passwords.
     * - Hash(password, salt) -> stores hash+parameters in DB
     * - Verify(password, storedHash) -> constant-time success/failure
     */
    public interface IPasswordHasher {

        /* Creates Argon2id hash */
        string Hash(string password);

        /* Verifies a password against a hashed string
         * Returns (isValid, needsRehash)
         */
        (bool isValid, bool needsRehash) Verify (string password, string storedPhc);

    }
}
