using Konscious.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParkSpotTLV.Core.Auth;
using System.Security.Cryptography;
using System.Text;

namespace ParkSpotTLV.Infrastructure.Auth {
    /* Argon2PasswordHasher
     * Argon2id hashing with per-user salt. Calibrated at boot for ~TargetHashMs.
     * Verify returns in constant-time to avoid timing leaks.
     */
    public sealed class Argon2PasswordHasher : IPasswordHasher {
        private sealed record ParsedPhc(byte[] Salt, byte[] Hash, int Iterations, int MemoryKiB, int Parallelism);
        private readonly ILogger<Argon2PasswordHasher> _log;
        private readonly Argon2Options _opts;

        private readonly int _t;             // Iterations
        private readonly int _mKiB; // memory in KiB
        private readonly int _p;    // Parallelism


        public Argon2PasswordHasher(IOptions<Argon2Options> options, ILogger<Argon2PasswordHasher> log) {
            _opts = options.Value;
            _log = log;

            _mKiB = Math.Max(8, _opts.MemoryMiB) * 1024; // Konscious expects MemorySize in KiB
            _p = Math.Max(1, _opts.Parallelism);
            _t = CalibrateIterations(_opts.TargetHashMs, _opts.MinIterations, _opts.MaxIterations);

            _log.LogInformation("Argon2id parameters: m={MemoryMiB}MiB, t={Iterations}, p={Parallelism}",
                _mKiB / 1024, _t, _p);
        }

        public string Hash(string password) {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password must not be empty.", nameof(password));

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = ComputeHash(password, salt, _t, _mKiB, _p, 32);

            string phc = BuildPhc(salt, hash, _t, _mKiB, _p);
            return phc;
        }

        public (bool isValid, bool needsRehash) Verify(string password, string storedPhc) {
            if (string.IsNullOrWhiteSpace(storedPhc))
                return (false, false);

            if (!TryParsePhc(storedPhc, out var parsed))
                return (false, false);

            byte[] recomputed = ComputeHash(password, parsed.Salt, parsed.Iterations, parsed.MemoryKiB, parsed.Parallelism, parsed.Hash.Length);

            bool ok = CryptographicOperations.FixedTimeEquals(recomputed, parsed.Hash);
            if (!ok) return (false, false);

            bool needsRehash = parsed.Iterations < _t || parsed.MemoryKiB < _mKiB || parsed.Parallelism < _p;
            return (true, needsRehash);
        }

        private static byte[] ComputeHash(string password, byte[] salt, int t, int mKiB, int p, int outLen) {
            var argon = new Argon2id(Encoding.UTF8.GetBytes(password)) {
                Salt = salt,
                Iterations = Math.Max(1, t),
                DegreeOfParallelism = Math.Max(1, p),
                MemorySize = Math.Max(8 * 1024, mKiB) // enforce >= 8 MiB
            };
            return argon.GetBytes(outLen);
        }

        private int CalibrateIterations(int targetMs, int minT, int maxT) {
            string probe = "Calibration-Probe-Only";
            for (int t = Math.Max(1, minT); t <= Math.Max(minT, maxT); t++) {
                var salt = RandomNumberGenerator.GetBytes(16);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                _ = ComputeHash(probe, salt, t, _mKiB, _p, 32);
                sw.Stop();
                _log.LogDebug("Argon2 calibration: t={T} → {Ms}ms", t, sw.ElapsedMilliseconds);
                if (sw.ElapsedMilliseconds >= targetMs) return t;
            }
            return Math.Max(minT, maxT);
        }

        private static string BuildPhc(byte[] salt, byte[] hash, int t, int mKiB, int p) {
            string saltB64 = Convert.ToBase64String(salt);
            string hashB64 = Convert.ToBase64String(hash);
            return $"$argon2id$v=19$m={mKiB},t={t},p={p}${saltB64}${hashB64}";
        }

        private static bool TryParsePhc(string phc, out ParsedPhc parsed) {
            // Expected: $argon2id$v=19$m=<mKiB>,t=<t>,p=<p>$<salt_b64>$<hash_b64>
            parsed = null!;
            try {
                var parts = phc.Split('$', StringSplitOptions.RemoveEmptyEntries);
                // parts[0]=argon2id, parts[1]=v=19, parts[2]=m=..,t=..,p=.., parts[3]=salt, parts[4]=hash
                if (parts.Length != 5 || parts[0] != "argon2id" || !parts[1].StartsWith("v="))
                    return false;

                int mKiB = 0, t = 0, p = 0;
                foreach (var kv in parts[2].Split(',', StringSplitOptions.RemoveEmptyEntries)) {
                    var pair = kv.Split('=', 2);
                    if (pair.Length != 2) return false;
                    switch (pair[0]) {
                        case "m": mKiB = int.Parse(pair[1]); break;
                        case "t": t = int.Parse(pair[1]); break;
                        case "p": p = int.Parse(pair[1]); break;
                    }
                }

                var salt = Convert.FromBase64String(parts[3]);
                var hash = Convert.FromBase64String(parts[4]);

                parsed = new ParsedPhc(salt, hash, t, mKiB, p);
                return true;
            }
            catch {
                return false;
            }
        }
    }
}