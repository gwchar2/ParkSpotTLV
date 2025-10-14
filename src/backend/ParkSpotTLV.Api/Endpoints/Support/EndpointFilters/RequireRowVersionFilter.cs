
using ParkSpotTLV.Api.Endpoints.Support.Errors;

namespace ParkSpotTLV.Api.Endpoints.Support.EndpointFilters {
    public sealed class RequireRowVersionFilter() : IEndpointFilter {

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
            var http = context.HttpContext;

            if (Guards.TryGetExpectedXmin(context, out var xmin)) {
                http.Items[UserContext.ExpectedXmin] = xmin;
                return await next(context);
            }

            return GeneralErrors.InvalidorMissingRowVersion(http);
        }

    }
}
