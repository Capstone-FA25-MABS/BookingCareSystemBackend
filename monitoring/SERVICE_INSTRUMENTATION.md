# BookingCare Service Instrumentation Guide

This guide explains how to instrument your BookingCare microservices to expose Prometheus metrics.

## Prerequisites

- .NET 6.0 or higher
- Services running in Docker with Prometheus monitoring stack

## Installation

### Step 1: Add NuGet Package

Add the Prometheus.Client package to your service project:

```bash
dotnet add package prometheus-client
```

### Step 2: Configure in Program.cs

```csharp
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Add Prometheus metrics
builder.Services.AddSingleton<ICollectorRegistry>(CollectorRegistry.Default);

var app = builder.Build();

// Configure middleware
app.UseRouting();

// Add Prometheus middleware - collects standard HTTP metrics
app.UseHttpMetrics();

// Map endpoints
app.MapControllers();

// Map metrics endpoint (required by Prometheus scraper)
app.MapMetrics();

app.Run();
```

## Basic Metrics

### 1. HTTP Metrics (Automatic)

These are automatically collected:

```promql
# Request rate
rate(http_requests_total[5m])

# Request duration
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# In-flight requests
http_requests_in_progress
```

### 2. Default Metrics (Automatic)

- `dotnet_build_info`: Build information
- `dotnet_collection_*`: Garbage collection metrics
- `process_*`: Process-level metrics (memory, CPU)

## Custom Metrics

### Counter Example

Count total appointments created:

```csharp
using Prometheus;

namespace BookingCare.Services.Appointment.Metrics
{
    public static class AppointmentMetrics
    {
        private static readonly Counter AppointmentsCreated = Counter
            .Create(
                "bookingcare_appointments_created_total",
                "Total number of appointments created",
                new CounterConfiguration { LabelNames = new[] { "status" } }
            );

        private static readonly Counter AppointmentsCancelled = Counter
            .Create(
                "bookingcare_appointments_cancelled_total",
                "Total number of appointments cancelled",
                new CounterConfiguration { LabelNames = new[] { "reason" } }
            );

        public static void RecordAppointmentCreated(string status)
        {
            AppointmentsCreated.WithLabels(status).Inc();
        }

        public static void RecordAppointmentCancelled(string reason)
        {
            AppointmentsCancelled.WithLabels(reason).Inc();
        }
    }
}
```

Usage in your service:

```csharp
public class AppointmentService
{
    public async Task<Appointment> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        try
        {
            var appointment = new Appointment { /* ... */ };
            await _repository.SaveAsync(appointment);
            
            // Record metric
            AppointmentMetrics.RecordAppointmentCreated("created");
            
            return appointment;
        }
        catch (Exception ex)
        {
            AppointmentMetrics.RecordAppointmentCreated("failed");
            throw;
        }
    }
}
```

### Gauge Example

Track active user sessions:

```csharp
namespace BookingCare.Services.Auth.Metrics
{
    public static class AuthMetrics
    {
        private static readonly Gauge ActiveSessions = Gauge
            .Create(
                "bookingcare_active_sessions",
                "Number of active user sessions",
                new GaugeConfiguration { LabelNames = new[] { "user_type" } }
            );

        public static void SetActiveSessions(string userType, int count)
        {
            ActiveSessions.WithLabels(userType).Set(count);
        }

        public static void IncActiveSessions(string userType)
        {
            ActiveSessions.WithLabels(userType).Inc();
        }

        public static void DecActiveSessions(string userType)
        {
            ActiveSessions.WithLabels(userType).Dec();
        }
    }
}
```

### Histogram Example

Track database query durations:

```csharp
namespace BookingCare.Services.Doctor.Metrics
{
    public static class DatabaseMetrics
    {
        private static readonly Histogram QueryDuration = Histogram
            .Create(
                "bookingcare_database_query_duration_seconds",
                "Database query duration in seconds",
                new HistogramConfiguration
                {
                    LabelNames = new[] { "query_type", "database" },
                    Buckets = new[] { 0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1, 5 }
                }
            );

        public static IDisposable TrackQueryDuration(string queryType, string database)
        {
            return QueryDuration.WithLabels(queryType, database).NewTimer();
        }
    }
}
```

Usage in your repository:

```csharp
public class DoctorRepository
{
    private readonly DbContext _context;

    public async Task<List<Doctor>> GetDoctorsBySpecialtyAsync(string specialty)
    {
        using (DatabaseMetrics.TrackQueryDuration("select", "doctor"))
        {
            return await _context.Doctors
                .Where(d => d.Specialty == specialty)
                .ToListAsync();
        }
    }
}
```

### Summary Example

Track API response sizes:

```csharp
namespace BookingCare.Services.Hospital.Metrics
{
    public static class ApiMetrics
    {
        private static readonly Summary ResponseSize = Summary
            .Create(
                "bookingcare_response_size_bytes",
                "API response size in bytes",
                new SummaryConfiguration
                {
                    LabelNames = new[] { "endpoint", "method" },
                    Objectives = new[] { 0.5, 0.9, 0.99 }
                }
            );

        public static void RecordResponseSize(string endpoint, string method, long sizeBytes)
        {
            ResponseSize.WithLabels(endpoint, method).Observe(sizeBytes);
        }
    }
}
```

## Business Metrics Examples

### Payment Service
```csharp
private static readonly Counter PaymentsProcessed = Counter
    .Create(
        "bookingcare_payments_processed_total",
        "Total payments processed",
        new CounterConfiguration { LabelNames = new[] { "status", "method" } }
    );

private static readonly Gauge TotalRevenueUsd = Gauge
    .Create(
        "bookingcare_revenue_usd",
        "Total revenue in USD",
        new GaugeConfiguration { LabelNames = new[] { "period" } }
    );

// Record successful payment
PaymentsProcessed.WithLabels("success", "credit_card").Inc();
TotalRevenueUsd.WithLabels("daily").Add(transactionAmount);
```

### User Service
```csharp
private static readonly Counter UserRegistrations = Counter
    .Create(
        "bookingcare_user_registrations_total",
        "Total user registrations",
        new CounterConfiguration { LabelNames = new[] { "user_type" } }
    );

private static readonly Gauge TotalUsers = Gauge
    .Create(
        "bookingcare_total_users",
        "Total registered users",
        new GaugeConfiguration { LabelNames = new[] { "user_type" } }
    );

// Record registration
UserRegistrations.WithLabels("patient").Inc();
TotalUsers.WithLabels("patient").Inc();
```

### Appointment Service
```csharp
private static readonly Histogram AppointmentWaitTime = Histogram
    .Create(
        "bookingcare_appointment_wait_minutes",
        "Appointment wait time in minutes",
        new HistogramConfiguration
        {
            LabelNames = new[] { "doctor_specialty" },
            Buckets = new[] { 5, 15, 30, 60, 120, 240 }
        }
    );

// Record wait time
var waitMinutes = (appointment.ScheduledTime - appointment.BookedTime).TotalMinutes;
AppointmentWaitTime.WithLabels(doctor.Specialty).Observe(waitMinutes);
```

## Integration with Middleware

### Error Tracking Middleware

```csharp
public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Counter ExceptionCount = Counter
        .Create(
            "bookingcare_exceptions_total",
            "Total exceptions",
            new CounterConfiguration { LabelNames = new[] { "exception_type" } }
        );

    public MetricsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            ExceptionCount.WithLabels(ex.GetType().Name).Inc();
            throw;
        }
    }
}

// In Program.cs:
app.UseMiddleware<MetricsMiddleware>();
```

## Metrics Endpoint Configuration

By default, metrics are available at `/metrics`. Customize if needed:

```csharp
// Custom port (not recommended - use same port as service)
app.MapMetrics("/prometheus/metrics");

// With authentication
app.MapMetrics("/metrics").RequireAuthorization();
```

## Labels Best Practices

### DO:
- Use predefined label values
- Keep label values low cardinality
- Use meaningful names

```csharp
// Good
Counter.Create("bookingcare_requests", 
    labelNames: new[] { "status" }); // Limited values: success, failed

Counter.Create("bookingcare_appointments",
    labelNames: new[] { "doctor_specialty" }); // Limited distinct values
```

### DON'T:
- Use dynamic/unbounded values as labels
- Create high cardinality metrics

```csharp
// BAD - Don't use user IDs as labels
Counter.Create("bookingcare_requests",
    labelNames: new[] { "user_id" }); // Millions of distinct values!

// BAD - Don't use timestamps as labels
Counter.Create("bookingcare_events",
    labelNames: new[] { "timestamp" }); // Unbounded!
```

## Testing Metrics

### Manual Testing

```bash
# Get metrics from a service
curl http://localhost:6003/metrics | grep bookingcare_

# Filter specific metric
curl http://localhost:6003/metrics | grep bookingcare_appointments

# Search in Prometheus
# Visit http://localhost:9090
# Query: bookingcare_appointments_created_total
```

### Unit Testing

```csharp
[Fact]
public void AppointmentMetrics_RecordsCreation()
{
    // Arrange
    var registry = CollectorRegistry.Default;
    
    // Act
    AppointmentMetrics.RecordAppointmentCreated("created");
    
    // Assert
    var metric = registry.Collect()
        .FirstOrDefault(m => m.Name == "bookingcare_appointments_created_total");
    
    Assert.NotNull(metric);
    Assert.Equal(1, metric.Samples.FirstOrDefault()?.Value);
}
```

## Common Patterns

### Request-Scoped Metrics

```csharp
public class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Histogram RequestDuration = Histogram
        .Create(
            "bookingcare_http_request_duration_seconds",
            "HTTP request duration",
            new HistogramConfiguration { LabelNames = new[] { "method", "endpoint" } }
        );

    public async Task InvokeAsync(HttpContext context)
    {
        using (RequestDuration
            .WithLabels(context.Request.Method, context.Request.Path)
            .NewTimer())
        {
            await _next(context);
        }
    }
}
```

### Dependency Injection

```csharp
public interface IMetricsService
{
    void RecordAppointmentCreated(string status);
    void RecordPaymentProcessed(string method, bool success);
}

public class MetricsService : IMetricsService
{
    public void RecordAppointmentCreated(string status) 
        => AppointmentMetrics.RecordAppointmentCreated(status);
    
    public void RecordPaymentProcessed(string method, bool success)
        => PaymentMetrics.RecordPaymentProcessed(method, success);
}

// In Program.cs
builder.Services.AddSingleton<IMetricsService, MetricsService>();
```

## Verification in Grafana

1. Open Grafana: http://localhost:3000
2. Go to Explore → Select Prometheus
3. Query your metrics:
   ```promql
   bookingcare_appointments_created_total
   ```
4. Create dashboards for visualization

## Troubleshooting

### Metrics not appearing

1. **Service not returning metrics**
   ```bash
   curl http://localhost:6003/metrics
   ```

2. **Prometheus not scraping**
   - Check: http://localhost:9090/targets
   - Verify service is reachable from Prometheus container

3. **Metric name doesn't match**
   - Prometheus converts metric names to lowercase
   - `MyMetric` becomes `my_metric`

### High cardinality warnings

If you see "high cardinality" errors:
1. Review label values
2. Remove unbounded labels
3. Pre-aggregate high cardinality data

## Performance Considerations

- **Minimal Overhead**: Prometheus metrics have < 1% performance impact
- **Memory**: Registers metrics at startup, not per-request
- **Network**: Metrics only sent when scraped by Prometheus

## References

- [Prometheus Client (.NET)](https://github.com/prometheus-net/prometheus-client_net)
- [Metric Naming Best Practices](https://prometheus.io/docs/practices/naming/)
- [Writing Exporters](https://prometheus.io/docs/instrumenting/writing_exporters/)
