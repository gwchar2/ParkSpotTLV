using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ParkSpotTLV.App.Data.Models;
using ParkSpotTLV.Contracts.Auth;

namespace ParkSpotTLV.App.Services;

/*
* Handles user authentication operations including login, signup, token refresh, and validation.
* Manages JWT tokens and authentication state for API requests.
*/
public class AuthenticationService
{
    private readonly HttpClient _http;
    private readonly LocalDataService _localDataService;
    private readonly JsonSerializerOptions _options;

    /*
    * Initializes the authentication service with HTTP client and local data storage.
    */
    public AuthenticationService(HttpClient http, LocalDataService localDataService, JsonSerializerOptions? options = null)
    {
        _http = http;
        _localDataService = localDataService;
        _options = options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /*
    * Attempts to automatically log in user using stored refresh token.
    * Returns true if auto-login succeeded, false otherwise.
    */
    public async Task<bool> TryAutoLoginAsync()
    {
        var session = await _localDataService.GetSessionAsync();

        // No session exists
        if (session is null || string.IsNullOrEmpty(session.RefreshToken))
            return false;

        // Session expired - clean it up
        if (session.TokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            await _localDataService.DeleteSessionAsync();
            return false;
        }

        // Valid session - refresh the access token
        return await RefreshTokenAsync();
    }

    /*
    * Authenticates user with username and password.
    * Creates local session and stores tokens on success.
    */
    public async Task<TokenPairResponse?> LoginAsync(string username, string password)
    {
        var payload = new { username, password };
        var response = await _http.PostAsJsonAsync("auth/login", payload, _options);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Login failed: {response.StatusCode} {error}");
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenPairResponse>(_options);

        // store the tokens for later requests
        if (tokens != null)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(tokens.TokenType, tokens.AccessToken);

            // new session
            var newSession = new Session
            {
                RefreshToken = tokens.RefreshToken,
                TokenExpiresAt = tokens.RefreshTokenExpiresAt,
                UserName = username
            };
            await _localDataService.AddSessionAsync(newSession);
        }

        return tokens;
    }

    /*
    * Registers a new user with username and password.
    * Creates local session and stores tokens on success.
    */
    public async Task<TokenPairResponse?> SignUpAsync(string username, string password)
    {
        var payload = new { username, password };
        var response = await _http.PostAsJsonAsync("auth/register", payload, _options);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Sign-up failed: {response.StatusCode} {error}");
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenPairResponse>(_options);

        // store the tokens
        if (tokens != null)
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(tokens.TokenType, tokens.AccessToken);

            // new session
            var newSession = new Session
            {
                RefreshToken = tokens.RefreshToken,
                TokenExpiresAt = tokens.RefreshTokenExpiresAt,
                UserName = username
            };
            await _localDataService.AddSessionAsync(newSession);
        }

        return tokens;
    }

    /*
    * Fetches current authenticated user information from API.
    * Automatically refreshes token if needed.
    */
    public async Task<UserMeResponse> AuthMeAsync()
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

        var me = await response.Content.ReadFromJsonAsync<UserMeResponse>(_options);
        if (me == null)
            throw new InvalidOperationException("Empty response from /auth/me.");

        return me;
    }

    /*
    * Logs out current user by clearing tokens and local session.
    */
    public async Task Logout()
    {
        _http.DefaultRequestHeaders.Authorization = null;
        await _localDataService.DeleteSessionAsync();
    }

    /*
    * Refreshes the access token using stored refresh token.
    * Updates authorization header and local session with new tokens.
    */
    public async Task<bool> RefreshTokenAsync()
    {
        var session = await _localDataService.GetSessionAsync();
        if (string.IsNullOrEmpty(session?.RefreshToken))
        {
            return false;
        }

        try
        {
            var payload = new { refreshToken = session.RefreshToken };
            var response = await _http.PostAsJsonAsync("auth/refresh", payload, _options);

            if (!response.IsSuccessStatusCode)
            {
                _http.DefaultRequestHeaders.Authorization = null;
                return false;
            }

            var tokens = await response.Content.ReadFromJsonAsync<TokenPairResponse>(_options);
            if (tokens != null)
            {
                // Update the authorization header with new access token
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue(tokens.TokenType, tokens.AccessToken);

                await _localDataService.UpdateTokenAsync(tokens.RefreshToken, tokens.RefreshTokenExpiresAt);

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error refreshing token: {ex.Message}");
            _http.DefaultRequestHeaders.Authorization = null;
            return false;
        }
    }

    /*
    * Executes an API call with automatic token refresh on 401 Unauthorized.
    * Retries the call with refreshed token if initial call fails due to auth.
    */
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

    /*
    * Executes an API call with automatic token refresh and deserializes response to type T.
    * Generic version of ExecuteWithTokenRefreshAsync that returns typed result.
    */
    public async Task<T?> ExecuteWithTokenRefreshAsync<T>(Func<Task<HttpResponseMessage>> apiCall, int maxRetries = 1)
    {
        var response = await ExecuteWithTokenRefreshAsync(apiCall, maxRetries);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<T>(_options);
        }

        return default(T);
    }

    /*
    * Validates username format. Must be at least 3 characters, alphanumeric and underscore only.
    * Returns true if username is valid, false otherwise.
    */
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

    /*
    * Validates password format. Must be at least 6 characters with no whitespace.
    * Returns true if password is valid, false otherwise.
    */
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

    /*
    * Updates user password. Validates new password and sends change request to API.
    * Returns true if password update succeeded, false otherwise.
    */
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
}
