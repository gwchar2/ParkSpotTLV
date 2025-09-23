using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ParkSpotTLV.Core.Auth;
using ParkSpotTLV.Infrastructure.Security;
using Xunit;

public class Argon2PasswordHasherTests {
    private static IPasswordHasher Make() =>
        new Argon2PasswordHasher(
            Options.Create(new Argon2Options { TargetHashMs = 120, MinIterations = 2, MaxIterations = 4, MemoryMiB = 64, Parallelism = 2 }),
            NullLogger<Argon2PasswordHasher>.Instance);

    [Fact]
    public void HashVerify_Ok() {
        var h = Make();
        var phc = h.Hash("CorrectHorseBatteryStaple!");
        var (ok, _) = h.Verify("CorrectHorseBatteryStaple!", phc);
        Assert.True(ok);
    }

    [Fact]
    public void WrongPassword_Fails() {
        var h = Make();
        var phc = h.Hash("secret-1");
        var (ok, _) = h.Verify("secret-2", phc);
        Assert.False(ok);
    }
}
