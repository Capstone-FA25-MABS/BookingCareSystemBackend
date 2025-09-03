#!/bin/bash

# Clean and Build Script for Doctor Service
# This script helps resolve build issues by cleaning and rebuilding

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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

print_header "Cleaning Doctor Service Project"

# Clean bin and obj directories
print_info "Cleaning bin and obj directories..."
rm -rf bin/
rm -rf obj/
print_success "Clean completed"

# Clean NuGet cache for this project
print_info "Cleaning NuGet cache..."
dotnet nuget locals all --clear
print_success "NuGet cache cleared"

# Restore packages
print_info "Restoring NuGet packages..."
dotnet restore
print_success "Packages restored"

# Build project
print_info "Building project..."
dotnet build --no-restore
print_success "Build completed"

# Build with detailed output if there are errors
print_info "Building with detailed output..."
if ! dotnet build --no-restore --verbosity normal; then
    print_error "Build failed. Showing detailed errors..."
    dotnet build --no-restore --verbosity detailed
    exit 1
fi

print_success "Doctor Service project cleaned and built successfully!"
print_info "You can now run: dotnet run"
