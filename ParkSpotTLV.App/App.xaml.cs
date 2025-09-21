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
                var localDataService = Handler?.MauiContext?.Services?.GetService<ILocalDataService>();
                if (localDataService != null)
                {
                    await localDataService.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");
                // Show error in UI for debugging
                var mainPage = Application.Current?.Windows?[0]?.Page;
                mainPage?.DisplayAlert("Database Error", $"Init failed: {ex.Message}", "OK");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState) {
            return new Window(new AppShell());
        }
    }
}