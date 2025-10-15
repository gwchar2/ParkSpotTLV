using ParkSpotTLV.App.Services;
using ParkSpotTLV.App.Data.Models;

namespace ParkSpotTLV.App.Pages;

/*
* Sign up page for new user registration.
* Creates user account and default car, then navigates to map.
*/
public partial class SignUpPage : ContentPage
{
    private readonly IAuthenticationService _authService ;
    private readonly ICarService _carService;

    /*
    * Initializes the SignUpPage with required services.
    */
    public SignUpPage(ICarService carService, IAuthenticationService authService)
    {
        InitializeComponent();
        _carService = carService;
        _authService = authService;
    }

    /*
    * Handles create account button click. Validates input, creates account and default car.
    */
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
                    var defCar = new Car
                        {
                            Name = "Default Car",
                            Type = CarType.Private,
                            HasResidentPermit = false,
                            ResidentPermitNumber = 0,
                            HasDisabledPermit = false
                        };
                    await _carService.AddCarAsync(defCar);
                    // DEBUG: await DisplayAlert("Debug", $"Default car created successfully: {defaultCar.Id}", "OK");
                }
                catch (Exception ex)
                {
                    // Don't fail signup if car creation fails - user account was already created
                    System.Diagnostics.Debug.WriteLine($"Failed to create default car: {ex.Message}");
                }

                // navigate
                await Shell.Current.GoToAsync("///ShowMapPage");
            }
        }
        catch (HttpRequestException ex) // contains status + body from the service
        {
            System.Diagnostics.Debug.WriteLine($"Signup failed: {ex.Message}");

            string msg;
            if (ex.Message.Contains("400"))
                msg = "Missing username or password. Please try again.";
            else if (ex.Message.Contains("409"))
                msg = "This username is already taken. Please choose another one.";
            else
                msg = "Unable to create account. Please check your connection and try again.";

            await DisplayAlert("Error", msg, "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected signup error: {ex.Message}");
            await DisplayAlert("Error", "Account creation failed. Please try again later.", "OK");
        }
        finally
        {
            // Re-enable create account button
            CreateAccountBtn.IsEnabled = true;
            CreateAccountBtn.Text = "Create Account";
        }
    }
}