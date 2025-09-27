/*
using Xunit;

namespace ParkSpotTLV.Tests;

public class ValidationTests
{
    private readonly AuthenticationService _authService;

    public ValidationTests()
    {
        _authService = AuthenticationService.Instance;
        _authService.Reset();
    }

    public class PasswordValidationTests : ValidationTests
    {
        [Theory]
        [InlineData("123456", true)]
        [InlineData("password", true)]
        [InlineData("P@ssw0rd123!", true)]
        [InlineData("short", false)]
        [InlineData("12345", false)]
        [InlineData("", false)]
        [InlineData("     ", false)]
        [InlineData("pass word", false)] // space
        [InlineData("pass\tword", false)] // tab
        [InlineData("pass\nword", false)] // newline
        public void ValidatePassword_ReturnsCorrectResult(string password, bool expected)
        {
            var result = _authService.ValidatePassword(password);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ValidatePassword_NullPassword_ReturnsFalse()
        {
            var result = _authService.ValidatePassword(null);
            Assert.False(result);
        }

        [Fact]
        public void ValidatePassword_ExactlyMinLength_ReturnsTrue()
        {
            var result = _authService.ValidatePassword("123456"); // exactly 6 chars
            Assert.True(result);
        }

        [Fact]
        public void ValidatePassword_LongPassword_ReturnsTrue()
        {
            var longPassword = new string('a', 100);
            var result = _authService.ValidatePassword(longPassword);
            Assert.True(result);
        }
    }

    public class UsernameValidationTests : ValidationTests
    {
        [Theory]
        [InlineData("abc", true)]
        [InlineData("user123", true)]
        [InlineData("test_user", true)]
        [InlineData("User_123_Test", true)]
        [InlineData("ab", false)]
        [InlineData("a", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        public void ValidateUsername_BasicCases_ReturnsCorrectResult(string username, bool expected)
        {
            var result = _authService.ValidateUsername(username);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("user@domain.com", false)]
        [InlineData("user-name", false)]
        [InlineData("user name", false)]
        [InlineData("user.name", false)]
        [InlineData("user#name", false)]
        [InlineData("user$name", false)]
        [InlineData("user%name", false)]
        public void ValidateUsername_InvalidCharacters_ReturnsFalse(string username, bool expected)
        {
            var result = _authService.ValidateUsername(username);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ValidateUsername_NullUsername_ReturnsFalse()
        {
            var result = _authService.ValidateUsername(null);
            Assert.False(result);
        }

        [Fact]
        public void ValidateUsername_ExactlyMinLength_ReturnsTrue()
        {
            var result = _authService.ValidateUsername("abc"); // exactly 3 chars
            Assert.True(result);
        }

        [Fact]
        public void ValidateUsername_OnlyNumbers_ReturnsTrue()
        {
            var result = _authService.ValidateUsername("123456");
            Assert.True(result);
        }

        [Fact]
        public void ValidateUsername_OnlyUnderscores_ReturnsFalse()
        {
            var result = _authService.ValidateUsername("___");
            Assert.False(result); // All underscores, no alphanumeric
        }

        [Fact]
        public void ValidateUsername_MixedCase_ReturnsTrue()
        {
            var result = _authService.ValidateUsername("UserName123");
            Assert.True(result);
        }
    }

    public class EdgeCaseTests : ValidationTests
    {
        [Fact]
        public void ValidatePassword_OnlySpaces_ReturnsFalse()
        {
            var result = _authService.ValidatePassword("      "); // 6 spaces
            Assert.False(result);
        }

        [Fact]
        public void ValidateUsername_OnlySpaces_ReturnsFalse()
        {
            var result = _authService.ValidateUsername("   "); // 3 spaces
            Assert.False(result);
        }

        [Fact]
        public void ValidatePassword_Unicode_ReturnsTrue()
        {
            var result = _authService.ValidatePassword("pásswörd"); // 9 chars with unicode
            Assert.True(result);
        }

        [Fact]
        public void ValidateUsername_Unicode_ReturnsFalse()
        {
            var result = _authService.ValidateUsername("usér"); // contains unicode
            Assert.False(result);
        }

        [Theory]
        [InlineData("\t\t\t\t\t\t", false)] // tabs
        [InlineData("\n\n\n\n\n\n", false)] // newlines
        public void ValidatePassword_WhitespaceCharacters_ReturnsFalse(string password, bool expected)
        {
            var result = _authService.ValidatePassword(password);
            Assert.Equal(expected, result);
        }
    }
}*/