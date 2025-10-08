# Docker Deployment Guide for BookingCare System

This guide explains how to build and deploy all microservices in the BookingCare system using Docker and Docker Compose.

## 📋 Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Architecture](#architecture)
- [Quick Start](#quick-start)
- [Service Configuration](#service-configuration)
- [Advanced Usage](#advanced-usage)
- [Troubleshooting](#troubleshooting)

## 🎯 Overview

The BookingCare system consists of 18 microservices, multiple databases, monitoring tools, and a message queue infrastructure. All services are containerized using Docker with optimized multi-stage builds.

## 📦 Prerequisites

- Docker Engine 20.10+ 
- Docker Compose 2.0+
- Minimum 8GB RAM
- 20GB free disk space

## 🏗️ Architecture

### Infrastructure Services
- **RabbitMQ**: Message broker for inter-service communication
- **Redis**: Caching and session storage
- **MongoDB**: NoSQL database for Content, Reviews, Notifications, Favorites, and ServiceMedical
- **SQL Server**: Relational database for Auth, User, Doctor, Hospital, Appointment, Payment, Schedule, Discount, Saga, Communication, AI, and Analytics services

### Monitoring Stack
- **Prometheus**: Metrics collection
- **Grafana**: Metrics visualization
- **Jaeger**: Distributed tracing
- **Node Exporter**: System metrics

### Microservices
- **API Gateway** (Port 5000): Ocelot-based API gateway
- **Auth Service** (Port 5001): Authentication and authorization
- **User Service** (Port 5002): User management
- **Doctor Service** (Port 5003): Doctor profiles and management
- **Hospital Service** (Port 5004): Hospital information
- **Appointment Service** (Port 5005): Booking management
- **Payment Service** (Port 5006): Payment processing
- **Review Service** (Port 5007): Reviews and ratings
- **Content Service** (Port 5008): CMS functionality
- **Discount Service** (Ports 5009, 5109): Promotion management
- **Notification Service** (Port 5011): Push notifications and alerts
- **Schedule Service** (Port 5012): Doctor scheduling
- **Saga Service** (Port 5013): Distributed transaction coordination
- **Communication Service** (Port 5014): Messaging and communications
- **AI Service** (Port 5015): AI-powered features
- **Analytics Service** (Port 5016): Data analytics
- **Favorites Service** (Port 5017): User favorites
- **ServiceMedical Service** (Port 5018): Medical services catalog

## 🚀 Quick Start

### 1. Clone the Repository
```bash
git clone <repository-url>
cd BookingCareSystemBackend
```

### 2. Start All Services
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Check service status
docker-compose ps
```

### 3. Stop All Services
```bash
# Stop all services
docker-compose down

# Stop and remove volumes (⚠️ This will delete all data)
docker-compose down -v
```

## ⚙️ Service Configuration

### Environment Variables

Each service can be configured through environment variables in `docker-compose.yml`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
  - ASPNETCORE_URLS=http://+:8080
  - ConnectionStrings__DefaultConnection=Server=...
  - RabbitMQ__HostName=rabbitmq
  - RabbitMQ__Port=5672
```

### Database Connections

#### SQL Server Databases
- **Auth DB**: Port 1431, Password: `Auth@1234!`
- **User DB**: Port 1444, Password: `User@1234!`
- **Doctor DB**: Port 1447, Password: `Doctor@1234!`
- **Hospital DB**: Port 1450, Password: `Hospital@1234!`
- **Appointment DB**: Port 1448, Password: `Appointment@1234!`
- **Payment DB**: Port 1449, Password: `Payment@1234!`
- **Schedule DB**: Port 1446, Password: `Schedule@1234!`
- **Discount DB**: Port 1434, Password: `Discount123!`
- **Saga DB**: Port 1400, Password: `Saga@1234`
- **Communication DB**: Port 1451, Password: `Communication@1234!`
- **AI DB**: Port 1452, Password: `AI@1234!`
- **Analytics DB**: Port 1453, Password: `Analytics@1234!`

#### MongoDB
- **Port**: 27017
- **Username**: bookingcare
- **Password**: password123
- **Used by**: Review, Content, Notification, Favorites, ServiceMedical services

### Monitoring Access

- **Grafana**: http://localhost:3000 (admin/admin123)
- **Prometheus**: http://localhost:9090
- **Jaeger UI**: http://localhost:16686
- **RabbitMQ Management**: http://localhost:15672 (bookingcare/bookingcare@1234)

## 🔧 Advanced Usage

### Build Specific Service
```bash
docker-compose build <service-name>
```

### Rebuild and Restart Service
```bash
docker-compose up -d --build <service-name>
```

### Scale a Service
```bash
docker-compose up -d --scale user-service=3
```

### View Service Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f user-service

# Last 100 lines
docker-compose logs --tail=100 user-service
```

### Execute Commands in Container
```bash
docker-compose exec user-service bash
```

### Check Resource Usage
```bash
docker stats
```

## 🛠️ Dockerfile Optimizations

All service Dockerfiles include the following optimizations:

### 1. Multi-Stage Builds
- Separate build and runtime stages
- Smaller final images (Alpine Linux base)
- Better layer caching

### 2. Security Features
- Non-root user execution
- Minimal base images
- No unnecessary packages

### 3. Performance
- Layer caching for dependencies
- Optimized build order
- Health checks for reliability

### 4. Example Dockerfile Structure
```dockerfile
# Stage 1: Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
# Copy and restore dependencies first (cached)
# Then copy source and build

# Stage 3: Publish
FROM build AS publish
# Publish with optimizations

# Stage 4: Final
FROM base AS final
# Copy artifacts and set up security
```

## 🐛 Troubleshooting

### Services Not Starting
```bash
# Check logs
docker-compose logs <service-name>

# Check if ports are available
netstat -an | grep LISTEN

# Restart specific service
docker-compose restart <service-name>
```

### Database Connection Issues
```bash
# Check if database is healthy
docker-compose ps

# Check database logs
docker-compose logs sqlserver-<service>

# Restart database
docker-compose restart sqlserver-<service>
```

### Out of Memory
```bash
# Check memory usage
docker stats

# Increase Docker memory limit in Docker Desktop settings
# Or add to docker-compose.yml:
services:
  service-name:
    deploy:
      resources:
        limits:
          memory: 512M
```

### Rebuild Everything from Scratch
```bash
# Stop all containers
docker-compose down -v

# Remove all images
docker-compose rm -f

# Rebuild without cache
docker-compose build --no-cache

# Start fresh
docker-compose up -d
```

### Network Issues
```bash
# Recreate network
docker-compose down
docker network prune
docker-compose up -d
```

## 📊 Health Checks

All services include health check endpoints:
- **HTTP Services**: `GET /health`
- **gRPC Services**: Standard gRPC health check protocol

Monitor health status:
```bash
docker-compose ps
```

## 🔒 Security Best Practices

1. **Change Default Passwords**: Update all passwords in `docker-compose.yml` for production
2. **Use Secrets**: Consider using Docker secrets for sensitive data
3. **Network Isolation**: Services communicate only through defined networks
4. **Regular Updates**: Keep base images and dependencies updated
5. **Scan Images**: Use `docker scan` to check for vulnerabilities

## 📝 Development vs Production

### Development (Current Configuration)
- Services expose ports on host
- Verbose logging enabled
- Debug mode active
- Hot reload not configured (requires volume mounts)

### Production Recommendations
- Use environment-specific configurations
- Enable HTTPS/TLS
- Configure proper logging aggregation
- Implement secret management
- Set up load balancing
- Configure auto-scaling
- Implement backup strategies
- Use production-grade monitoring

## 🤝 Contributing

When adding new services:
1. Create optimized Dockerfile following the existing pattern
2. Add service to `docker-compose.yml`
3. Update this documentation
4. Test locally before committing

## 📞 Support

For issues and questions:
- Check logs: `docker-compose logs`
- Review documentation
- Contact the development team

## 📚 Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Docker Best Practices](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/docker-application-development-process/docker-app-development-workflow)
- [Alpine Linux](https://alpinelinux.org/)

---

**Last Updated**: October 2025
