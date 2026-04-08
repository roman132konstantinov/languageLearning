using System.Diagnostics;
using Application.Common.Diagnostics;

namespace API.Middleware
{
    public class RequestTelemetryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTelemetryMiddleware> _logger;

        public RequestTelemetryMiddleware(RequestDelegate next, ILogger<RequestTelemetryMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startedAt = Stopwatch.GetTimestamp();
            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["TraceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
                ["RequestPath"] = context.Request.Path.Value
            });

            await _next(context);

            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            ApplicationTelemetry.RequestDurationMs.Record(
                elapsedMs,
                KeyValuePair.Create<string, object?>("method", context.Request.Method),
                KeyValuePair.Create<string, object?>("route", context.Request.Path.Value),
                KeyValuePair.Create<string, object?>("status_code", context.Response.StatusCode));
        }
    }
}
