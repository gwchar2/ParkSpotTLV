using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ParkSpotTLV.App {
    public static class MauiProgram {

        const string DevApiBaseUrl = "http://10.0.2.2:5293/";

        public static MauiApp CreateMauiApp() {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddHttpClient("api", client => {
                client.BaseAddress = new Uri(DevApiBaseUrl);
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
