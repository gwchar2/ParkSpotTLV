using Google.Apis.Http;
using ParkSpotTLV.Api.Features.Notifications.Options;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ParkSpotTLV.API.Features.Notifications.Services {
    public sealed class FcmV1Sender(System.Net.Http.IHttpClientFactory http, IOptions<FirebaseOptions> fb) : IFcmV1Sender {
        private static readonly string[] Scopes = ["https://www.googleapis.com/auth/firebase.messaging"];
        private readonly System.Net.Http.IHttpClientFactory _http = http;
        private readonly FirebaseOptions _fb = fb.Value;


        public async Task<FcmSendResult> SendToTokenAsync(
    string deviceToken,
    string title,
    string body,
    IDictionary<string, string>? data,
    CancellationToken ct) {
            try {
                // 1) Load service-account JSON (from Path or Base64)
                byte[] jsonBytes;
                if (_fb.ServiceAccount.Source.Equals("Path", StringComparison.OrdinalIgnoreCase)) {
                    if (string.IsNullOrWhiteSpace(_fb.ServiceAccount.JsonPath))
                        return new FcmSendResult(false, null, "Firebase: ServiceAccount.JsonPath is empty.");

                    jsonBytes = await File.ReadAllBytesAsync(_fb.ServiceAccount.JsonPath!, ct);
                } else {
                    if (string.IsNullOrWhiteSpace(_fb.ServiceAccount.JsonBase64))
                        return new FcmSendResult(false, null, "Firebase: ServiceAccount.JsonBase64 is empty.");

                    jsonBytes = Convert.FromBase64String(_fb.ServiceAccount.JsonBase64!);
                }

                string clientEmail;
                string privateKey;
                using (var doc = JsonDocument.Parse(jsonBytes)) {
                    var root = doc.RootElement;
                    clientEmail = root.GetProperty("client_email").GetString()!;
                    privateKey = root.GetProperty("private_key").GetString()!;
                    if (string.IsNullOrWhiteSpace(clientEmail) || string.IsNullOrWhiteSpace(privateKey))
                        return new FcmSendResult(false, null, "Service-account JSON missing client_email or private_key.");
                }

                // 2) Build a ServiceAccountCredential (non-obsolete) and scope it for FCM
                var initializer = new Google.Apis.Auth.OAuth2.ServiceAccountCredential.Initializer(clientEmail) {
                    Scopes = Scopes
                }.FromPrivateKey(privateKey);

                var sac = new Google.Apis.Auth.OAuth2.ServiceAccountCredential(initializer);

                // 3) Get OAuth2 access token
                var token = await sac.GetAccessTokenForRequestAsync(cancellationToken: ct);
                if (string.IsNullOrWhiteSpace(token))
                    return new FcmSendResult(false, null, "Failed to obtain OAuth access token for FCM.");

                // 4) Prepare HTTP and send
                var client = _http.CreateClient(nameof(FcmV1Sender)); // ensure this is System.Net.Http.IHttpClientFactory
                client.Timeout = TimeSpan.FromSeconds(_fb.HttpTimeoutSeconds);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = $"https://fcm.googleapis.com/v1/projects/{_fb.ProjectId}/messages:send";

                var payload = new {
                    message = new {
                        token = deviceToken,
                        notification = new { title, body },
                        data = data ?? new Dictionary<string, string>()
                    },
                    validate_only = false
                };

                using var resp = await client.PostAsJsonAsync(url, payload, ct);
                var text = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                    return new FcmSendResult(false, null, $"{(int)resp.StatusCode} {resp.ReasonPhrase}: {text}");

                // Response contains: { "name": "projects/.../messages/0:..." }
                using var respDoc = JsonDocument.Parse(text);
                var name = respDoc.RootElement.GetProperty("name").GetString();
                return new FcmSendResult(true, name, null);
            }
            catch (Exception ex) {
                return new FcmSendResult(false, null, ex.Message);
            }
        }

    }
}
