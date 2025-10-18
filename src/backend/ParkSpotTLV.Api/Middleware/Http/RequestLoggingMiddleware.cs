using System.Diagnostics;
using Microsoft.Extensions.Primitives;
using Serilog;


namespace ParkSpotTLV.Api.Middleware.Http {

    /*
     * Logs every request’s metadata (start + completed) safely (no bodies, redacted headers).
    */
    public sealed class RequestLoggingMiddleware(RequestDelegate next) {
        private readonly RequestDelegate _next = next;                                         // Function pointer that requests the next middleware

        /*
         * HttpContext ctx which represents everything about the current request and response.
         * Returns a completed log with startus + timing
        */
        public async Task Invoke(HttpContext ctx) {

            var sw = Stopwatch.StartNew();
            var req = ctx.Request;
            var res = ctx.Response;

            var userId = ctx.User?.FindFirst("sub")?.Value
                ?? ctx.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? "anonymous";
            var method = req.Method;
            var path = req.Path.Value ?? "/";
            var scheme = req.Scheme;
            var host = req.Host.Value;
            var protocol = req.Protocol;
            var query = req.QueryString.HasValue ? req.QueryString.Value : "";
            var ua = req.Headers.TryGetValue("User-Agent", out StringValues s) ? (string?)s.ToString() : null;
            var clientIp = ctx.Connection.RemoteIpAddress?.ToString();

            long? requestSize = req.ContentLength;
            long? responseSize = null;

            var requestHeaders = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
                ["Content-Type"] = req.Headers.TryGetValue("Content-Type", out var ct) ? ct.ToString() : null,
                ["Accept"] = req.Headers.TryGetValue("Accept", out var acc) ? acc.ToString() : null,
                ["User-Agent"] = ua
            };

            /* Request started logging */
            Log.ForContext("event", "http_request_started")
                .ForContext("userId", userId)
                .ForContext("method", method)
                .ForContext("path", path)
                .ForContext("scheme", scheme)
                .ForContext("host", host)
                .ForContext("protocol", protocol)
                .ForContext("query", query)
                .ForContext("requestSize", requestSize)
                .ForContext("userAgent", ua)
                .ForContext("clientIP", clientIp)
                .Information("HTTP {Method} {Path} started", method, path);

            try {
                await _next(ctx);
            }
            finally {
                /* Request ended logging */
                sw.Stop();

                responseSize = res.ContentLength;

                var status = res.StatusCode;
                var durationMs = sw.Elapsed.TotalMilliseconds;

                Log.ForContext("event", "http_request_completed")
                    .ForContext("userId", userId)
                    .ForContext("method", method)
                    .ForContext("path", path)
                    .ForContext("status", status)
                    .ForContext("durationMs", durationMs)
                    .ForContext("requestSize", requestSize)
                    .ForContext("responseSize", responseSize)
                    .ForContext("userAgent", ua)
                    .ForContext("clientIP", clientIp)
                    .Information("HTTP {Method} {Path} responded {Status} in {Elapsed:0.###} ms",
                                method, path, status, durationMs);
            }
        }
    }
}
