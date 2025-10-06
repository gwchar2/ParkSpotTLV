using ParkSpotTLV.App.Data.Services;
using ParkSpotTLV.App.Services;


namespace ParkSpotTLV.App
{
    public partial class App : Application
    {
        private readonly AppShell _shell;
        private readonly ILocalDataService _localData;
        private readonly AuthenticationService _authService;

        // AppShell and ILocalDataService come from DI
        public App(AppShell shell, ILocalDataService localData, AuthenticationService authService)
        {
            InitializeComponent();
            _shell = shell;
            _localData = localData;
            _authService = authService;

            _ = InitializeAppAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Provide the root page for the app via a Window
            return new Window(_shell);
        }

        private async Task InitializeAppAsync()
        {
            try
            {
                // Step 1: Initialize database first
                await _localData.InitializeAsync();

                // Step 2: Try auto-login
                bool success = await _authService.TryAutoLoginAsync();

                if (success)
                {
                    // Auto-login succeeded, navigate to map page
                    await Shell.Current.GoToAsync("ShowMapPage");
                }
                // If auto-login fails, user stays on MainPage (login screen)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
