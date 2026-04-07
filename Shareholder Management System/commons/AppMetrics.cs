using Prometheus;

public static class AppMetrics
{


    public static readonly Counter HttpRequests =
        Metrics.CreateCounter(
            "sms_http_requests_total",
            "Total HTTP requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "method", "endpoint", "status" }
            });

    public static readonly Histogram HttpRequestDuration =
        Metrics.CreateHistogram(
            "sms_http_request_duration_seconds",
            "HTTP request duration",
            new HistogramConfiguration
            {
                LabelNames = new[] { "endpoint" }
            });


}