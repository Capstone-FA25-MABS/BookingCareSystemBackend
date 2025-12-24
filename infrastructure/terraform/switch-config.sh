#!/bin/bash

###############################################################################
# BookingCare Terraform Config Switcher
# Script để chuyển đổi giữa cấu hình test (free tier) và production
###############################################################################

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_header() {
    echo -e "${BLUE}=========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}=========================================${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

show_usage() {
    echo "Usage: $0 [test|production|status|diff]"
    echo ""
    echo "Commands:"
    echo "  test        - Switch to Free Tier test configuration (t3.micro)"
    echo "  production  - Switch to Production configuration (c5.4xlarge)"
    echo "  status      - Show current configuration"
    echo "  diff        - Show differences between test and production configs"
    echo ""
    echo "Examples:"
    echo "  $0 test         # Switch to test config"
    echo "  $0 production   # Switch to production config"
    echo "  $0 status       # Check current config"
    echo "  $0 diff         # Compare configs"
}

show_status() {
    print_header "Current Configuration"
    
    if [ ! -f "terraform.tfvars" ]; then
        print_error "terraform.tfvars not found!"
        exit 1
    fi
    
    ENV=$(grep '^environment' terraform.tfvars | awk -F'"' '{print $2}')
    INSTANCE_TYPE=$(grep '^instance_type' terraform.tfvars | awk -F'"' '{print $2}')
    ROOT_SIZE=$(grep '^root_volume_size' terraform.tfvars | awk '{print $3}')
    DOCKER_SIZE=$(grep '^docker_volume_size' terraform.tfvars | awk '{print $3}')
    
    echo ""
    echo "Environment: $ENV"
    echo "Instance Type: $INSTANCE_TYPE"
    echo "Root Volume: ${ROOT_SIZE}GB"
    echo "Docker Volume: ${DOCKER_SIZE}GB"
    echo ""
    
    if [ "$ENV" = "test" ]; then
        print_warning "Currently using TEST configuration (Free Tier)"
        echo "To switch to production: $0 production"
    elif [ "$ENV" = "production" ]; then
        print_success "Currently using PRODUCTION configuration"
        echo "To test with free tier: $0 test"
    else
        print_warning "Unknown environment: $ENV"
    fi
}

show_diff() {
    print_header "Configuration Differences"
    
    if [ ! -f "terraform.tfvars.test" ]; then
        print_error "terraform.tfvars.test not found!"
        exit 1
    fi
    
    if [ ! -f "terraform.tfvars.backup" ]; then
        print_error "terraform.tfvars.backup (production) not found!"
        exit 1
    fi
    
    echo ""
    echo "Comparing: production (backup) vs test"
    echo ""
    diff -y --suppress-common-lines terraform.tfvars.backup terraform.tfvars.test || true
    echo ""
}

switch_to_test() {
    print_header "Switching to TEST Configuration"
    
    # Backup current config if it's not already backed up
    if [ ! -f "terraform.tfvars.production" ]; then
        if grep -q 'environment.*production' terraform.tfvars 2>/dev/null; then
            print_warning "Backing up current production config..."
            cp terraform.tfvars terraform.tfvars.production
            print_success "Backup saved to terraform.tfvars.production"
        fi
    fi
    
    # Check if test config exists
    if [ ! -f "terraform.tfvars.test" ]; then
        print_error "terraform.tfvars.test not found!"
        exit 1
    fi
    
    # Switch to test config
    cp terraform.tfvars.test terraform.tfvars
    print_success "Switched to FREE TIER test configuration"
    
    echo ""
    print_warning "Configuration Details:"
    echo "  - Instance: t3.micro (1GB RAM, 2 vCPU)"
    echo "  - Root Volume: 20GB"
    echo "  - Docker Volume: 10GB"
    echo "  - Monitoring: Disabled"
    echo "  - Environment: test"
    echo ""
    
    print_warning "Next steps:"
    echo "  1. Review changes: terraform plan"
    echo "  2. Deploy: terraform apply"
    echo ""
    
    # Check if infrastructure exists
    if [ -f "terraform.tfstate" ] && [ -s "terraform.tfstate" ]; then
        print_warning "⚠️  WARNING: Infrastructure already exists!"
        print_warning "   Run 'terraform plan' to see what will change."
        print_warning "   You may need to destroy and recreate resources."
    fi
}

switch_to_production() {
    print_header "Switching to PRODUCTION Configuration"
    
    # Check if production config exists
    if [ -f "terraform.tfvars.production" ]; then
        SOURCE_FILE="terraform.tfvars.production"
    elif [ -f "terraform.tfvars.backup" ]; then
        SOURCE_FILE="terraform.tfvars.backup"
    else
        print_error "Production config not found!"
        print_error "Looking for: terraform.tfvars.production or terraform.tfvars.backup"
        exit 1
    fi
    
    # Backup test config if currently using test
    if grep -q 'environment.*test' terraform.tfvars 2>/dev/null; then
        print_warning "Saving current test config..."
        cp terraform.tfvars terraform.tfvars.test.bak
        print_success "Test config backed up to terraform.tfvars.test.bak"
    fi
    
    # Switch to production config
    cp "$SOURCE_FILE" terraform.tfvars
    print_success "Switched to PRODUCTION configuration"
    
    echo ""
    print_warning "Configuration Details:"
    echo "  - Instance: c5.4xlarge (32GB RAM, 16 vCPU)"
    echo "  - Root Volume: 100GB"
    echo "  - Docker Volume: 200GB"
    echo "  - Monitoring: Enabled"
    echo "  - Environment: production"
    echo ""
    
    print_warning "⚠️  PRODUCTION DEPLOYMENT WARNING:"
    echo "  - Higher costs (~\$500-800/month estimated)"
    echo "  - Make sure you've tested with free tier first"
    echo "  - Review budget and cost alerts"
    echo ""
    
    print_warning "Next steps:"
    echo "  1. Review changes: terraform plan"
    echo "  2. Deploy: terraform apply"
    echo ""
}

# Main script
case "${1:-}" in
    test)
        switch_to_test
        ;;
    production|prod)
        switch_to_production
        ;;
    status)
        show_status
        ;;
    diff)
        show_diff
        ;;
    help|--help|-h)
        show_usage
        ;;
    *)
        print_error "Invalid command: ${1:-}"
        echo ""
        show_usage
        exit 1
        ;;
esac
