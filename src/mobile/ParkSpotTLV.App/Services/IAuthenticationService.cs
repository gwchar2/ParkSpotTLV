using ParkSpotTLV.App.Data.Models;
using ParkSpotTLV.Contracts.Auth;

namespace ParkSpotTLV.App.Services;

public interface IAuthenticationService
{
    Task<bool> TryAutoLoginAsync();
    Task<TokenPairResponse?> LoginAsync(string username, string password);
    Task<TokenPairResponse?> SignUpAsync(string username, string password);
    Task<UserMeResponse> AuthMeAsync();
    Task Logout();
    Task<bool> RefreshTokenAsync();
    Task<HttpResponseMessage> ExecuteWithTokenRefreshAsync(Func<Task<HttpResponseMessage>> apiCall, int maxRetries = 1);
    Task<T?> ExecuteWithTokenRefreshAsync<T>(Func<Task<HttpResponseMessage>> apiCall, int maxRetries = 1);
    bool ValidateUsername(string username);
    bool ValidatePassword(string password);
    Task<bool> UpdatePasswordAsync(string newPassword, string oldPassword);
}
