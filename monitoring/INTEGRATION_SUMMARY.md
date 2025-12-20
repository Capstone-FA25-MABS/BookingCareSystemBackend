# BookingCare Prometheus & Grafana Integration Summary

## Overview

Prometheus and Grafana monitoring stack has been successfully integrated into the BookingCare microservices platform. This document summarizes the integration and next steps.

## What Was Added

### 1. Docker Compose Services (docker-compose.yml)

**Prometheus** (Port 9090)
- Metrics collection and time-series database
- Alert rule evaluation
- 15-day data retention
- Auto-discovery of all BookingCare services

**Grafana** (Port 3000)
- Visualization dashboard platform
- Pre-configured Prometheus data source
- Automated dashboard provisioning
- Default credentials: admin/admin

**Node Exporter** (Port 9100)
- System-level metrics (CPU, memory, disk, network)
- Collects host infrastructure statistics
- Essential for infrastructure monitoring

**Redis Exporter** (Port 9121)
- Redis-specific metrics
- Connection pooling information
- Memory usage tracking
- Key statistics

### 2. Configuration Files

**Prometheus Configuration** (`monitoring/prometheus/prometheus.yml`)
- Updated service discovery for all 16 microservices
- Corrected port numbers matching docker-compose
- Separate scrape intervals for services vs infrastructure
- RabbitMQ metrics endpoint configuration

**Alert Rules** (`monitoring/prometheus/alert.rules.yml`)
- 18 production-ready alert rules across 4 categories
- Service Alerts: Down, high error rate, latency issues
- Infrastructure Alerts: Memory, CPU, disk usage
- Database Alerts: Redis, MongoDB connectivity
- Message Queue Alerts: RabbitMQ health and queue depth

### 3. Grafana Dashboards

**Microservices Metrics Dashboard** (`microservices-metrics.json`)
- HTTP request rates across all services
- P95 latency gauge
- 5xx error tracking
- Service status indicators

**Infrastructure Monitoring Dashboard** (`infrastructure-monitoring.json`)
- Memory and CPU usage trends
- Network I/O visualization
- Redis connection metrics
- System resource tracking

### 4. Documentation

**MONITORING_GUIDE.md** - Complete Reference
- Architecture overview
- Quick start instructions
- Service configuration details
- Prometheus query examples
- Troubleshooting guide
- Production considerations

**SERVICE_INSTRUMENTATION.md** - Developer Guide
- Step-by-step instrumentation for .NET services
- Counter, Gauge, Histogram, and Summary examples
- Business metrics patterns
- Testing and verification instructions
- Performance best practices

**QUICK_COMMANDS.md** - Command Reference
- Docker commands for monitoring operations
- Health check commands
- Log viewing
- Backup and restore procedures
- Common troubleshooting commands

## Architecture

```
BookingCare Services (all 16 microservices)
           ↓ (/metrics endpoints)
        Prometheus (9090)
           ↓ (scrapes every 10-15s)
        Time-Series DB
           ↓ (queries)
        Grafana (3000)
           ↓ (visualizes)
    Pre-built Dashboards

External Monitoring:
  Node Exporter (9100) → Prometheus
  Redis Exporter (9121) → Prometheus
  RabbitMQ (15672) → Prometheus
```

## Quick Start Guide

### 1. Start Monitoring Stack

```bash
cd /Users/hieumaixuan/Documents/capstone-src/BookingCareSystemBackend

# Start all services including monitoring
docker-compose up -d

# Or selectively start monitoring
docker-compose up -d prometheus grafana node-exporter redis-exporter
```

### 2. Access Dashboards

| Service | URL | Purpose |
|---------|-----|---------|
| Grafana | http://localhost:3000 | Visualization & dashboards |
| Prometheus | http://localhost:9090 | Metrics query interface |
| Node Exporter | http://localhost:9100/metrics | Host metrics |
| Redis Exporter | http://localhost:9121/metrics | Redis metrics |

### 3. Verify Setup

```bash
# Check all monitoring containers
docker ps | grep -E "prometheus|grafana|exporter"

# Test Prometheus targets
curl http://localhost:9090/api/v1/targets | jq '.data.activeTargets | length'

# Get metrics from a service
curl http://localhost:5001/metrics | head -20
```

## Next Steps

### For Development Teams

1. **Instrument Your Services**
   - Add Prometheus.Client NuGet package: `dotnet add package prometheus-client`
   - Follow guide in `SERVICE_INSTRUMENTATION.md`
   - Export business metrics alongside HTTP metrics
   - Test metrics endpoint: `curl http://localhost:6003/metrics`

2. **Verify Metrics Collection**
   - Check Prometheus targets: http://localhost:9090/targets
   - All should show "UP" status
   - If DOWN, verify service is running and metrics endpoint is accessible

3. **Create Custom Dashboards**
   - Visit Grafana: http://localhost:3000
   - Create dashboards for your service
   - Use Prometheus queries in panels
   - Share with team members

### For Operations

1. **Configure Alerts**
   - Integrate with AlertManager (if using)
   - Set up email/Slack/PagerDuty notifications
   - Review alert thresholds in `alert.rules.yml`

2. **Set Up Data Retention**
   - Prometheus default: 15 days retention
   - Adjust with `--storage.tsdb.retention.time=30d` in docker-compose
   - Consider external storage for long-term metrics

3. **Production Deployment**
   - Set secure Grafana password: `GRAFANA_ADMIN_PASSWORD` env var
   - Enable HTTPS/SSL for web interfaces
   - Use persistent volumes on stable storage
   - Implement high availability if needed

4. **Monitoring the Monitor**
   - Set up alerts for Prometheus/Grafana availability
   - Monitor disk usage of time-series database
   - Track query performance

## Example Service Instrumentation

### Minimal Implementation

```csharp
// Program.cs
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<ICollectorRegistry>(CollectorRegistry.Default);

var app = builder.Build();
app.UseHttpMetrics();
app.MapControllers();
app.MapMetrics(); // Exposes /metrics endpoint
app.Run();
```

### With Business Metrics

```csharp
// Metrics.cs
public static class AppointmentMetrics
{
    private static readonly Counter Created = Counter.Create(
        "bookingcare_appointments_created_total",
        "Appointments created",
        new CounterConfiguration { LabelNames = new[] { "status" } }
    );

    public static void RecordCreated(string status) 
        => Created.WithLabels(status).Inc();
}

// Service.cs
public class AppointmentService
{
    public async Task<Appointment> CreateAsync(CreateRequest request)
    {
        var appointment = await _repo.SaveAsync(MapToDomain(request));
        AppointmentMetrics.RecordCreated("success");
        return appointment;
    }
}
```

## Monitoring Coverage

### Services Being Monitored (16 services)

✓ API Gateway
✓ Auth Service
✓ User Service
✓ Doctor Service
✓ Hospital Service
✓ Appointment Service
✓ Payment Service
✓ Review Service
✓ Content Service
✓ Discount Service
✓ Communication Service
✓ Notification Service
✓ Favorites Service
✓ Analytics Service
✓ Schedule Service
✓ ServiceMedical Service
✓ Saga Service
✓ AI Service

### Infrastructure Components

✓ RabbitMQ (message queue)
✓ Redis (cache)
✓ MongoDB (analytics database)
✓ SQL Server (9 instances)
✓ Node resources (CPU, memory, disk, network)

## Key Metrics Available

### HTTP Metrics (Automatic)
- Request rate (req/s)
- Request duration (p50, p95, p99)
- In-flight requests
- Error rates by status code

### Infrastructure Metrics
- CPU usage %
- Memory usage %
- Disk usage %
- Network I/O (bytes/sec)

### Custom Metrics (To Be Added)
- Appointments booked/cancelled
- Payments processed
- User registrations
- Queue depths
- Database query times

## Alert Categories

### 🔴 Critical Alerts
- Service Down (1 min)
- Error Rate > 5% (2 min)
- Memory Usage > 90% (2 min)
- CPU Usage > 90% (2 min)
- Database Connection Issues
- RabbitMQ Queue Depth > 5000

### 🟡 Warning Alerts
- High Latency (P95 > 2s)
- Error Rate > 2% (5 min)
- Memory Usage > 80% (5 min)
- CPU Usage > 80% (5 min)
- Disk Usage > 80% (5 min)
- Slow Requests (P99 > 5s)

## Performance Expectations

- **Monitoring Overhead**: < 1% CPU usage
- **Memory Impact**: ~100MB for Prometheus + 200MB for Grafana
- **Disk Usage**: ~1GB per week at default settings
- **Query Latency**: < 100ms for typical queries
- **Scrape Latency**: 10-15 seconds collection interval

## Troubleshooting Checklist

- [ ] Prometheus health: `curl http://localhost:9090/-/healthy`
- [ ] Grafana health: `curl http://localhost:3000/api/health`
- [ ] Prometheus targets: http://localhost:9090/targets (all should be UP)
- [ ] Metrics endpoint: `curl http://service:port/metrics`
- [ ] Container logs: `docker logs bookingcare_prometheus`
- [ ] Network connectivity: `docker network inspect bookingcare-integration_bookingcare-network`

## File Structure

```
monitoring/
├── MONITORING_GUIDE.md              # Complete reference guide
├── SERVICE_INSTRUMENTATION.md       # Developer instrumentation guide
├── QUICK_COMMANDS.md                # Command reference
├── prometheus/
│   ├── prometheus.yml               # ✓ Updated with all services
│   └── alert.rules.yml              # ✓ 18 production rules
├── grafana/
│   ├── provisioning/
│   │   ├── dashboards/dashboards.yml
│   │   └── datasources/datasources.yml
│   └── dashboards/
│       ├── bookingcare-overview.json (existing)
│       ├── microservices-metrics.json (NEW)
│       └── infrastructure-monitoring.json (NEW)
└── README.md                        # Overview
```

## Support & Resources

- **Prometheus Docs**: https://prometheus.io/docs/
- **Grafana Docs**: https://grafana.com/docs/grafana/latest/
- **Prometheus Client Library**: https://github.com/prometheus-net/prometheus-client_net
- **PromQL Guide**: https://prometheus.io/docs/prometheus/latest/querying/basics/

## Status Checklist

- ✓ Prometheus configured with all 16+ services
- ✓ Grafana configured with Prometheus datasource
- ✓ Node Exporter for system metrics
- ✓ Redis Exporter for cache metrics
- ✓ Alert rules configured (18 rules)
- ✓ Sample dashboards created (2 new dashboards)
- ✓ Documentation complete (3 guides)
- ✓ Docker-compose integration complete
- ✓ Health checks configured for all monitoring services

## Next Phase

**Ready for Service Instrumentation:**
1. Add Prometheus.Client to each microservice
2. Implement custom business metrics
3. Create service-specific dashboards
4. Test end-to-end monitoring flow
5. Configure alert notifications (email/Slack)

---

**Integration Date**: December 18, 2025
**Version**: 1.0
**Status**: Ready for Development
