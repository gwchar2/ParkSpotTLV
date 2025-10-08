
using ParkSpotTLV.API.Features.Notifications.Services;

namespace ParkSpotTLV.Api.Endpoints {
    public static class NotificationsTestEndpoint {
        public static IEndpointRouteBuilder MapNotificationsTest(this IEndpointRouteBuilder app) {
            // Guard this in prod if needed (e.g., Only Development / or require Admin)
            app.MapPost("/push/test", async (IFcmV1Sender fcm, TestPushRequest req, CancellationToken ct) => {
                if (string.IsNullOrWhiteSpace(req.Token))
                    return Results.BadRequest("Device token is required.");

                var result = await fcm.SendToTokenAsync(
                    req.Token, 
                    req.Title ?? "Test push", 
                    req.Body ?? "Hello from ParkSpotTLV", 
                    req.Data, 
                    ct
                    );

                if (!result.Success) return Results.Problem(result.Error);

                return Results.Ok(new { messageId = result.ProviderMessageId });
            }).WithTags("Notification Testing");

            return app;
        }

        public sealed record TestPushRequest(string Token, string? Title, string? Body, Dictionary<string, string>? Data);
    }
}
