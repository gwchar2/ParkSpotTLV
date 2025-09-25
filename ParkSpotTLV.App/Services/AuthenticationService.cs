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
        // Simple password validation
        return !string.IsNullOrWhiteSpace(password) && password.Length >= 6;
    }

    public bool ValidateUsername(string username)
    {
        // Simple username validation
        return !string.IsNullOrWhiteSpace(username) &&
               username.Length >= 3 &&
               username.All(c => char.IsLetterOrDigit(c) || c == '_');
    }
}
