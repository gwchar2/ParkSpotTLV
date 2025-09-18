using Serilog.Context;
using System.Diagnostics;
using System.Security.Claims;

namespace ParkSpotTLV.Api.Http {
    public sealed class TracingMiddleware {

        private static readonly ActivitySource ActivitySource = new("Backend.Api");
        private readonly RequestDelegate _next;

        public TracingMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx) {

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            var req = ctx.Request;

            var traceparent = req.Headers["traceparent"].ToString();
            var tracestate = req.Headers["tracestate"].ToString();

            ActivityContext parentContext = default;
            bool hasParent = false;

            if (!string.IsNullOrWhiteSpace(traceparent))
                hasParent = ActivityContext.TryParse(traceparent, tracestate, out parentContext);

            /* Creating a new Activity */
            var activity = Activity.Current;

            /* Just add key-value pairs */
            if (activity is not null) {
                activity.SetTag("http.method", req.Method);
                activity.SetTag("http.scheme", req.Scheme);
                activity.SetTag("http.host", req.Host.Value);
                activity.SetTag("http.target", req.Path.Value);
                if (req.QueryString.HasValue) activity.SetTag("http.query", req.QueryString.Value);
            }

            /* We add extra fields to all serilog logs. Every log in this request should include traceId, spanId, parentId.*/
            using (LogContext.PushProperty("traceId", activity?.TraceId.ToString()))
            using (LogContext.PushProperty("spanId", activity?.SpanId.ToString()))
            using (LogContext.PushProperty("parentId", activity?.ParentSpanId.ToString()))
            using (LogContext.PushProperty("userId", ctx.User?.FindFirst("sub")?.Value
                    ?? ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "anonymous")) {
                ctx.Response.OnStarting(() =>
                {
                    var id = activity?.Id; // W3C traceparent
                    if (!string.IsNullOrEmpty(id))
                        ctx.Response.Headers["traceparent"] = id;

                    var state = activity?.TraceStateString;
                    if (!string.IsNullOrEmpty(state))
                        ctx.Response.Headers["tracestate"] = state;

                    return Task.CompletedTask;
                });

                /* Call the next middleware */
                await _next(ctx);
            }
            
        }

    }
}
