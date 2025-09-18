using ParkSpotTLV.Core.Services;
using Xunit;

namespace ParkSpotTLV.Tests;

public class AuthenticationServiceTests
{
    private AuthenticationService GetFreshAuthService()
    {
        var authService = AuthenticationService.Instance;
        authService.Reset(); // Reset to clean state for each test
        return authService;
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var authService = GetFreshAuthService();

        // Act
        var result = await authService.LoginAsync("admin", "password");

        // Assert
        Assert.True(result);
        Assert.True(authService.IsAuthenticated);
        Assert.Equal("admin", authService.CurrentUsername);
    }

    [Fact]
    public async Task LoginAsync_InvalidUsername_ReturnsFalse()
    {
        // Arrange
        var authService = GetFreshAuthService();

        // Act
        var result = await authService.LoginAsync("nonexistent", "password");

        // Assert
        Assert.False(result);
        Assert.False(authService.IsAuthenticated);
        Assert.Null(authService.CurrentUsername);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        var authService = GetFreshAuthService();

        // Act
        var result = await authService.LoginAsync("admin", "wrongpassword");

        // Assert
        Assert.False(result);
        Assert.False(authService.IsAuthenticated);
        Assert.Null(authService.CurrentUsername);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("admin", "")]
    [InlineData("", "")]
    [InlineData(null, "password")]
    [InlineData("admin", null)]
    [InlineData("   ", "password")]
    [InlineData("admin", "   ")]
    public async Task LoginAsync_EmptyOrNullCredentials_ReturnsFalse(string username, string password)
    {
        // Arrange
        var authService = GetFreshAuthService();

        // Act
        var result = await authService.LoginAsync(username, password);

        // Assert
        Assert.False(result);
        Assert.False(authService.IsAuthenticated);
        Assert.Null(authService.CurrentUsername);
    }

    [Fact]
    public async Task SignUpAsync_NewUser_ReturnsTrue()
    {
        // Arrange
        var authService = GetFreshAuthService();
        var initialUserCount = authService.GetUserCount();

        // Act
        var result = await authService.SignUpAsync("newuser", "newpassword");

        // Assert
        Assert.True(result);
        Assert.True(authService.IsAuthenticated);
        Assert.Equal("newuser", authService.CurrentUsername);
        Assert.True(authService.UserExists("newuser"));
        Assert.Equal(initialUserCount + 1, authService.GetUserCount());
    }

    [Fact]
    public async Task SignUpAsync_ExistingUser_ReturnsFalse()
    {
        // Arrange
        var authService = GetFreshAuthService();

        // Act
        var result = await authService.SignUpAsync("admin", "newpassword");

        // Assert
        Assert.False(result);
        Assert.False(authService.IsAuthenticated);
        Assert.Null(authService.CurrentUsername);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("username", "")]
    [InlineData("", "")]
    [InlineData(null, "password")]
    [InlineData("username", null)]
    [InlineData("   ", "password")]
    [InlineData("username", "   ")]
    public async Task SignUpAsync_EmptyOrNullCredentials_ReturnsFalse(string username, string password)
    {
        // Arrange
        var authService = GetFreshAuthService();

        // Act
        var result = await authService.SignUpAsync(username, password);

        // Assert
        Assert.False(result);
        Assert.False(authService.IsAuthenticated);
        Assert.Null(authService.CurrentUsername);
    }

    [Fact]
    public void Logout_WhenAuthenticated_ClearsSession()
    {
        // Arrange
        var authService = GetFreshAuthService();
        authService.LoginAsync("admin", "password").Wait();
        Assert.True(authService.IsAuthenticated);

        // Act
        authService.Logout();

        // Assert
        Assert.False(authService.IsAuthenticated);
        Assert.Null(authService.CurrentUsername);
    }

    [Fact]
    public void Logout_WhenNotAuthenticated_DoesNotThrow()
    {
        // Arrange
        var authService = GetFreshAuthService();
        Assert.False(authService.IsAuthenticated);

        // Act & Assert (should not throw)
        authService.Logout();
        Assert.False(authService.IsAuthenticated);
        Assert.Null(authService.CurrentUsername);
    }

    [Theory]
    [InlineData("password123", true)]
    [InlineData("123456", true)]
    [InlineData("abcdef", true)]
    [InlineData("p@ssw0rd!", true)]
    [InlineData("12345", false)]  // too short
    [InlineData("abc", false)]    // too short
    [InlineData("", false)]       // empty
    [InlineData(null, false)]     // null
    [InlineData("   ", false)]    // whitespace
    public void ValidatePassword_VariousPasswords_ReturnsExpectedResult(string password, bool expected)
    {
        // Arrange
        var authService = GetFreshAuthService();

        // Act
        var result = authService.ValidatePassword(password);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("user123", true)]
    [InlineData("test_user", true)]
    [InlineData("admin", true)]
    [InlineData("User_123", true)]
    [InlineData("ab", false)]      // too short
    [InlineData("a", false)]       // too short
    [InlineData("", false)]        // empty
    [InlineData(null, false)]      // null
    [InlineData("   ", false)]     // whitespace
    [InlineData("user@domain", false)]  // invalid character
    [InlineData("user-name", false)]    // invalid character
    [InlineData("user name", false)]    // space
    [InlineData("user.name", false)]    // dot
    public void ValidateUsername_VariousUsernames_ReturnsExpectedResult(string username, bool expected)
    {
        // Arrange
        var authService = GetFreshAuthService();

        // Act
        var result = authService.ValidateUsername(username);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task MultipleLogins_LastLoginWins()
    {
        // Arrange
        var authService = GetFreshAuthService();

        // Act
        await authService.LoginAsync("admin", "password");
        Assert.Equal("admin", authService.CurrentUsername);

        await authService.LoginAsync("test", "test123");

        // Assert
        Assert.True(authService.IsAuthenticated);
        Assert.Equal("test", authService.CurrentUsername);
    }

    [Fact]
    public async Task LoginAsync_ExecutionTime_IsWithinExpectedRange()
    {
        // Arrange
        var authService = GetFreshAuthService();
        var startTime = DateTime.UtcNow;

        // Act
        await authService.LoginAsync("admin", "password");

        // Assert
        var executionTime = DateTime.UtcNow - startTime;
        Assert.True(executionTime.TotalMilliseconds >= 400); // At least close to simulated delay
        Assert.True(executionTime.TotalMilliseconds < 1000); // But not too long
    }
}