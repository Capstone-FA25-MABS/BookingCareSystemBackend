#!/bin/bash

# Build and Push All BookingCare Services to Docker Hub
# This script builds all microservices for linux/amd64 platform and pushes them to Docker Hub
# Usage: ./build-and-push-all-services.sh [version]

set -e

# Configuration
DOCKER_USERNAME="${DOCKER_USERNAME:-hiumx}"
VERSION="${1:-latest}"
PLATFORM="linux/amd64"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "=================================="
echo "BookingCare Services Build & Push"
echo "=================================="
echo "Docker Username: $DOCKER_USERNAME"
echo "Version: $VERSION"
echo "Platform: $PLATFORM"
echo "Root Directory: $ROOT_DIR"
echo "=================================="
echo ""

# Function to build and push a service
build_and_push() {
    local service_name=$1
    local dockerfile_path=$2
    local image_name="${DOCKER_USERNAME}/bookingcare-${service_name}:${VERSION}"
    
    echo "----------------------------------------"
    echo "Building: $service_name"
    echo "Image: $image_name"
    echo "----------------------------------------"
    
    docker buildx build \
        --platform "$PLATFORM" \
        -f "$dockerfile_path" \
        -t "$image_name" \
        --push \
        "$ROOT_DIR"
    
    if [ $? -eq 0 ]; then
        echo "✅ Successfully built and pushed: $image_name"
    else
        echo "❌ Failed to build: $service_name"
        exit 1
    fi
    echo ""
}

# Start timing
start_time=$(date +%s)

# Build and push API Gateway
build_and_push "api-gateway" "src/ApiGateway/BookingCare.ApiGateway.Ocelot/Dockerfile"

# Build and push all microservices
build_and_push "ai-service" "src/Services/BookingCare.Services.AI/Dockerfile"
build_and_push "analytics-service" "src/Services/BookingCare.Services.Analytics/Dockerfile"
build_and_push "appointment-service" "src/Services/BookingCare.Services.Appointment/Dockerfile"
build_and_push "auth-service" "src/Services/BookingCare.Services.Auth/Dockerfile"
build_and_push "communication-service" "src/Services/BookingCare.Services.Communication/Dockerfile"
build_and_push "content-service" "src/Services/BookingCare.Services.Content/Dockerfile"
build_and_push "discount-service" "src/Services/BookingCare.Services.Discount/Dockerfile"
build_and_push "doctor-service" "src/Services/BookingCare.Services.Doctor/Dockerfile"
build_and_push "favorites-service" "src/Services/BookingCare.Services.Favorites/Dockerfile"
build_and_push "hospital-service" "src/Services/BookingCare.Services.Hospital/Dockerfile"
build_and_push "notification-service" "src/Services/BookingCare.Services.Notification/Dockerfile"
build_and_push "payment-service" "src/Services/BookingCare.Services.Payment/Dockerfile"
build_and_push "review-service" "src/Services/BookingCare.Services.Review/Dockerfile"
build_and_push "saga-service" "src/Services/BookingCare.Services.Saga/Dockerfile"
build_and_push "schedule-service" "src/Services/BookingCare.Services.Schedule/Dockerfile"
build_and_push "servicemedical-service" "src/Services/BookingCare.Services.ServiceMedical/Dockerfile"
build_and_push "user-service" "src/Services/BookingCare.Services.User/Dockerfile"

# End timing
end_time=$(date +%s)
duration=$((end_time - start_time))
minutes=$((duration / 60))
seconds=$((duration % 60))

echo "=========================================="
echo "✅ All services built and pushed successfully!"
echo "=========================================="
echo "Total time: ${minutes}m ${seconds}s"
echo ""
echo "Images pushed:"
echo "  - ${DOCKER_USERNAME}/bookingcare-api-gateway:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-ai-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-analytics-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-appointment-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-auth-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-communication-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-content-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-discount-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-doctor-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-favorites-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-hospital-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-notification-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-payment-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-review-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-saga-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-schedule-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-servicemedical-service:${VERSION}"
echo "  - ${DOCKER_USERNAME}/bookingcare-user-service:${VERSION}"
echo "=========================================="
