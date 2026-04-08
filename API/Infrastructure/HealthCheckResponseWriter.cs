using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.Infrastructure
{
    public static class HealthCheckResponseWriter
    {
        public static Task WriteAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var payload = new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    duration = x.Value.Duration.TotalMilliseconds,
                    error = x.Value.Exception?.Message
                })
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
