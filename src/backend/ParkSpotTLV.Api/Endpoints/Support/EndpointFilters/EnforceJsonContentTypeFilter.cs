namespace ParkSpotTLV.Api.Endpoints.Support.EndpointFilters {

    public sealed class EnforceJsonContentTypeFilter : IEndpointFilter {

        /*
         * Enforces that the supported request is in JSON format only.
         */
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
            var req = context.HttpContext.Request;
            if (req.Method is "POST" or "PATCH" or "PUT") {
                if (string.IsNullOrWhiteSpace(req.ContentType) || !req.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)) {
                    var pd = Results.Problem(
                        title: "Unsupported content type", 
                        detail: "Use Content-Type: application/json", 
                        statusCode: StatusCodes.Status415UnsupportedMediaType
                        );
                    return await ValueTask.FromResult(pd);
                }
            }
            return await next(context);
        }
    }

}
