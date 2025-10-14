using ParkSpotTLV.Infrastructure;
using System.Reflection;

namespace ParkSpotTLV.Api.Endpoints.Support.EndpointFilters {
    public sealed class RequireVehicleOwnerFilter : IEndpointFilter {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
            var http = context.HttpContext;
            var db = http.RequestServices.GetRequiredService<AppDbContext>();

            // 1) Try from bound args: any argument with a public "VehicleId" Guid property
            if (!TryGetVehicleIdFromArgs(context, out var vehicleId)) {
                // 2) Fallback: route values {vehicleId} or {id}
                vehicleId = TryGetGuidFromRoute(http, "vehicleId")
                            ?? TryGetGuidFromRoute(http, "id")
                            ?? Guid.Empty;
            }

            var problem = await Guards.EnsureVehicleOwnershipAsync(http, db, vehicleId, http.RequestAborted);
            if (problem is not null) return problem;

            http.Items[UserContext.VehicleId] = vehicleId;
            return await next(context);
        }

        private static bool TryGetVehicleIdFromArgs(EndpointFilterInvocationContext ctx, out Guid vehicleId) {
            foreach (var arg in ctx.Arguments) {
                if (arg is null) continue;
                var prop = arg.GetType().GetProperty("VehicleId",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop?.GetValue(arg) is Guid g && g != Guid.Empty) {
                    vehicleId = g;
                    return true;
                }
            }
            vehicleId = Guid.Empty;
            return false;
        }

        private static Guid? TryGetGuidFromRoute(HttpContext http, string key) {
            if (http.Request.RouteValues.TryGetValue(key, out var raw) &&
                raw is string s && Guid.TryParse(s, out var g))
                return g;
            return null;
        }
    }
}
