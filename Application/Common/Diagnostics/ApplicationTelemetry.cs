using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Application.Common.Diagnostics
{
    public static class ApplicationTelemetry
    {
        public static readonly ActivitySource ActivitySource = new("LanguageLearning.API");
        public static readonly Meter Meter = new("LanguageLearning.API");
        public static readonly Counter<long> AuthEvents = Meter.CreateCounter<long>("auth.events");
        public static readonly Histogram<double> RequestDurationMs = Meter.CreateHistogram<double>("http.server.request.duration", unit: "ms");
    }
}
