#!/bin/bash

# BookingCare Microservices Build Script
# This script builds all services in the correct order

set -e

echo "🚀 Starting BookingCare Microservices Build..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}📦 $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check if .NET 8 is installed
if ! command -v dotnet &> /dev/null; then
    print_error ".NET 8 SDK is not installed. Please install it first."
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
if [[ ! $DOTNET_VERSION == 8.* ]]; then
    print_error "This project requires .NET 8. Current version: $DOTNET_VERSION"
    exit 1
fi

print_status "Using .NET version: $DOTNET_VERSION"

# Clean previous builds
print_status "Cleaning previous builds..."
dotnet clean "$(dirname "$0")/../BookingCareSystem.sln" --configuration Release

# Restore NuGet packages
print_status "Restoring NuGet packages..."
dotnet restore "$(dirname "$0")/../BookingCareSystem.sln"

# Build shared libraries first
print_status "Building shared libraries..."
dotnet build src/Shared/BookingCare.Shared.Common/BookingCare.Shared.Common.csproj --configuration Release --no-restore
dotnet build src/Shared/BookingCare.Shared.Contracts/BookingCare.Shared.Contracts.csproj --configuration Release --no-restore
dotnet build src/Shared/BookingCare.Shared.EventBus/BookingCare.Shared.EventBus.csproj --configuration Release --no-restore

# Build infrastructure services
print_status "Building infrastructure services..."
dotnet build src/Infrastructure/BookingCare.MessageBus/BookingCare.MessageBus.csproj --configuration Release --no-restore
dotnet build src/Infrastructure/BookingCare.ServiceDiscovery/BookingCare.ServiceDiscovery.csproj --configuration Release --no-restore
dotnet build src/Infrastructure/BookingCare.Gateway/BookingCare.Gateway.csproj --configuration Release --no-restore

# Build microservices
print_status "Building microservices..."

SERVICES=(
    "AuthService"
    "UserService"
    "DoctorService"
    "ClinicService"
    "AppointmentService"
    "PaymentService"
    "ReviewService"
    "ContentService"
    "ServiceMedicalService"
    "CommunicationService"
    "NotificationService"
    "PromotionService"
    "FavoriteService"
    "AnalyticsService"
    "AIService"
)

for service in "${SERVICES[@]}"; do
    if [ -f "src/Services/$service/BookingCare.$service/BookingCare.$service.csproj" ]; then
        print_status "Building $service..."
        dotnet build "src/Services/$service/BookingCare.$service/BookingCare.$service.csproj" --configuration Release --no-restore
        print_success "$service built successfully"
    else
        print_error "$service project file not found, skipping..."
    fi
done

# Build entire solution
print_status "Building entire solution..."
dotnet build "$(dirname "$0")/../BookingCareSystem.sln" --configuration Release --no-restore

print_success "🎉 All services built successfully!"

# Optional: Run tests if test projects exist
if [ "$1" = "--with-tests" ]; then
    print_status "Running tests..."
    if [ -d "tests" ]; then
        dotnet test "$(dirname "$0")/../BookingCareSystem.sln" --configuration Release --no-build --verbosity normal
        print_success "All tests passed!"
    else
        print_error "No test projects found"
    fi
fi

print_success "Build completed successfully! 🚀"
