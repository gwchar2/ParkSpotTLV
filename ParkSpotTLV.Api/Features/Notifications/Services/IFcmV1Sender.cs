namespace ParkSpotTLV.API.Features.Notifications.Services {
    public interface IFcmV1Sender {
        Task<FcmSendResult> SendToTokenAsync(string deviceToken, string title, string body, IDictionary<string, string>? data, CancellationToken ct);
    }

    public sealed record FcmSendResult(bool Success, string? ProviderMessageId, string? Error);
}
