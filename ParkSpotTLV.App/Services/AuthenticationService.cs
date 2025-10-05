using System.Text.Json;
using System.Net.Http.Json;
using System.Net;
using ParkSpotTLV.App.Data.Services;


namespace ParkSpotTLV.App.Services;

public class AuthenticationService
{

    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _options;

    public bool IsAuthenticated { get; private set; }
    public string? CurrentUsername { get; private set; }
    private string? _refreshToken;
    // private readonly LocalDataService _localDataService;


    

    public AuthenticationService(HttpClient http, JsonSerializerOptions? options = null )
    {
        _http = http;    // same HttpClient instance as CarService
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        // _localDataService = localDataService ;
    }
    public sealed class AuthResponse
    {
    public string AccessToken { get; set; } = "";
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTime RefreshTokenExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    }

    public sealed class MeResponse
    {
    public string Username { get; set; } = "";
    public string Id { get; set; } = "";
    public string[] Roles { get; set; } = Array.Empty<string>();
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

        // store the tokens for later requests
        if (tokens != null)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(tokens.TokenType, tokens.AccessToken);
            _refreshToken = tokens.RefreshToken;
            IsAuthenticated = true;
            // _localDataService.SaveUserAsync(_refreshToken, tokens.RefreshTokenExpiresAt);
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

    // store the tokens
    if (tokens != null)
    {
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(tokens.TokenType, tokens.AccessToken);
        _refreshToken = tokens.RefreshToken;
        IsAuthenticated = true;
        // _localDataService.SaveUserAsync(_refreshToken, tokens.RefreshTokenExpiresAt) ;
    }

    return tokens;
    }

    public void Logout()
    {
        IsAuthenticated = false;
        CurrentUsername = null;
        _refreshToken = null;
        _http.DefaultRequestHeaders.Authorization = null;
        // _localDataService.LogoutAsync();

    }

    // method no ensure authentication of current session
    public async Task<MeResponse> AuthMeAsync()
    {
        // Ensure we even have a token attached
        if (_http.DefaultRequestHeaders.Authorization == null)
            throw new InvalidOperationException("Missing Authorization header – user is not logged in.");

        var response = await ExecuteWithTokenRefreshAsync(() => _http.GetAsync("auth/me"));

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new HttpRequestException("Not authenticated or token expired. Please sign in again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to call /auth/me: {(int)response.StatusCode} {body}");
        }

        var me = await response.Content.ReadFromJsonAsync<MeResponse>(_options);
        if (me == null)
            throw new InvalidOperationException("Empty response from /auth/me.");

        return me;
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(_refreshToken))
        {
            IsAuthenticated = false;
            return false;
        }

        try
        {
            var payload = new { refreshToken = _refreshToken };
            var response = await _http.PostAsJsonAsync("auth/refresh", payload, _options);

            if (!response.IsSuccessStatusCode)
            {
                // Refresh token is invalid or expired
                IsAuthenticated = false;
                _refreshToken = null;
                _http.DefaultRequestHeaders.Authorization = null;
                return false;
            }

            var tokens = await response.Content.ReadFromJsonAsync<AuthResponse>(_options);
            if (tokens != null)
            {
                // Update the authorization header with new access token
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(tokens.TokenType, tokens.AccessToken);
                _refreshToken = tokens.RefreshToken;
                IsAuthenticated = true;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing token: {ex.Message}");
            IsAuthenticated = false;
            _refreshToken = null;
            _http.DefaultRequestHeaders.Authorization = null;
            return false;
        }
    }

    public async Task<HttpResponseMessage> ExecuteWithTokenRefreshAsync(Func<Task<HttpResponseMessage>> apiCall, int maxRetries = 1)
    {
        var response = await apiCall();

        // If the call succeeds or it's not an auth issue, return immediately
        if (response.IsSuccessStatusCode || response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        // Try to refresh token and retry the call
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var refreshSuccess = await RefreshTokenAsync();
            if (!refreshSuccess)
            {
                // Refresh failed, return the original unauthorized response
                return response;
            }

            // Retry the original API call with the new token
            response.Dispose(); // Clean up the previous response
            response = await apiCall();

            if (response.IsSuccessStatusCode || response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }
        }

        return response;
    }

    public async Task<T?> ExecuteWithTokenRefreshAsync<T>(Func<Task<HttpResponseMessage>> apiCall, int maxRetries = 1)
    {
        var response = await ExecuteWithTokenRefreshAsync(apiCall, maxRetries);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(_options);
        }

        return default(T);
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

    // public async Task<bool> UpdateUsernameAsync(string newUsername)
    // {
    
    // }

    public async Task<bool> UpdatePasswordAsync(string newPassword, string oldPassword)
    {
        // Validate the new password first
        if (!ValidatePassword(newPassword))
        {
            return false;
        }

        try
        {
            var payload = new {
                newPassword = newPassword,
                oldPassword = oldPassword
            };

            var response = await ExecuteWithTokenRefreshAsync(() =>
                _http.PostAsJsonAsync("auth/change-password", payload, _options));

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating password: {ex.Message}");
            return false;
        }
    }

    // Test helper methods


    
}
