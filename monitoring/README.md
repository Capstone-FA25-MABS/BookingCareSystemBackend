# BookingCare Monitoring Stack

Complete monitoring solution for BookingCare microservices using Prometheus & Grafana.

## 📋 Overview

This integrated monitoring solution provides:

- **Prometheus**: Time-series metrics collection and storage
- **Grafana**: Advanced visualization and dashboards
- **Node Exporter**: System-level metrics (CPU, memory, disk, network)
- **Redis Exporter**: Cache performance metrics
- **Alert Rules**: Production-ready alerting (18 rules)
- **Custom Dashboards**: Pre-built visualization dashboards

## Quick Start

### 1. Start the Monitoring Stack

```bash
# Start all services including monitoring
docker-compose up -d

# Or start just the monitoring stack
docker-compose up -d prometheus grafana jaeger node-exporter
```

### 2. Access the Dashboards

- **Grafana**: http://localhost:3000 (admin/admin123)
- **Prometheus**: http://localhost:9090
- **Jaeger**: http://localhost:16686

### 3. Configure Services for Monitoring

In any BookingCare service, add monitoring to your `Program.cs`:

```csharp
using BookingCare.Shared.Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// Add monitoring (includes OpenTelemetry, Prometheus, Jaeger)
builder.Services.AddBookingCareMonitoring("BookingCare.Services.Auth", "1.0.0");

// Add global exception handling (already includes monitoring integration)
builder.Services.AddGlobalExceptionHandling();

var app = builder.Build();

// Configure monitoring middleware (must be early in pipeline)
app.UseBookingCareMonitoring();

// Configure global exception handling
app.UseGlobalExceptionHandling();

app.UseRouting();
app.MapControllers();

app.Run();
```

### 4. Environment Variables

Set these environment variables for services:

```bash
# Jaeger configuration
JAEGER_AGENT_HOST=localhost  # or jaeger in Docker
JAEGER_AGENT_PORT=6831

# Service information
SERVICE_NAME=BookingCare.Services.Auth
SERVICE_VERSION=1.0.0
```

## Features

### Automatic Metrics Collection

The monitoring extensions automatically collect:

- **Request metrics**: Count, duration, status codes
- **Error metrics**: Error rates by type
- **System metrics**: CPU, memory, disk usage
- **Database metrics**: Query performance, connection pools
- **gRPC metrics**: Request/response metrics for gRPC services

### Custom Metrics

You can add custom metrics in your services:

```csharp
using BookingCare.Shared.Common.Extensions;

public class DiscountService : BaseService
{
    public async Task<DiscountResponse> CreateDiscountAsync(CreateDiscountRequest request)
    {
        // Start a custom trace
        using var activity = MonitoringExtensions.StartActivity("CreateDiscount");
        activity?.SetTag("discount.code", request.Code);
        activity?.SetTag("discount.type", request.DiscountType.ToString());

        try
        {
            var result = await CreateDiscountInternalAsync(request);
            
            // Record success metric
            MonitoringExtensions.RecordRequest("/api/discounts", "POST", 201);
            
            return result;
        }
        catch (Exception ex)
        {
            // Record error metric
            MonitoringExtensions.RecordError("discount-service", "creation_failed");
            throw;
        }
    }
}
```

### Distributed Tracing

Traces are automatically created for:

- HTTP requests
- Database queries
- gRPC calls
- External HTTP client calls

You can add custom spans:

```csharp
using var activity = MonitoringExtensions.StartActivity("BusinessLogic");
activity?.SetTag("user.id", userId);
activity?.SetTag("operation", "discount-calculation");

// Your business logic here
var discount = CalculateDiscount(amount);

activity?.SetTag("discount.amount", discount.ToString());
```

## Grafana Dashboards

### Pre-configured Dashboards

1. **BookingCare System Overview**: General system health and performance
2. **Service Details**: Per-service metrics and performance
3. **Infrastructure**: System resource usage
4. **Alerts**: Active alerts and their status

### Creating Custom Dashboards

1. Open Grafana at http://localhost:3000
2. Login with admin/admin123
3. Click "+" → Dashboard
4. Add panels with Prometheus queries

Example queries:

```promql
# Request rate by service
sum(rate(bookingcare_requests_total[5m])) by (job)

# Error rate
(sum(rate(bookingcare_requests_total{status_code=~"5.."}[5m])) / sum(rate(bookingcare_requests_total[5m]))) * 100

# 95th percentile response time
histogram_quantile(0.95, sum(rate(bookingcare_request_duration_seconds_bucket[5m])) by (le))

# Memory usage
((node_memory_MemTotal_bytes - node_memory_MemFree_bytes - node_memory_Buffers_bytes - node_memory_Cached_bytes) / node_memory_MemTotal_bytes) * 100
```

## Alerting

### Prometheus Alerts

Alerts are configured in `monitoring/prometheus/alert.rules.yml`:

- High error rate (>5%)
- High response time (>2s)
- Service down
- High CPU usage (>80%)
- High memory usage (>80%)
- Database connection issues
- RabbitMQ queue growing

### Adding Custom Alerts

Edit `monitoring/prometheus/alert.rules.yml`:

```yaml
- alert: HighDiscountCreationFailure
  expr: |
    sum(rate(bookingcare_errors_total{service="discount-service",error_type="creation_failed"}[5m])) > 0.1
  for: 2m
  labels:
    severity: warning
  annotations:
    summary: "High discount creation failure rate"
    description: "Discount creation failure rate is {{ $value }} per second"
```

## Jaeger Tracing

### Viewing Traces

1. Open Jaeger at http://localhost:16686
2. Select a service from the dropdown
3. Click "Find Traces" to view recent traces
4. Click on a trace to see the detailed timeline

### Trace Analysis

Use traces to:

- Identify slow operations
- Debug cross-service calls
- Analyze error propagation
- Understand request flow

## Performance Impact

The monitoring setup has minimal performance impact:

- **CPU overhead**: <2%
- **Memory overhead**: ~50MB per service
- **Network overhead**: Negligible (async metrics collection)

## Troubleshooting

### Services Not Appearing in Prometheus

1. Check service is exposing `/metrics` endpoint
2. Verify Prometheus configuration in `prometheus.yml`
3. Check service discovery and network connectivity

### Missing Traces in Jaeger

1. Verify `JAEGER_AGENT_HOST` and `JAEGER_AGENT_PORT` environment variables
2. Check Jaeger agent is running and accessible
3. Ensure OpenTelemetry is properly configured

### Grafana Dashboard Issues

1. Check Prometheus datasource connection
2. Verify metric names in queries
3. Check time range selection

## Production Considerations

### Security

- Change default Grafana password
- Configure authentication and authorization
- Use HTTPS for external access
- Secure Prometheus and Jaeger interfaces

### Scalability

- Configure Prometheus retention based on needs
- Use Prometheus federation for multiple clusters
- Consider Jaeger storage backends (Elasticsearch, Cassandra)

### Backup

- Backup Grafana dashboards and configuration
- Consider Prometheus data backup strategy

## Integration with CI/CD

Add monitoring checks to your pipeline:

```yaml
# .github/workflows/monitoring-check.yml
name: Monitoring Health Check

on: [push]

jobs:
  monitoring-check:
    runs-on: ubuntu-latest
    steps:
      - name: Check Prometheus targets
        run: |
          curl -f http://prometheus:9090/api/v1/targets || exit 1
      
      - name: Check Grafana health
        run: |
          curl -f http://grafana:3000/api/health || exit 1
```

## Additional Resources

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
