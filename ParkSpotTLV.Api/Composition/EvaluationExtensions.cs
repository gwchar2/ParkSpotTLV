using Microsoft.Extensions.DependencyInjection;
using ParkSpotTLV.Api.Services.Evaluation;
using ParkSpotTLV.Api.Services.Evaluation.Facade;
using ParkSpotTLV.Api.Services.Evaluation.Logic;
using ParkSpotTLV.Api.Services.Evaluation.Query;
using ParkSpotTLV.Api.Services.Evaluation.Specs;
using ParkSpotTLV.Api.Services.Evaluation.Strategies;


namespace ParkSpotTLV.Api.Composition;
/* ----------------------------------------------------------------------
 * Feature Extensions Methods for the map segments evaluation feature.
 * Initiated by program.cs 
 * ---------------------------------------------------------------------- */

public static class EvaluationExtensions {
    public static IServiceCollection AddEvaluation(this IServiceCollection services) {

        // Query / reading data
        services.AddScoped<ISegmentQueryService, SegmentQueryService>();

        // Strategies
        services.AddScoped<ITariffCalendarService, TariffCalendarService>();
        services.AddScoped<ILegalPolicyService, LegalPolicyService>();
        services.AddScoped<IPaymentDecisionService, PaymentDecisionService>();

        // Budget
        services.AddScoped<IDailyBudgetService, DailyBudgetService>();

        // Availability + Classifications
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IClassificationService, ClassificationService>();

        // Specs
        services.AddSingleton<IRestrictedSpec, RestrictedSpec>();

        // Facade (starter)
        services.AddScoped<IMapSegmentsEvaluator, MapSegmentsEvaluator>();



        return services;
    }
}
