using ParkSpotTLV.App.Services;
using ParkSpotTLV.App.Data.Services;
using System.Net.Http.Json;

namespace ParkSpotTLV.App.Controls;

public partial class MenuOverlay : Grid{

    public record VersionResponse(string Version);
    private readonly AuthenticationService _authService;
    private readonly ILocalDataService _localDataService;
    private readonly HttpClient _http;
    public MenuOverlay(AuthenticationService authService, IHttpClientFactory httpFactory, ILocalDataService localDataService)
    {
        InitializeComponent();
        _authService = authService;
        _localDataService = localDataService ;
        _http = httpFactory.CreateClient("Api");
    }

    public async Task ShowMenu()
    {
        // Reset position first
        MenuPanel.TranslationX = 250;
        IsVisible = true;
        await MenuPanel.TranslateTo(0, 0, 300, Easing.CubicOut);

        /* Will fetch version */
        //using var client = new HttpClient();
        try {
            var response = await _http.GetFromJsonAsync<VersionResponse>("http://10.0.2.2:8080/version");
            VersionLabel.Text = $"Version: {response?.Version ?? "N/A"}";
        }
        catch (Exception ex) {
            VersionLabel.Text = $"Error: {ex.Message}";
        }

    }

    private async void OnOverlayTapped(object sender, EventArgs e)
    {
        await CloseMenu();
    }

    private async Task CloseMenu()
    {
        await MenuPanel.TranslateTo(250, 0, 300, Easing.CubicIn);
        IsVisible = false;

        // Remove the menu overlay from the page
        if (Shell.Current is AppShell appShell)
        {
            appShell.RemoveMenuOverlay();
        }
    }

    private async void OnAccountDetailsClicked(object sender, EventArgs e)
    {
        await CloseMenu();
        await Shell.Current.GoToAsync("AccountDetailsPage");
    }

    private async void OnPreferencesClicked(object sender, EventArgs e)
    {
        await CloseMenu();
        await Shell.Current.GoToAsync("PreferencesPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await CloseMenu();

        //var authService = AuthenticationService.Instance;
        var session = await _localDataService.GetSessionAsync();
        string username = session?.UserName ?? "User";

        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
        {
            bool confirm = await page.DisplayAlert(
                "Logout",
                $"Are you sure you want to logout, {username}?",
                "Yes", "No");

            if (confirm)
            {
                // Logout from authentication service
                await _authService.Logout();

                // Navigate back to login page
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
    }

}