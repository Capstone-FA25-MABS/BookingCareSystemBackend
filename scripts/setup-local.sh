#!/bin/bash

# BookingCare Local Development Setup Script
# This script sets up the local development environment

set -e

echo "🚀 Setting up BookingCare Microservices Development Environment..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_status() {
    echo -e "${BLUE}📦 $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

# Check if Docker is installed and running
if ! command -v docker &> /dev/null; then
    print_error "Docker is not installed. Please install Docker Desktop first."
    exit 1
fi

if ! docker info &> /dev/null; then
    print_error "Docker is not running. Please start Docker Desktop."
    exit 1
fi

print_success "Docker is installed and running"

# Check if Docker Compose is available
if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    print_error "Docker Compose is not available. Please install Docker Compose."
    exit 1
fi

# Use docker-compose or docker compose based on availability
DOCKER_COMPOSE_CMD="docker-compose"
if docker compose version &> /dev/null; then
    DOCKER_COMPOSE_CMD="docker compose"
fi

print_success "Docker Compose is available"

# Check if .NET 8 SDK is installed
if ! command -v dotnet &> /dev/null; then
    print_error ".NET 8 SDK is not installed. Please install it from https://dotnet.microsoft.com/download"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
if [[ ! $DOTNET_VERSION == 8.* ]]; then
    print_warning "This project is designed for .NET 8. Current version: $DOTNET_VERSION"
fi

print_success ".NET SDK version: $DOTNET_VERSION"

# Create necessary directories
print_status "Creating necessary directories..."
mkdir -p logs
mkdir -p data/sqlserver
mkdir -p data/mongodb
mkdir -p data/redis

# Start infrastructure services
print_status "Starting infrastructure services..."
$DOCKER_COMPOSE_CMD up -d consul rabbitmq redis sqlserver mongodb

# Wait for services to be ready
print_status "Waiting for services to be ready..."
sleep 30

# Check if Consul is ready
print_status "Checking Consul health..."
for i in {1..30}; do
    if curl -f http://localhost:8500/v1/status/leader &> /dev/null; then
        print_success "Consul is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        print_error "Consul failed to start properly"
        exit 1
    fi
    sleep 2
done

# Check if RabbitMQ is ready
print_status "Checking RabbitMQ health..."
for i in {1..30}; do
    if curl -f http://localhost:15672 &> /dev/null; then
        print_success "RabbitMQ is ready"
        break
    fi
    if [ $i -eq 30 ]; then
        print_error "RabbitMQ failed to start properly"
        exit 1
    fi
    sleep 2
done

# Check if SQL Server is ready
print_status "Checking SQL Server health..."
for i in {1..60}; do
    if docker exec $(docker ps -q -f name=sqlserver) /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'BookingCare123!' -Q "SELECT 1" &> /dev/null; then
        print_success "SQL Server is ready"
        break
    fi
    if [ $i -eq 60 ]; then
        print_error "SQL Server failed to start properly"
        exit 1
    fi
    sleep 2
done

# Display service URLs
print_success "🎉 Local development environment is ready!"
echo ""
echo "📍 Service URLs:"
echo "   🔍 Consul UI:     http://localhost:8500"
echo "   🐰 RabbitMQ UI:   http://localhost:15672 (user: bookingcare, pass: password123)"
echo "   🗄️  SQL Server:    localhost:1433 (user: sa, pass: BookingCare123!)"
echo "   🍃 MongoDB:       localhost:27017 (user: bookingcare, pass: password123)"
echo "   🚀 Redis:         localhost:6379"
echo ""
echo "📝 Next steps:"
echo "   1. Run './scripts/build.sh' to build all services"
echo "   2. Start individual services with 'dotnet run' in their respective directories"
echo "   3. Or use 'docker-compose up' to run all services in containers"
echo ""
echo "🔧 Development commands:"
echo "   • Build all: ./scripts/build.sh"
echo "   • Run tests: ./scripts/build.sh --with-tests"
echo "   • Stop services: docker-compose down"
echo "   • View logs: docker-compose logs -f [service-name]"

print_success "Happy coding! 🚀"
