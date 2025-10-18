

namespace ParkSpotTLV.Api.Endpoints.Support.EndpointFilters {


    /*
     * Endpoint filter callers.
     */
    public static class EndpointFilterExtensions {
        public static RouteHandlerBuilder RequireUser(this RouteHandlerBuilder builder)
            => builder.AddEndpointFilter<RequireUser>();
        public static RouteHandlerBuilder EnforceJsonContent(this RouteHandlerBuilder b)
           => b.AddEndpointFilter<EnforceJsonContentTypeFilter>();
        public static RouteHandlerBuilder RequireRowVersion(this RouteHandlerBuilder b)
            => b.AddEndpointFilter(new RequireRowVersionFilter());
        public static RouteHandlerBuilder RequireVehicleOwner(this RouteHandlerBuilder b)
            => b.AddEndpointFilter(new RequireVehicleOwnerFilter());
        public static RouteGroupBuilder RequireUser(this RouteGroupBuilder group)
            => group.AddEndpointFilter<RequireUser>();

    }
}


