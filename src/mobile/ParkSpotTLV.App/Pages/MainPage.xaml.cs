using ParkSpotTLV.App.Services;

namespace ParkSpotTLV.App.Pages {
    /*
    * Main login page for user authentication.
    * Entry point for returning users to log in.
    */
    public partial class MainPage : ContentPage {
        private readonly AuthenticationService _authService ;

        /*
        * Initializes the MainPage with required services.
        */
        public MainPage(AuthenticationService authService)
        {
            InitializeComponent();
            _authService = authService;
        }

        /*
        * Handles login button click. Authenticates user and navigates to map page.
        */
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
                System.Diagnostics.Debug.WriteLine($"Login failed: {ex.Message}");

                var msg = ex.Message.Contains("401") ? "Invalid username or password."
                        : ex.Message.Contains("400") ? "Missing username or password."
                        : "Login failed. Please check your connection and try again.";
                await DisplayAlert("Error", msg, "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Unexpected login error: {ex.Message}");
                await DisplayAlert("Error", "Login failed. Please try again later.", "OK");
            }

            finally
            {
                // Re-enable login button
                LoginBtn.IsEnabled = true;
                LoginBtn.Text = "Log In";
            }
        }

        /*
        * Handles sign up button click. Navigates to SignUpPage.
        */
        private async void OnSignUpClicked(object? sender, EventArgs e) {
            await Shell.Current.GoToAsync("SignUpPage");
        }
    }
}
