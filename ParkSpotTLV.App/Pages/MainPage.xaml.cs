using ParkSpotTLV.App.Services;
using System.Net;

namespace ParkSpotTLV.App.Pages {
    public partial class MainPage : ContentPage {
        private readonly AuthenticationService _authService = AuthenticationService.Instance;

        public MainPage() {
            InitializeComponent();
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

            try
            {
                var response = await _authService.LoginAsync(username, password);
                Console.WriteLine($"Response: {response.StatusCode}");

                switch (response.StatusCode)
            {

                case HttpStatusCode.OK:
                {
                    await Shell.Current.GoToAsync("ShowMapPage");
                    break;
                }
                case HttpStatusCode.BadRequest:
                {
                    await DisplayAlert("Error", "Missing username or password. Please try again.", "OK");
                    break;
                }
                case HttpStatusCode.Unauthorized:
                {
                    await DisplayAlert("Error", "Invalid username or password. Please try again.", "OK");
                    break;
                }
                default:
                {
                    await DisplayAlert("Error", "Login failed. Please try again.", "OK");
                    break;
                }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
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
