using ParkSpotTLV.Api.Middleware;
using ParkSpotTLV.Api.Middleware.Http;

namespace ParkSpotTLV.Api.Composition {


    /* 
    * MIDDLEWARE PIPELINE
    */
    public static class AppPipelineExtensions {
        public static IApplicationBuilder UseAppPipeline(this IApplicationBuilder app) {
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseGlobalProblemDetails();                          // problem+json for errors
            app.UseMiddleware<TracingMiddleware>();                 // W3C trace + response headers
            app.UseMiddleware<RequestLoggingMiddleware>();          // request logs (no bodies)

            return app;
        }
    }
}