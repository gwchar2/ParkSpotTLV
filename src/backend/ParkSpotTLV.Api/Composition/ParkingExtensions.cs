
using ParkSpotTLV.Api.Features.Parking.Query;
using ParkSpotTLV.Api.Features.Parking.Services;
using ParkSpotTLV.Api.Features.AutoStop;


/* 
 * Feature Extensions Methods for the map segments evaluation feature.
 * Initiated by program.cs 
 */
namespace ParkSpotTLV.Api.Composition {

    public static class ParkingExtensions {
        public static IServiceCollection AddParking(this IServiceCollection services) {

            // Query / Reading Data
            services.AddScoped<ISegmentQueryService, SegmentQueryService>();

            // Helper Services
            services.AddScoped<ITariffCalendarService, TariffCalendarService>();
            services.AddScoped<ILegalPolicyService, LegalPolicyService>();
            services.AddScoped<IPaymentDecisionService, PaymentDecisionService>();
            services.AddScoped<IDailyBudgetService, DailyBudgetService>();

            // Availability + Classifications
            services.AddScoped<IAvailabilityService, AvailabilityService>();
            services.AddScoped<IClassificationService, ClassificationService>();

            // Evaluation Service
            services.AddScoped<ISegmentEvaluationService, SegmentEvaluationService>();

            // Auto Stop Parking Service
            services.AddHostedService<AutoStopParkingService>();


            return services;
        }
    }

}