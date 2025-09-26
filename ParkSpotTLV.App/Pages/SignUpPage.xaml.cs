using ParkSpotTLV.App.Services;

namespace ParkSpotTLV.App.Pages;

public partial class SignUpPage : ContentPage
{
    private readonly AuthenticationService _authService = AuthenticationService.Instance;

    public SignUpPage()
    {
        InitializeComponent();
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

                // (optional) add a default car for the new user
                //await AddDefaultCarAsync();   // shown below

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
                msg = "Account creation failed. Please try again later.";

            await DisplayAlert("Error", msg, "OK");
        }
        catch (Exception)
        {
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