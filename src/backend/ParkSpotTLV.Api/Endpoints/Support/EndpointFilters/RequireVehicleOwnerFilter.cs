using ParkSpotTLV.Infrastructure;
using System.Reflection;

namespace ParkSpotTLV.Api.Endpoints.Support.EndpointFilters {

    /*
     * .RequireVehicleOwnerFilter()
     * Checks if the user ID received is indeed the owner of the vehicle ID received.
     */
    public sealed class RequireVehicleOwnerFilter : IEndpointFilter {

        /*
         * Ensures ownership of the vehicle ID received
         */
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
            var http = context.HttpContext;
            var db = http.RequestServices.GetRequiredService<AppDbContext>();

            // Trys to parse the user ID from the json args received in the request
            if (!TryGetVehicleIdFromArgs(context, out var vehicleId)) {
                vehicleId = TryGetGuidFromRoute(http, "vehicleId")
                            ?? TryGetGuidFromRoute(http, "id")
                            ?? Guid.Empty;
            }

            var problem = await Guards.EnsureVehicleOwnershipAsync(http, db, vehicleId, http.RequestAborted);
            if (problem is not null) return problem;

            http.Items[UserContext.VehicleId] = vehicleId;
            return await next(context);
        }


        /*
         * Trys to read the vehicle ID from arguments received in the endpoint request
         */
        private static bool TryGetVehicleIdFromArgs(EndpointFilterInvocationContext ctx, out Guid vehicleId) {
            foreach (var arg in ctx.Arguments) {
                if (arg is null) continue;
                var prop = arg.GetType().GetProperty("VehicleId",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop?.GetValue(arg) is Guid guid && guid != Guid.Empty) {
                    vehicleId = guid;
                    return true;
                }
            }
            vehicleId = Guid.Empty;
            return false;
        }

        /*
         * Trys to get the vehicle Guid from route variables
         */
        private static Guid? TryGetGuidFromRoute(HttpContext http, string key) {
            if (http.Request.RouteValues.TryGetValue(key, out var raw) &&
                raw is string s && Guid.TryParse(s, out var guid))
                return guid;
            return null;
        }
    }
}
