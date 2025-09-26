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

    public async Task<HttpResponseMessage> LoginAsync(string username, string password)
    {
        try
        {
            var payload = new { username = username, password = password };

            System.Diagnostics.Debug.WriteLine($"Attempting login for user: {username}");
            var response = await _http.PostAsJsonAsync("auth/login", payload, _options);
            System.Diagnostics.Debug.WriteLine($"Login response: {response.StatusCode}");

            return response;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> SignUpAsync(string username, string password)
    {
        // Simulate network delay
        await Task.Delay(500);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        // Check if user already exists
        if (_users.ContainsKey(username))
            return false;

        // Add new user
        _users[username] = password;
        IsAuthenticated = true;
        CurrentUsername = username;
        return true;
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
