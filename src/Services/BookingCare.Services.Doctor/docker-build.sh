#!/bin/bash

# Docker build and run script for Doctor Service
# This script provides easy commands for Docker operations

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SERVICE_NAME="doctor-service"
IMAGE_NAME="bookingcare/${SERVICE_NAME}"
VERSION=${1:-latest}
DOCKER_CONTEXT="../../../"  # Root of the solution

# Function to print colored output
print_header() {
    echo -e "${BLUE}=== $1 ===${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

# Function to check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        print_error "Docker is not running. Please start Docker and try again."
        exit 1
    fi
}

# Function to build the Docker image
build_image() {
    print_header "Building Docker Image"
    
    print_info "Building ${IMAGE_NAME}:${VERSION}..."
    print_info "Context: ${DOCKER_CONTEXT}"
    
    docker build \
        -t "${IMAGE_NAME}:${VERSION}" \
        -t "${IMAGE_NAME}:latest" \
        -f Dockerfile \
        "${DOCKER_CONTEXT}"
    
    print_success "Image built successfully: ${IMAGE_NAME}:${VERSION}"
}

# Function to run the container standalone
run_container() {
    print_header "Running Container Standalone"
    
    # Stop existing container if running
    if docker ps -q -f name="${SERVICE_NAME}" | grep -q .; then
        print_info "Stopping existing container..."
        docker stop "${SERVICE_NAME}" || true
        docker rm "${SERVICE_NAME}" || true
    fi
    
    print_info "Starting ${SERVICE_NAME} container..."
    
    docker run -d \
        --name "${SERVICE_NAME}" \
        -p 6008:6008 \
        -p 6018:6018 \
        -e ASPNETCORE_ENVIRONMENT=Development \
        -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1435;Database=MABS_Doctor;User Id=sa;Password=Doctor123!;TrustServerCertificate=true;" \
        "${IMAGE_NAME}:${VERSION}"
    
    print_success "Container started successfully"
    print_info "HTTP API: http://localhost:6008"
    print_info "gRPC: http://localhost:6018"
    print_info "Health Check: http://localhost:6008/health"
}

# Function to run with Docker Compose
run_compose() {
    print_header "Running with Docker Compose"
    
    if [ "$1" = "prod" ]; then
        print_info "Starting in production mode..."
        docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
    else
        print_info "Starting in development mode..."
        docker-compose up -d
    fi
    
    print_success "Services started with Docker Compose"
    print_info "HTTP API: http://localhost:6008"
    print_info "gRPC: http://localhost:6018"
    print_info "Database: localhost:1435"
}

# Function to stop services
stop_services() {
    print_header "Stopping Services"
    
    # Stop Docker Compose services
    if docker-compose ps -q 2>/dev/null | grep -q .; then
        print_info "Stopping Docker Compose services..."
        docker-compose down
    fi
    
    # Stop standalone container
    if docker ps -q -f name="${SERVICE_NAME}" | grep -q .; then
        print_info "Stopping standalone container..."
        docker stop "${SERVICE_NAME}"
        docker rm "${SERVICE_NAME}"
    fi
    
    print_success "All services stopped"
}

# Function to show logs
show_logs() {
    print_header "Service Logs"
    
    if docker-compose ps -q 2>/dev/null | grep -q .; then
        print_info "Showing Docker Compose logs..."
        docker-compose logs -f
    elif docker ps -q -f name="${SERVICE_NAME}" | grep -q .; then
        print_info "Showing container logs..."
        docker logs -f "${SERVICE_NAME}"
    else
        print_error "No running services found"
    fi
}

# Function to run database migrations
run_migrations() {
    print_header "Running Database Migrations"
    
    # Check if service is running
    if docker ps -q -f name="${SERVICE_NAME}" | grep -q .; then
        print_info "Running migrations in existing container..."
        docker exec "${SERVICE_NAME}" dotnet ef database update
    else
        print_info "Starting temporary container for migrations..."
        docker run --rm \
            -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1435;Database=MABS_Doctor;User Id=sa;Password=Doctor123!;TrustServerCertificate=true;" \
            "${IMAGE_NAME}:${VERSION}" \
            dotnet ef database update
    fi
    
    print_success "Migrations completed"
}

# Function to clean up Docker resources
cleanup() {
    print_header "Cleaning Up Docker Resources"
    
    # Stop services
    stop_services
    
    # Remove images
    print_info "Removing Docker images..."
    docker rmi "${IMAGE_NAME}:${VERSION}" 2>/dev/null || true
    docker rmi "${IMAGE_NAME}:latest" 2>/dev/null || true
    
    # Remove volumes (ask for confirmation)
    read -p "Remove database volumes? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker volume rm doctor_db_data 2>/dev/null || true
        docker volume rm doctor_db_prod_data 2>/dev/null || true
        print_success "Volumes removed"
    fi
    
    print_success "Cleanup completed"
}

# Function to show service status
show_status() {
    print_header "Service Status"
    
    print_info "Docker Compose Services:"
    if docker-compose ps 2>/dev/null; then
        echo
    else
        echo "No Docker Compose services running"
    fi
    
    print_info "Standalone Containers:"
    if docker ps --filter "name=${SERVICE_NAME}" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -q "${SERVICE_NAME}"; then
        docker ps --filter "name=${SERVICE_NAME}" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    else
        echo "No standalone containers running"
    fi
    
    print_info "Images:"
    docker images --filter "reference=${IMAGE_NAME}" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedSince}}"
}

# Function to test the service
test_service() {
    print_header "Testing Service"
    
    print_info "Waiting for service to be ready..."
    sleep 10
    
    # Test health endpoint
    if curl -f -s "http://localhost:6008/health" > /dev/null; then
        print_success "Health check passed"
        
        # Show health response
        print_info "Health response:"
        curl -s "http://localhost:6008/health" | jq '.' 2>/dev/null || curl -s "http://localhost:6008/health"
    else
        print_error "Health check failed"
        print_info "Service logs:"
        show_logs
        return 1
    fi
}

# Main script logic
case "${2:-help}" in
    build)
        check_docker
        build_image
        ;;
    run)
        check_docker
        build_image
        run_container
        test_service
        ;;
    compose)
        check_docker
        build_image
        run_compose "${3:-dev}"
        test_service
        ;;
    stop)
        check_docker
        stop_services
        ;;
    logs)
        check_docker
        show_logs
        ;;
    status)
        check_docker
        show_status
        ;;
    migrate)
        check_docker
        run_migrations
        ;;
    test)
        check_docker
        test_service
        ;;
    clean)
        check_docker
        cleanup
        ;;
    help|*)
        echo "Docker Management Script for Doctor Service"
        echo
        echo "Usage: $0 [version] [command] [options]"
        echo
        echo "Commands:"
        echo "  build          Build Docker image"
        echo "  run            Build and run container standalone"
        echo "  compose [mode] Build and run with Docker Compose (dev|prod)"
        echo "  stop           Stop all services"
        echo "  logs           Show service logs"
        echo "  status         Show service status"
        echo "  migrate        Run database migrations"
        echo "  test           Test service health"
        echo "  clean          Clean up Docker resources"
        echo "  help           Show this help message"
        echo
        echo "Examples:"
        echo "  $0 latest build                 # Build latest version"
        echo "  $0 v1.0.0 run                   # Build and run v1.0.0"
        echo "  $0 latest compose dev           # Run with Docker Compose (development)"
        echo "  $0 latest compose prod          # Run with Docker Compose (production)"
        echo "  $0 latest stop                  # Stop all services"
        echo "  $0 latest logs                  # Show logs"
        echo "  $0 latest clean                 # Clean up everything"
        ;;
esac
