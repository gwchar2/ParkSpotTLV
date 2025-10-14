namespace ParkSpotTLV.Api.Endpoints.Support.EndpointFilters {
    public sealed class RequireUser : IEndpointFilter {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
            var http = context.HttpContext;

            if (!Guards.TryGetUserId(http, out var userId, out var problem))
                return problem;

            http.Items[UserContext.UserIdKey] = userId;
            return await next(context);
        }

    }
}
