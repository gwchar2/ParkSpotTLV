using System.Text.Json;
using System.Net.Http.Json;

namespace ParkSpotTLV.App.Services;

public class AuthenticationService
{
    private static AuthenticationService? _instance;
    public static AuthenticationService Instance => _instance ??= new AuthenticationService();

    public bool IsAuthenticated { get; private set; }
    public string? CurrentUsername { get; private set; }

    private readonly HttpClient _http = new() { BaseAddress = new Uri("http://10.0.2.2:8080/") };

    // Simple in-memory user storage (for demo purposes)
    private readonly Dictionary<string, string> _users = new()
    {
        { "admin", "password" },
        { "test", "test123" },
        { "john_doe", "mypassword" }
    };

    private readonly JsonSerializerOptions _options = new() {
        PropertyNameCaseInsensitive = true
    };

    private AuthenticationService() { }
    
    public sealed class AuthResponse
    {
    public string AccessToken { get; set; } = "";
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTime RefreshTokenExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    }


    public async Task<AuthResponse?> LoginAsync(string username, string password)
    {
        var payload = new { username, password };
        var response = await _http.PostAsJsonAsync("auth/login", payload, _options);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Login failed: {response.StatusCode} {error}");
        }

        var tokens = await response.Content.ReadFromJsonAsync<AuthResponse>(_options);

        // store the token for later requests
        if (tokens != null)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(tokens.TokenType, tokens.AccessToken);
        }

        return tokens;
    }

    public async Task<AuthResponse?> SignUpAsync(string username, string password)
    {
    var payload = new { username, password };
    var response = await _http.PostAsJsonAsync("auth/register", payload, _options);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException($"Sign-up failed: {response.StatusCode} {error}");
    }

    var tokens = await response.Content.ReadFromJsonAsync<AuthResponse>(_options);

    // store the token
    if (tokens != null)
    {
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(tokens.TokenType, tokens.AccessToken);
    }

    return tokens;
    }

    public void Logout()
    {
        IsAuthenticated = false;
        CurrentUsername = null;
    }

    public bool ValidatePassword(string password)
    {
        // Password validation: at least 6 characters, no whitespace characters
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (password.Length < 6)
            return false;

        // Check for whitespace characters (space, tab, newline, etc.)
        if (password.Any(char.IsWhiteSpace))
            return false;

        return true;
    }
    // get username - check - not empty , mlonger than 3 , only ASCII , alphanumeric
    public bool ValidateUsername(string username)
    {
        // Username validation: at least 3 characters, alphanumeric + underscore only, must contain at least one alphanumeric
        if (string.IsNullOrWhiteSpace(username))
            return false;

        if (username.Length < 3)
            return false;

        // Must contain only ASCII letters, digits, and underscores
        if (!username.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
            return false;

        // Must contain at least one alphanumeric character (not all underscores)
        if (!username.Any(char.IsAsciiLetterOrDigit))
            return false;

        return true;
    }

    public async Task<bool> UpdateUsernameAsync(string newUsername)
    {
        // Simulate network delay
        await Task.Delay(300);

        if (!IsAuthenticated || CurrentUsername == null)
            return false;

        // Validate new username
        if (!ValidateUsername(newUsername))
            return false;

        // Check if new username already exists (and it's not the current user)
        if (_users.ContainsKey(newUsername) && newUsername != CurrentUsername)
            return false;

        // Update username
        var currentPassword = _users[CurrentUsername];
        _users.Remove(CurrentUsername);
        _users[newUsername] = currentPassword;
        CurrentUsername = newUsername;

        return true;
    }

    public async Task<bool> UpdatePasswordAsync(string newPassword)
    {
        // Simulate network delay
        await Task.Delay(300);

        if (!IsAuthenticated || CurrentUsername == null)
            return false;

        // Validate new password
        if (!ValidatePassword(newPassword))
            return false;

        // Update password
        _users[CurrentUsername] = newPassword;

        return true;
    }

    // Test helper methods
    public void Reset()
    {
        IsAuthenticated = false;
        CurrentUsername = null;
        _users.Clear();
        _users["admin"] = "password";
        _users["test"] = "test123";
        _users["john_doe"] = "mypassword";
    }

    public bool UserExists(string username)
    {
        return _users.ContainsKey(username);
    }

    public int GetUserCount()
    {
        return _users.Count;
    }
}
