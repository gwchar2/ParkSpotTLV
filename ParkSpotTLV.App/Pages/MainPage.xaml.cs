using ParkSpotTLV.App.Services;
using System.Net;

namespace ParkSpotTLV.App.Pages {
    public partial class MainPage : ContentPage {
        private readonly AuthenticationService _authService ; // = AuthenticationService.Instance

        public MainPage(AuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        private async void OnLoginClicked(object? sender, EventArgs e) {
            string username = UsernameEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text?.Trim() ?? "";

            // Validate input
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
                await DisplayAlert("Error", "Missing username or password.", "OK");
                return;
            }

            // Disable login button during authentication
            LoginBtn.IsEnabled = false;
            LoginBtn.Text = "Logging in...";

            try {
                var tokens = await _authService.LoginAsync(username, password); // throws on non-2xx

                if (tokens is not null)
                {
                    // token is already attached to HttpClient by the service
                    await Shell.Current.GoToAsync("ShowMapPage");
                }
            }
            catch (HttpRequestException ex) {
                // You can map common cases by checking ex.Message or wrapping extra info in the service
                // e.g., if it contains "401" -> invalid credentials, "400" -> missing fields, etc.
                var msg = ex.Message.Contains("401") ? "Invalid username or password."
                        : ex.Message.Contains("400") ? "Missing username or password."
                        : "Login failed. Please try again.";
                await DisplayAlert("Error", msg, "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
            }

            finally
            {
                // Re-enable login button
                LoginBtn.IsEnabled = true;
                LoginBtn.Text = "Log In";
            }
        }

        private async void OnSignUpClicked(object? sender, EventArgs e) {
            await Shell.Current.GoToAsync("SignUpPage");
        }
    }
}
