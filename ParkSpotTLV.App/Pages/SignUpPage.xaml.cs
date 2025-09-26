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

        try
        {
            bool success = await _authService.SignUpAsync(username, password);

            if (success)
            {
                await DisplayAlert("Success", $"Account created successfully! Welcome, {username}!", "OK");
                await Shell.Current.GoToAsync("..");
                await Shell.Current.GoToAsync("ShowMapPage");
            }
            else
            {
                await DisplayAlert("Error", "Username already exists. Please choose a different username.", "OK");
            }
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