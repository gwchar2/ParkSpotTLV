

namespace ParkSpotTLV.Api.Endpoints.Support;

public static class EndpointFilterExtensions {
    // Per-endpoint
    public static RouteHandlerBuilder RequireUser(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter<RequireUser>();

    // Per-group
    public static RouteGroupBuilder RequireUser(this RouteGroupBuilder group)
        => group.AddEndpointFilter<RequireUser>();


}
// Runs for a group or an endpoint, ensures a valid user and stashes UserId
public sealed class RequireUser : IEndpointFilter {
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
        var ctx = context.HttpContext;

        if (!Guards.TryGetUserId(ctx, out var userId, out var problem))
            return problem; 

        ctx.Items[UserContext.UserIdKey] = userId;
        return await next(context);
    }
}
