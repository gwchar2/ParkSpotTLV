
using ParkSpotTLV.Api.Features.Parking.Query;
using ParkSpotTLV.Api.Features.Parking.Services;


namespace ParkSpotTLV.Api.Composition {
    /* ----------------------------------------------------------------------
     * Feature Extensions Methods for the map segments evaluation feature.
     * Initiated by program.cs 
     * ---------------------------------------------------------------------- */

    public static class ParkingExtensions {
        public static IServiceCollection AddParking(this IServiceCollection services) {

            // Query / reading data
            services.AddScoped<ISegmentQueryService, SegmentQueryService>();

            // Helper services
            services.AddScoped<ITariffCalendarService, TariffCalendarService>();
            services.AddScoped<ILegalPolicyService, LegalPolicyService>();
            services.AddScoped<IPaymentDecisionService, PaymentDecisionService>();
            services.AddScoped<IDailyBudgetService, DailyBudgetService>();
            services.AddScoped<IDailyBudgetService, DailyBudgetService>();

            // Availability + Classifications
            services.AddScoped<IAvailabilityService, AvailabilityService>();
            services.AddScoped<IClassificationService, ClassificationService>();

            // Evaluation service
            services.AddScoped<ISegmentEvaluationService, SegmentEvaluationService>();


            return services;
        }
    }

}