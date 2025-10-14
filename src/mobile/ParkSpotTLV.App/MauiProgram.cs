
using Microsoft.Extensions.Logging;
using ParkSpotTLV.App.Controls; // if you DI MenuOverlay or other controls
using ParkSpotTLV.App.Services;

using System.Text.Json;

namespace ParkSpotTLV.App {
    public static class MauiProgram {
        // Android emulator -> host mapping. Match your backend port.
        private const string DevApiBaseUrl = "http://10.0.2.2:8080/";

        public static MauiApp CreateMauiApp() {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            // Shared serializer options (optional but handy)
            builder.Services.AddSingleton(new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            });

            // Single named HttpClient used by BOTH services
            builder.Services.AddHttpClient("Api", client => {
                client.BaseAddress = new Uri(DevApiBaseUrl);
            });



            // ---- Register your refactored services (DI-friendly) ----
            // Single shared HttpClient instance
            builder.Services.AddSingleton<HttpClient>(sp => {
                return sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api");
            });

            builder.Services.AddSingleton<ILocalDataService, LocalDataService>();

            builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>(sp => {
                var http = sp.GetRequiredService<HttpClient>();
                var opts = sp.GetRequiredService<JsonSerializerOptions>();
                var db = sp.GetRequiredService<ILocalDataService>();
                return new AuthenticationService(http, db, opts);
            });

            builder.Services.AddSingleton<ICarService, CarService>(sp => {
                var http = sp.GetRequiredService<HttpClient>();
                var auth = sp.GetRequiredService<IAuthenticationService>();
                var opts = sp.GetRequiredService<JsonSerializerOptions>();
                return new CarService(http, auth, opts);
            });

            builder.Services.AddSingleton<IMapService, MapService>(sp => {
                var http = sp.GetRequiredService<HttpClient>();
                var auth = sp.GetRequiredService<IAuthenticationService>();
                var opts = sp.GetRequiredService<JsonSerializerOptions>();
                var db = sp.GetRequiredService<ILocalDataService>();
                return new MapService(http, auth, db, opts);
            });

            builder.Services.AddSingleton<IParkingService, ParkingService>(sp =>
            {
                var http = sp.GetRequiredService<HttpClient>();
                var auth = sp.GetRequiredService<IAuthenticationService>();
                var opts = sp.GetRequiredService<JsonSerializerOptions>();
                return new ParkingService(http, auth, opts);
            });

            builder.Services.AddSingleton<MapSegmentRenderer>();
            builder.Services.AddSingleton<IMapInteractionService, MapInteractionService>();
            builder.Services.AddSingleton<ParkingPopUps>();

            

            // Core app services you already had
            builder.Services.AddTransient<Pages.MainPage>();
            builder.Services.AddTransient<Pages.PreferencesPage>();
            builder.Services.AddTransient<Pages.SignUpPage>();
            builder.Services.AddTransient<Pages.AccountDetailsPage>();
            builder.Services.AddTransient<Pages.ShowMapPage>();
            builder.Services.AddTransient<Pages.AddCarPage>();
            builder.Services.AddTransient<Pages.AccountDetailsPage>();

            // (Optional) if you resolved MenuOverlay via DI
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<MenuOverlay>();


#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}
