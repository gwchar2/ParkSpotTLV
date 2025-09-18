using ParkSpotTLV.Core.Services;

namespace ParkSpotTLV.App;

public partial class AccountDetailsPage : ContentPage
{
    private readonly AuthenticationService _authService = AuthenticationService.Instance;

    public AccountDetailsPage()
    {
        InitializeComponent();
        LoadUserData();
    }

    private void LoadUserData()
    {
        // Load current user data
        if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.CurrentUsername))
        {
            UsernameEntry.Text = _authService.CurrentUsername;
        }
    }

    private async void OnEditUsernameClicked(object sender, EventArgs e)
    {
        if (EditUsernameBtn.Text == "Edit")
        {
            // Enable editing
            UsernameEntry.IsReadOnly = false;
            UsernameEntry.BackgroundColor = Colors.White;
            EditUsernameBtn.Text = "Save";
            EditUsernameBtn.BackgroundColor = Color.FromArgb("#4CAF50");
        }
        else
        {
            // Save changes
            UsernameEntry.IsReadOnly = true;
            UsernameEntry.BackgroundColor = Colors.LightGray;
            EditUsernameBtn.Text = "Edit";
            EditUsernameBtn.BackgroundColor = Color.FromArgb("#2E7D32");

            await DisplayAlert("Success", "Username updated successfully!", "OK");
        }
    }

    private async void OnEditPasswordClicked(object sender, EventArgs e)
    {
        if (EditPasswordBtn.Text == "Edit")
        {
            // Enable editing
            PasswordEntry.IsReadOnly = false;
            PasswordEntry.BackgroundColor = Colors.White;
            PasswordEntry.Text = ""; // Clear for new password input
            EditPasswordBtn.Text = "Save";
            EditPasswordBtn.BackgroundColor = Color.FromArgb("#4CAF50");
        }
        else
        {
            // Validate password
            if (string.IsNullOrWhiteSpace(PasswordEntry.Text) || PasswordEntry.Text.Length < 6)
            {
                await DisplayAlert("Error", "Password must be at least 6 characters long.", "OK");
                return;
            }

            // Save changes
            PasswordEntry.IsReadOnly = true;
            PasswordEntry.BackgroundColor = Colors.LightGray;
            PasswordEntry.Text = "••••••••"; // Hide password again
            EditPasswordBtn.Text = "Edit";
            EditPasswordBtn.BackgroundColor = Color.FromArgb("#2E7D32");

            await DisplayAlert("Success", "Password updated successfully!", "OK");
        }
    }

    private async void OnCarTapped(object sender, EventArgs e)
    {
        // For now, navigate to a placeholder edit car page
        // Later we'll pass the car details as parameters
        await Shell.Current.GoToAsync("EditCarPage");
    }

    private async void OnRemoveCarClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string carName)
        {
            bool confirm = await DisplayAlert("Remove Car",
                $"Are you sure you want to remove '{carName}'?",
                "Yes", "No");

            if (confirm)
            {
                // TODO: Actually remove the car from the list
                // For now, just show confirmation
                await DisplayAlert("Success", $"'{carName}' has been removed.", "OK");

                // Here you would remove the car from your data source and refresh the UI
                // For now, we'll just show the confirmation
            }
        }
    }

    private async void OnAddCarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AddCarPage");
    }

    private async void OnFindParkingClicked(object sender, EventArgs e)
    {
        // Navigate to ShowMapPage
        await Shell.Current.GoToAsync("ShowMapPage");
    }
}