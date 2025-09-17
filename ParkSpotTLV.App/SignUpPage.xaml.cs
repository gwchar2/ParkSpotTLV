namespace ParkSpotTLV.App;

public partial class SignUpPage : ContentPage
{
    public SignUpPage()
    {
        InitializeComponent();
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        string username = UsernameEntry.Text?.Trim() ?? "";
        string password = PasswordEntry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Error", "Please enter both username and password", "OK");
            return;
        }

        await DisplayAlert("Success", $"Account created for: {username}", "OK");
        await Shell.Current.GoToAsync("..");
        await Shell.Current.GoToAsync("ShowMapPage");
    }
}