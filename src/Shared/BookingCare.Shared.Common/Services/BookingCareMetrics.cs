using Prometheus;
using BookingCare.Shared.Common.Interfaces;

namespace BookingCare.Shared.Common.Services;

/// <summary>
/// Implementation of BookingCare metrics using Prometheus
/// </summary>
public class BookingCareMetrics : IBookingCareMetrics
{
    private readonly string _serviceName;
    
    // Common Counters
    private readonly Counter _operationCounter;
    private readonly Counter _errorCounter;
    
    // Common Histograms
    private readonly Histogram _operationDuration;
    private readonly Histogram _databaseDuration;
    private readonly Histogram _grpcDuration;
    private readonly Histogram _externalApiDuration;
    
    // Common Gauges
    private readonly Gauge _activeItemsGauge;
    
    // Business Metrics
    private readonly Counter _businessCounter;
    private readonly Gauge _businessGauge;

    public BookingCareMetrics(string serviceName)
    {
        _serviceName = serviceName;

        // Initialize counters
        _operationCounter = Metrics
            .CreateCounter(
                "bookingcare_operations_total",
                "Total operations performed",
                new[] { "service", "operation", "result" });

        _errorCounter = Metrics
            .CreateCounter(
                "bookingcare_errors_total", 
                "Total errors occurred",
                new[] { "service", "error_type", "operation" });

        // Initialize histograms  
        _operationDuration = Metrics
            .CreateHistogram(
                "bookingcare_operation_duration_seconds",
                "Duration of operations in seconds",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "service", "operation" },
                    Buckets = Histogram.LinearBuckets(0.001, 0.05, 20)
                });

        _databaseDuration = Metrics
            .CreateHistogram(
                "bookingcare_database_duration_ms",
                "Database operation duration in milliseconds",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "service", "operation", "table" },
                    Buckets = new double[] { 1, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000 }
                });

        _grpcDuration = Metrics
            .CreateHistogram(
                "bookingcare_grpc_duration_ms",
                "gRPC operation duration in milliseconds", 
                new HistogramConfiguration
                {
                    LabelNames = new[] { "service", "method", "result" },
                    Buckets = new double[] { 1, 5, 10, 25, 50, 100, 250, 500, 1000 }
                });

        _externalApiDuration = Metrics
            .CreateHistogram(
                "bookingcare_external_api_duration_ms",
                "External API call duration in milliseconds",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "service", "api", "endpoint", "status_code" },
                    Buckets = new double[] { 10, 50, 100, 250, 500, 1000, 2500, 5000, 10000 }
                });

        // Initialize gauges
        _activeItemsGauge = Metrics
            .CreateGauge(
                "bookingcare_active_items",
                "Current number of active items",
                new[] { "service", "item_type" });

        // Business metrics
        _businessCounter = Metrics
            .CreateCounter(
                "bookingcare_business_events_total",
                "Business events counter",
                new[] { "service", "event_type", "category" });

        _businessGauge = Metrics
            .CreateGauge(
                "bookingcare_business_metrics",
                "Business metrics gauge",
                new[] { "service", "metric_name", "category" });
    }

    public void IncrementOperation(string operation, string result = "success")
    {
        _operationCounter
            .WithLabels(_serviceName, operation, result)
            .Inc();
    }

    public void RecordDuration(string operation, double durationSeconds)
    {
        _operationDuration
            .WithLabels(_serviceName, operation)
            .Observe(durationSeconds);
    }

    public void SetActiveItems(string itemType, double count)
    {
        _activeItemsGauge
            .WithLabels(_serviceName, itemType)
            .Set(count);
    }

    public void IncrementError(string errorType, string operation = "")
    {
        _errorCounter
            .WithLabels(_serviceName, errorType, operation)
            .Inc();
    }

    public void RecordDatabaseOperation(string operation, string table, double durationMs)
    {
        _databaseDuration
            .WithLabels(_serviceName, operation, table)
            .Observe(durationMs);
    }

    public void RecordGrpcOperation(string method, string result, double durationMs)
    {
        _grpcDuration
            .WithLabels(_serviceName, method, result)
            .Observe(durationMs);
    }

    public void RecordExternalApiCall(string api, string endpoint, string statusCode, double durationMs)
    {
        _externalApiDuration
            .WithLabels(_serviceName, api, endpoint, statusCode)
            .Observe(durationMs);
    }

    public void RecordBusinessMetric(string metricName, double value, params (string key, string value)[] labels)
    {
        // Default labels
        var defaultLabels = new List<string> { _serviceName, metricName, "default" };
        
        // Add custom labels if provided
        if (labels?.Length > 0)
        {
            var category = labels.FirstOrDefault(l => l.key == "category").value ?? "default";
            defaultLabels[2] = category;
        }

        _businessGauge
            .WithLabels(defaultLabels.ToArray())
            .Set(value);
    }

    /// <summary>
    /// Record appointment-specific metrics
    /// </summary>
    public void RecordAppointmentMetric(string action, string status = "success")
    {
        _businessCounter
            .WithLabels(_serviceName, "appointment", action)
            .Inc();
    }

    /// <summary>
    /// Record authentication-specific metrics  
    /// </summary>
    public void RecordAuthMetric(string action, string result = "success")
    {
        _businessCounter
            .WithLabels(_serviceName, "authentication", action)
            .Inc();
    }

    /// <summary>
    /// Record user-specific metrics
    /// </summary>
    public void RecordUserMetric(string action, string category = "default")
    {
        _businessCounter
            .WithLabels(_serviceName, "user", action)
            .Inc();
    }

    /// <summary>
    /// Record payment-specific metrics
    /// </summary>
    public void RecordPaymentMetric(string action, string status, double amount = 0)
    {
        _businessCounter
            .WithLabels(_serviceName, "payment", action)
            .Inc();
            
        if (amount > 0)
        {
            _businessGauge
                .WithLabels(_serviceName, "payment_amount", status)
                .Set(amount);
        }
    }
}