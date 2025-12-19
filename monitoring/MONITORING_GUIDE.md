# BookingCare Monitoring Stack

Complete monitoring solution using Prometheus and Grafana for the BookingCare microservices platform.

## Overview

This monitoring stack provides:
- **Prometheus**: Metrics collection and storage
- **Grafana**: Visualization and dashboarding
- **Node Exporter**: Host metrics collection
- **Redis Exporter**: Redis-specific metrics
- **Alert Rules**: Automated alerting based on metrics thresholds

## Architecture

```
┌─────────────────────────────────────────────────┐
│          BookingCare Microservices               │
│  (API Gateway, Auth, User, Doctor, etc.)         │
└──────────────────┬──────────────────────────────┘
                   │ metrics (/metrics endpoint)
                   ▼
┌─────────────────────────────────────────────────┐
│            Prometheus (9090)                     │
│  - Scrapes metrics from all services            │
│  - Stores time-series data                      │
│  - Evaluates alert rules                        │
└──────────────────┬──────────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────────┐
│             Grafana (3000)                       │
│  - Visualizes metrics                           │
│  - Provides dashboards                          │
│  - Alert notifications                          │
└─────────────────────────────────────────────────┘

Infrastructure Monitoring:
  Node Exporter (9100) → Prometheus
  Redis Exporter (9121) → Prometheus
```

## Quick Start

### 1. Starting the Monitoring Stack

```bash
cd /Users/hieumaixuan/Documents/capstone-src/BookingCareSystemBackend

# Start the entire stack including monitoring
docker-compose up -d

# Or start just the monitoring services
docker-compose up -d prometheus grafana node-exporter redis-exporter
```

### 2. Accessing the Services

- **Grafana**: http://localhost:3000
  - Default credentials: `admin` / `admin`
  - Change password on first login

- **Prometheus**: http://localhost:9090
  - Query interface for metrics
  - Alert status and configuration

- **Node Exporter**: http://localhost:9100/metrics
  - Raw host metrics

- **Redis Exporter**: http://localhost:9121/metrics
  - Redis metrics

### 3. Verify Services are Running

```bash
# Check all monitoring containers
docker ps | grep -E "prometheus|grafana|exporter"

# Verify Prometheus targets
curl http://localhost:9090/api/v1/targets

# Test metrics from a service
curl http://localhost:5001/metrics
```

## Configuration

### Prometheus Configuration (`prometheus/prometheus.yml`)

The configuration includes:
- **Scrape Interval**: 15 seconds globally, 10 seconds for microservices
- **Evaluation Interval**: 15 seconds for alert rules
- **Service Discovery**: Static IP-based discovery using Docker container names
- **Metrics Endpoints**: All services expose `/metrics` endpoint on port 6XXX

### Alert Rules (`prometheus/alert.rules.yml`)

Organized into groups:

#### Service Alerts
- `ServiceDown`: Triggers when any BookingCare service is unreachable
- `HighErrorRate`: Alerts when error rate > 5% for 2 minutes
- `HighResponseLatency`: Warns when p95 latency > 2 seconds
- `SlowRequests`: Triggers when p99 latency > 5 seconds

#### Infrastructure Alerts
- `HighMemoryUsage`: Warns at 80% usage
- `CriticalMemoryUsage`: Alerts at 90% usage
- `HighCPUUsage`: Warns at 80% usage
- `CriticalCPUUsage`: Alerts at 90% usage
- `HighDiskUsage`: Warns at 80% usage
- `CriticalDiskUsage`: Alerts at 95% usage

#### Database Alerts
- `RedisDown`: Triggers when Redis is unreachable
- `RedisHighMemory`: Warns when Redis memory > 80%
- `MongoDBDown`: Triggers when MongoDB is unreachable

#### Message Queue Alerts
- `RabbitMQDown`: Triggers when RabbitMQ is unreachable
- `RabbitMQQueueDepthHigh`: Warns when queue depth > 5000 messages
- `RabbitMQConsumerLag`: Triggers when total unprocessed > 10000 messages

## Dashboards

### 1. BookingCare Overview
Main dashboard with system-wide metrics:
- System uptime and availability
- Request rates and latencies
- Error rates and status codes
- Service health status

### 2. Microservices Metrics
Detailed metrics per microservice:
- HTTP request rate
- P95 and P99 latencies
- Error rates (5xx, 4xx)
- Service availability status

### 3. Infrastructure & Database Monitoring
System and database health:
- Memory usage trends
- CPU utilization
- Network I/O
- Redis connected clients
- MongoDB operations per second

## Instrumentation

### ASP.NET Core Services

Add the following to your service for Prometheus metrics:

```csharp
// Program.cs
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add Prometheus metrics
builder.Services.AddSingleton<ICollectorRegistry>(CollectorRegistry.Default);

var app = builder.Build();

// Use Prometheus middleware
app.UseMetricServer();

// Map metrics endpoint
app.MapMetrics();

app.Run();
```

### Metrics Exposed

Standard metrics per service:
- `http_requests_total`: Total HTTP requests
- `http_request_duration_seconds`: Request duration distribution
- `http_requests_in_progress`: Currently processing requests
- Custom business metrics (e.g., appointments booked, payments processed)

## Prometheus Query Examples

### Service Health
```promql
# Service uptime
up{job=~"bookingcare-.*"}

# Available instances
count(up{job=~"bookingcare-.*"} == 1) by (job)
```

### Performance Metrics
```promql
# Request rate (requests per second)
rate(http_requests_total[5m])

# P95 latency
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))

# Error rate percentage
(sum(rate(http_requests_total{status=~"5.."}[5m])) / sum(rate(http_requests_total[5m]))) * 100
```

### Resource Usage
```promql
# Memory usage percentage
(1 - (node_memory_MemAvailable_bytes / node_memory_MemTotal_bytes)) * 100

# CPU usage percentage
(1 - avg(rate(node_cpu_seconds_total{mode="idle"}[5m]))) * 100

# Disk usage percentage
(1 - (node_filesystem_avail_bytes / node_filesystem_size_bytes)) * 100
```

### Queue Metrics
```promql
# RabbitMQ queue depth
rabbitmq_queue_messages_ready

# Redis memory usage
redis_memory_used_bytes / redis_memory_max_bytes

# Unprocessed messages in RabbitMQ
rabbitmq_queue_messages_ready + rabbitmq_queue_messages_unacked
```

## Data Retention

- **Prometheus Storage**: 15 days of data retention
- **Grafana Snapshots**: Stored in Grafana database
- **Volumes**:
  - `prometheus_data`: Time-series database
  - `grafana_data`: Grafana configuration and stored data

## Environment Variables

Set in `.env` file:

```bash
# Grafana
GRAFANA_ADMIN_USER=admin
GRAFANA_ADMIN_PASSWORD=<secure-password>

# Infrastructure credentials (for authenticated metrics endpoints)
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
```

## Troubleshooting

### Prometheus not scraping metrics

1. Check service is running: `docker ps | grep bookingcare_<service>`
2. Verify metrics endpoint: `curl http://localhost:<service-port>/metrics`
3. Check Prometheus targets: http://localhost:9090/targets
4. Review logs: `docker logs bookingcare_prometheus`

### Grafana not showing data

1. Verify Prometheus datasource: Configuration → Datasources
2. Test Prometheus connection: "Test" button in datasource
3. Check dashboard queries: Edit dashboard panels
4. Review Grafana logs: `docker logs bookingcare_grafana`

### High metrics collection CPU usage

1. Reduce scrape frequency in `prometheus.yml`
2. Disable unnecessary exporters
3. Add metric relabeling to filter metrics

### Storage growing too fast

1. Reduce retention time: `--storage.tsdb.retention.time`
2. Enable compression: `--storage.tsdb.wal-compression`
3. Decrease scrape interval for less critical metrics

## Performance Tips

1. **Selective Metrics**: Only collect metrics you need
2. **Scrape Intervals**: Balance between granularity and overhead
   - Critical services: 10-15 seconds
   - Infrastructure: 30-60 seconds
   - Non-critical: 60+ seconds
3. **Alert Thresholds**: Set realistic thresholds to reduce noise
4. **Data Retention**: Balance storage with historical needs

## Backup & Recovery

### Backup Prometheus Data
```bash
docker exec bookingcare_prometheus tar czf /tmp/prometheus-backup.tar.gz /prometheus
docker cp bookingcare_prometheus:/tmp/prometheus-backup.tar.gz ./prometheus-backup.tar.gz
```

### Backup Grafana Configuration
```bash
docker exec bookingcare_grafana tar czf /tmp/grafana-backup.tar.gz /var/lib/grafana
docker cp bookingcare_grafana:/tmp/grafana-backup.tar.gz ./grafana-backup.tar.gz
```

## Integration Points

### With Services
- All microservices must expose `/metrics` endpoint on their HTTP port
- Implement Prometheus client libraries (Prometheus.Client for .NET)
- Export custom business metrics alongside standard HTTP metrics

### With Alert Systems
- Configure AlertManager for email/Slack notifications
- Set up PagerDuty integration for critical alerts
- Implement custom webhook receivers

## Production Considerations

1. **High Availability**
   - Use multiple Prometheus instances with federation
   - Set up Grafana in HA mode with external database
   - Use persistent volumes on robust storage

2. **Security**
   - Enable authentication for Grafana
   - Restrict Prometheus access to internal network
   - Use HTTPS in production
   - Implement network policies

3. **Monitoring the Monitor**
   - Set up alerts for Prometheus itself
   - Monitor Prometheus database size
   - Track query performance

4. **Scaling**
   - Consider Prometheus federation for large deployments
   - Use remote storage backends for long-term retention
   - Implement service discovery for dynamic environments

## References

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Prometheus Client Libraries](https://prometheus.io/docs/instrumenting/clientlibs/)
- [Prometheus Best Practices](https://prometheus.io/docs/practices/)

## Support

For issues or questions:
1. Check logs: `docker logs bookingcare_prometheus`
2. Review metrics endpoint: http://localhost:9090/targets
3. Test individual service metrics: `curl http://service-name:port/metrics`
4. Consult Prometheus/Grafana documentation
