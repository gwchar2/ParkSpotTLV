using ParkSpotTLV.App.Data.Services;

namespace ParkSpotTLV.App
{
    public partial class App : Application
    {
        private readonly AppShell _shell;
        private readonly ILocalDataService _localData;

        // AppShell and ILocalDataService come from DI
        public App(AppShell shell, ILocalDataService localData)
        {
            InitializeComponent();
            _shell = shell;
            _localData = localData;

            _ = InitializeDatabaseAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Provide the root page for the app via a Window
            return new Window(_shell);
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                await _localData.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
