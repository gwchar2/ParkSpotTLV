using ParkSpotTLV.App.Data.Services;

namespace ParkSpotTLV.App {
    public partial class App : Application {
        public App() {
            InitializeComponent();
            InitializeDatabaseAsync();
        }

        private async void InitializeDatabaseAsync()
        {
            try
            {
                // Create the service directly instead of relying on DI during app construction
                var localDataService = new LocalDataService();
                await localDataService.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("Database initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState) {
            return new Window(new AppShell());
        }
    }
}