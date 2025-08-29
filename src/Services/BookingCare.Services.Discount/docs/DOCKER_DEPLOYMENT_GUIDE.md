# Docker Deployment Guide for Discount Service

This guide provides comprehensive instructions for deploying the BookingCare Discount Service using Docker.

## 📋 Overview

The Discount Service is containerized with Docker and can be deployed in multiple ways:
- **Standalone Container**: Single service with external database
- **Docker Compose**: Service + SQL Server database
- **Production Deployment**: Optimized for production environments

## 🗂️ Docker Files

| File | Purpose |
|------|---------|
| `Dockerfile` | Multi-stage build configuration |
| `docker-compose.yml` | Development deployment with database |
| `docker-compose.prod.yml` | Production overrides |
| `.dockerignore` | Files to exclude from build context |
| `docker-build.sh` | Build and deployment automation script |
| `.env.template` | Environment variables template |

## 🚀 Quick Start

### 1. Prerequisites

```bash
# Install Docker and Docker Compose
docker --version
docker-compose --version

# Clone the repository
git clone <repository-url>
cd BookingCareSystemBackend/src/Services/BookingCare.Services.Discount
```

### 2. Environment Setup

```bash
# Copy environment template
cp .env.template .env

# Edit configuration (optional)
nano .env
```

### 3. Build and Run

```bash
# Option 1: Use the automated script (Recommended)
./docker-build.sh latest compose dev

# Option 2: Manual Docker Compose
docker-compose up -d

# Option 3: Build and run manually
docker build -t discount-service ../../../
docker run -p 6007:6007 -p 6017:6017 discount-service
```

### 4. Verify Deployment

```bash
# Check service health
curl http://localhost:6007/api/discounts/health

# View logs
docker-compose logs -f discount-service

# Check status
./docker-build.sh latest status
```

## 🐳 Docker Commands

### Building Images

```bash
# Build development image
docker build -t bookingcare/discount-service:latest -f Dockerfile ../../../

# Build production image
docker build -t bookingcare/discount-service:prod -f Dockerfile --target final ../../../

# Build with specific version
docker build -t bookingcare/discount-service:v1.0.0 -f Dockerfile ../../../
```

### Running Containers

```bash
# Run standalone container
docker run -d \
  --name discount-service \
  -p 6007:6007 \
  -p 6017:6017 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  bookingcare/discount-service:latest

# Run with database connection
docker run -d \
  --name discount-service \
  -p 6007:6007 \
  -p 6017:6017 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1434;Database=MABS_Discount;User Id=sa;Password=Discount123!;TrustServerCertificate=true;" \
  bookingcare/discount-service:latest
```

### Container Management

```bash
# Start services
docker-compose up -d

# Stop services
docker-compose down

# Restart services
docker-compose restart

# View logs
docker-compose logs -f

# Scale service (if needed)
docker-compose up -d --scale discount-service=2
```

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Development` |
| `ASPNETCORE_URLS` | Listening URLs | `http://+:6007;http://+:6017` |
| `ConnectionStrings__DefaultConnection` | Database connection | See appsettings.json |
| `Logging__LogLevel__Default` | Log level | `Information` |

### Port Configuration

| Port | Protocol | Purpose |
|------|----------|---------|
| `6007` | HTTP | REST API endpoints |
| `6017` | HTTP/2 | gRPC services |
| `1434` | TCP | SQL Server database |

### Volume Mounts

```yaml
volumes:
  - ./logs:/app/logs              # Application logs
  - ./data:/app/data              # Application data
  - discount_db_data:/var/opt/mssql  # Database data
```

## 🏭 Production Deployment

### 1. Production Build

```bash
# Build production image
./docker-build.sh v1.0.0 build

# Deploy with production overrides
./docker-build.sh v1.0.0 compose prod
```

### 2. Production Configuration

```bash
# Set environment variables
export DISCOUNT_DB_PASSWORD="YourStrongProductionPassword123!"
export ASPNETCORE_ENVIRONMENT="Production"

# Run production stack
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### 3. Security Considerations

```bash
# Use secrets for sensitive data
docker secret create db_password password.txt

# Enable HTTPS in production
# Uncomment UseHttps() in Program.cs
# Add SSL certificates

# Set strong passwords
# Update connection strings
# Configure firewall rules
```

## 🔍 Monitoring and Health Checks

### Health Check Endpoint

```bash
# Internal health check (container)
curl http://localhost:6007/api/discounts/health

# External health check (load balancer)
curl http://your-domain.com/api/discounts/health
```

### Docker Health Checks

```bash
# Check container health
docker ps --filter "name=discount-service"

# View health check logs
docker inspect discount-service | jq '.[0].State.Health'

# Manual health check
docker exec discount-service curl -f http://localhost:6007/api/discounts/health
```

### Logging

```bash
# View application logs
docker-compose logs discount-service

# Follow logs in real-time
docker-compose logs -f discount-service

# View database logs
docker-compose logs discount-db

# Export logs
docker-compose logs --no-color discount-service > discount-service.log
```

## 🛠️ Database Management

### Database Initialization

```bash
# Run migrations in container
docker exec discount-service dotnet ef database update

# Run migrations with script
./docker-build.sh latest migrate

# Initialize with seed data
docker exec discount-service dotnet run --seed-data
```

### Database Backup

```bash
# Backup database
docker exec discount-db sqlcmd -S localhost -U sa -P "Discount123!" \
  -Q "BACKUP DATABASE [MABS_Discount] TO DISK = '/var/opt/mssql/backup/discount.bak'"

# Copy backup file
docker cp discount-db:/var/opt/mssql/backup/discount.bak ./backup/
```

### Database Restore

```bash
# Copy backup to container
docker cp ./backup/discount.bak discount-db:/var/opt/mssql/backup/

# Restore database
docker exec discount-db sqlcmd -S localhost -U sa -P "Discount123!" \
  -Q "RESTORE DATABASE [MABS_Discount] FROM DISK = '/var/opt/mssql/backup/discount.bak'"
```

## 🧪 Testing

### API Testing

```bash
# Run API tests against containerized service
cd docs
./test-discount-api.sh

# Test specific endpoints
curl -X GET http://localhost:6007/api/discounts/health
curl -X GET http://localhost:6007/api/discounts?pageSize=5
```

### Load Testing

```bash
# Install Apache Bench
apt-get install apache2-utils

# Run load tests
ab -n 1000 -c 10 http://localhost:6007/api/discounts/health

# Or use Artillery
npm install -g artillery
artillery quick --count 10 --num 100 http://localhost:6007/api/discounts/health
```

## 🐛 Troubleshooting

### Common Issues

#### Service Won't Start

```bash
# Check logs
docker-compose logs discount-service

# Check database connectivity
docker exec discount-service dotnet ef database update --verbose

# Verify ports
netstat -tulpn | grep :6007
```

#### Database Connection Issues

```bash
# Check database container
docker-compose logs discount-db

# Test database connection
docker exec discount-db sqlcmd -S localhost -U sa -P "Discount123!" -Q "SELECT @@VERSION"

# Check network connectivity
docker exec discount-service ping discount-db
```

#### Performance Issues

```bash
# Check resource usage
docker stats

# Check container limits
docker inspect discount-service | jq '.[0].HostConfig.Memory'

# Monitor logs for errors
docker-compose logs -f discount-service | grep -i error
```

### Debugging

```bash
# Run container in interactive mode
docker run -it --rm \
  -p 6007:6007 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  bookingcare/discount-service:latest \
  /bin/bash

# Attach to running container
docker exec -it discount-service /bin/bash

# Check application files
docker exec discount-service ls -la /app/
```

## 📊 Automation Script Usage

The `docker-build.sh` script provides comprehensive automation:

```bash
# Build image
./docker-build.sh latest build

# Run standalone container
./docker-build.sh latest run

# Run with Docker Compose (development)
./docker-build.sh latest compose dev

# Run with Docker Compose (production)
./docker-build.sh latest compose prod

# Show service status
./docker-build.sh latest status

# View logs
./docker-build.sh latest logs

# Run database migrations
./docker-build.sh latest migrate

# Test service health
./docker-build.sh latest test

# Stop all services
./docker-build.sh latest stop

# Clean up everything
./docker-build.sh latest clean

# Show help
./docker-build.sh latest help
```

## 🔐 Security Best Practices

1. **Use Strong Passwords**: Change default database passwords
2. **Enable HTTPS**: Configure SSL certificates for production
3. **Limit Network Access**: Use Docker networks and firewalls
4. **Regular Updates**: Keep base images and dependencies updated
5. **Secrets Management**: Use Docker secrets or external vaults
6. **Resource Limits**: Set memory and CPU limits
7. **Log Security**: Avoid logging sensitive information

## 📝 Maintenance

### Regular Tasks

```bash
# Update base images
docker pull mcr.microsoft.com/dotnet/aspnet:8.0
docker pull mcr.microsoft.com/mssql/server:2022-latest

# Rebuild images
./docker-build.sh latest build

# Update containers
docker-compose pull
docker-compose up -d

# Clean up unused resources
docker system prune -f
docker volume prune -f
```

### Backup Strategy

```bash
# Automated backup script
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
docker exec discount-db sqlcmd -S localhost -U sa -P "Discount123!" \
  -Q "BACKUP DATABASE [MABS_Discount] TO DISK = '/var/opt/mssql/backup/discount_${DATE}.bak'"
docker cp discount-db:/var/opt/mssql/backup/discount_${DATE}.bak ./backups/
```

---

**Need Help?** 🤔
- Check the troubleshooting section
- Review Docker and service logs
- Test with the automation script
- Verify network and database connectivity
