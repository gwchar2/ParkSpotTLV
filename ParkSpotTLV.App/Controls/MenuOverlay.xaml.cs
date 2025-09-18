namespace ParkSpotTLV.App.Controls;

public partial class MenuOverlay : Grid
{
    public MenuOverlay()
    {
        InitializeComponent();
    }

    public async Task ShowMenu()
    {
        // Reset position first
        MenuPanel.TranslationX = 250;
        IsVisible = true;
        await MenuPanel.TranslateTo(0, 0, 300, Easing.CubicOut);
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
        if (Application.Current?.Windows.FirstOrDefault()?.Page is Page page)
        {
            bool confirm = await page.DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (confirm)
            {
                await Shell.Current.GoToAsync("//MainPage");
            }
        }
    }
}