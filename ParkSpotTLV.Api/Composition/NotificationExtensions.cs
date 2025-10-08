
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParkSpotTLV.Api.Features.Notifications.Options;
using ParkSpotTLV.API.Features.Notifications.Services;
using System.ComponentModel.DataAnnotations;

namespace ParkSpotTLV.Api.Composition {
    public static class NotificationExtensions {

        public static IServiceCollection AddNotification(this IServiceCollection services,IConfiguration config) {

            services.AddOptions<FirebaseOptions>().Bind(config.GetSection(FirebaseOptions.SectionName))
               .Validate(o => {
                   // Mode must be either V1 or Legacy
                   var mode = (o.Mode ?? "V1").Trim();
                   if (mode is not ("V1" or "Legacy")) return false;

                   if (mode == "Legacy") {
                       // Legacy requires a ServerKey
                       return !string.IsNullOrWhiteSpace(o.ServerKey);
                   }

                   // V1 requires ProjectId and a valid ServiceAccount source
                   if (string.IsNullOrWhiteSpace(o.ProjectId)) return false;
                   if (o.ServiceAccount is null) return false;

                   var src = (o.ServiceAccount.Source ?? "Path").Trim();
                   if (src == "Path") {
                       return !string.IsNullOrWhiteSpace(o.ServiceAccount.JsonPath);
                   }
                   if (src == "Base64") {
                       return !string.IsNullOrWhiteSpace(o.ServiceAccount.JsonBase64);
                   }
                   return false;
               }, $"{FirebaseOptions.SectionName}: configuration invalid (check Mode, ProjectId, and ServiceAccount settings).")
               .ValidateOnStart();

            services.AddOptions<HangfireOptions>()
                .Bind(config.GetSection(HangfireOptions.SectionName))
                .ValidateDataAnnotations()
                .Validate(obj => obj.UseMainDbConnection || !string.IsNullOrWhiteSpace(obj.ConnectionString),
                          $"{HangfireOptions.SectionName}: either UseMainDbConnection=true or provide ConnectionString")
                .ValidateOnStart();

            services.AddOptions<NotificationsOptions>()
                .Bind(config.GetSection(NotificationsOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();




            services.AddHttpClient();
            services.AddSingleton<IFcmV1Sender, FcmV1Sender>();
            return services;

        }

    }
}
