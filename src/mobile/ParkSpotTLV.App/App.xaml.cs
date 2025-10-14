using ParkSpotTLV.App.Services;

namespace ParkSpotTLV.App
{
    public partial class App : Application
    {
        private readonly AppShell _shell;
        private readonly ILocalDataService _localData;
        private readonly IAuthenticationService _authService;

        // AppShell and LocalDataService come from DI
        public App(AppShell shell, ILocalDataService localData, IAuthenticationService authService)
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
                System.Diagnostics.Debug.WriteLine("Starting app initialization...");

                // Step 1: Initialize database first
                await _localData.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("Database initialized");

                // Step 2: Try auto-login
                bool success = await _authService.TryAutoLoginAsync();
                System.Diagnostics.Debug.WriteLine($"Auto-login result: {success}");

                // Add a small delay to ensure Shell is ready
                await Task.Delay(500);

                if (success)
                {
                    // Auto-login succeeded, navigate to map page
                    System.Diagnostics.Debug.WriteLine("Navigating to ShowMapPage...");
                    await Shell.Current.GoToAsync("//ShowMapPage");
                }
                else
                {
                    // Auto-login failed, navigate to login page
                    System.Diagnostics.Debug.WriteLine("Navigating to MainPage...");
                    await Shell.Current.GoToAsync("//MainPage");
                }

                System.Diagnostics.Debug.WriteLine("Navigation completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"App initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Fallback to login page on error
                try
                {
                    await Shell.Current.GoToAsync("//MainPage");
                }
                catch { }
            }
        }
    }
}
