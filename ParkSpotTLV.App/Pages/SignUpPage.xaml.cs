using ParkSpotTLV.App.Services;

namespace ParkSpotTLV.App.Pages;

public partial class SignUpPage : ContentPage
{
    private readonly AuthenticationService _authService ; // = AuthenticationService.Instance
    private readonly CarService _carService; //  = CarService.Instance


    public SignUpPage(CarService carService, AuthenticationService authService)
    {
        InitializeComponent();
        _carService = carService;
        _authService = authService;
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text?.Trim() ?? "";

        // Validate input
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Error", "Please enter both username and password", "OK");
            return;
        }

        // Validate username format
        if (!_authService.ValidateUsername(username))
        {
            await DisplayAlert("Error", "Username must be at least 3 characters and contain only letters, numbers, and underscores.", "OK");
            return;
        }

        // Validate password strength
        if (!_authService.ValidatePassword(password))
        {
            await DisplayAlert("Error", "Password must be at least 6 characters long.", "OK");
            return;
        }

        // Disable create account button during registration
        CreateAccountBtn.IsEnabled = false;
        CreateAccountBtn.Text = "Creating Account...";

        try {
            var tokens = await _authService.SignUpAsync(username, password); // throws on 400/409

            if (tokens is not null) {
                await DisplayAlert("Success", $"Account created successfully! Welcome, {username}!", "OK");

                // add a default car for the new user
                try
                {
                    var defaultCar = await _carService.CreateDefaultCarForUserAsync();
                    // DEBUG: await DisplayAlert("Debug", $"Default car created successfully: {defaultCar.Id}", "OK");
                }
                catch (Exception )
                {
                    // Don't fail signup if car creation fails - user account was already created
                    // DEBUG: await DisplayAlert("Debug", $"Failed to create default car: {ex.Message}", "OK");
                }

                // navigate
                await Shell.Current.GoToAsync("..");
                await Shell.Current.GoToAsync("ShowMapPage");
            }
        }
        catch (HttpRequestException ex) // contains status + body from the service
        {
            string msg;
            if (ex.Message.Contains("400"))
                msg = "Missing username or password. Please try again.";
            else if (ex.Message.Contains("409"))
                msg = "This username is already taken. Please choose another one.";
            else
                msg = $"Account creation failed: {ex.Message}";

            await DisplayAlert("Error", msg, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Account creation failed: {ex.Message}", "OK");
        }
        finally
        {
            // Re-enable create account button
            CreateAccountBtn.IsEnabled = true;
            CreateAccountBtn.Text = "Create Account";
        }
    }
}