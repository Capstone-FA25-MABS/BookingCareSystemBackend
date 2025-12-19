# BookingCare Monitoring Quick Start
# This file provides a quick reference for starting the monitoring stack

## Starting Monitoring Services

# Start all services including monitoring
```bash
docker-compose up -d
```

# Start only monitoring services
```bash
docker-compose up -d prometheus grafana node-exporter redis-exporter
```

# Rebuild and start monitoring services
```bash
docker-compose up -d --build prometheus grafana
```

## Stopping Services

# Stop all services
```bash
docker-compose down
```

# Stop only monitoring
```bash
docker-compose stop prometheus grafana node-exporter redis-exporter
```

## Viewing Logs

# Prometheus logs
```bash
docker logs -f bookingcare_prometheus
```

# Grafana logs
```bash
docker logs -f bookingcare_grafana
```

# Node Exporter logs
```bash
docker logs -f bookingcare_node_exporter
```

# All monitoring logs
```bash
docker logs -f bookingcare_prometheus bookingcare_grafana
```

## Health Checks

# Check Prometheus health
```bash
curl http://localhost:9090/-/healthy
```

# Check Grafana health
```bash
curl http://localhost:3000/api/health
```

# Check targets in Prometheus
```bash
curl http://localhost:9090/api/v1/targets | jq .
```

## Data Inspection

# Query Prometheus for up services
```bash
curl 'http://localhost:9090/api/v1/query?query=up' | jq .
```

# Get metrics from a service
```bash
curl http://localhost:5001/metrics | head -20
```

# Get Redis metrics
```bash
curl http://localhost:9121/metrics | grep redis_ | head -10
```

## Backup Commands

# Backup Prometheus data
```bash
docker exec bookingcare_prometheus tar czf /tmp/prom-backup.tar.gz /prometheus
docker cp bookingcare_prometheus:/tmp/prom-backup.tar.gz ./
```

# Backup Grafana data
```bash
docker exec bookingcare_grafana tar czf /tmp/grafana-backup.tar.gz /var/lib/grafana
docker cp bookingcare_grafana:/tmp/grafana-backup.tar.gz ./
```

## Cleanup

# Remove all monitoring data
```bash
docker volume rm bookingcare-integration_prometheus_data bookingcare-integration_grafana_data
```

# Reset a specific service
```bash
docker-compose rm -f prometheus
docker volume rm bookingcare-integration_prometheus_data
docker-compose up -d prometheus
```

## Common Troubleshooting

# Verify service connectivity
```bash
docker network ls | grep bookingcare
docker network inspect bookingcare-integration_bookingcare-network
```

# Check specific service metrics availability
```bash
docker exec bookingcare_prometheus wget -O - http://auth-service:6003/metrics 2>/dev/null | head
```

# Restart metrics collection
```bash
docker restart bookingcare_prometheus
```

# Force reload Grafana dashboards
```bash
docker restart bookingcare_grafana
```
