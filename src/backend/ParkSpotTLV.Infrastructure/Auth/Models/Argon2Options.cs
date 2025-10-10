

namespace ParkSpotTLV.Infrastructure.Auth.Models{
    /* Argon2Options
     * Tuning for the Argon2id password hasher. The app calibrates on boot to target ~100–250ms.
     */

    public sealed class Argon2Options {
        public int TargetHashMs { get; set; } = 180;
        public int MinIterations { get; set; } = 2;
        public int MaxIterations { get; set; } = 8;
        public int MemoryMiB { get; set; } = 64;
        public int Parallelism { get; set; } = 2;

    }
}
