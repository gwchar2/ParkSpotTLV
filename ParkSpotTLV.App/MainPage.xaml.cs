namespace ParkSpotTLV.App {
    public partial class MainPage : ContentPage {
        public MainPage() {
            InitializeComponent();
        }

        private async void OnLoginClicked(object? sender, EventArgs e) {
            string username = UsernameEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
                await DisplayAlert("Error", "Please enter both username and password", "OK");
                return;
            }

            //await DisplayAlert("Login", $"Logging in user: {username}", "OK");
            await Shell.Current.GoToAsync("ShowMapPage");
        }

        private async void OnSignUpClicked(object? sender, EventArgs e) {
            await Shell.Current.GoToAsync("SignUpPage");
        }
    }
}
