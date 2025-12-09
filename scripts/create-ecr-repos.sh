#!/bin/bash

# Script to create ECR repositories for all BookingCare services
# Usage: ./create-ecr-repos.sh [region]

set -e

# Configuration
AWS_REGION="${1:-us-east-1}"

# Color codes for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Services list
SERVICES=(
  "apigateway"
  "ai"
  "analytics"
  "appointment"
  "auth"
  "blog"
  "communication"
  "content"
  "discount"
  "doctor"
  "favorites"
  "hospital"
  "notification"
  "payment"
  "review"
  "saga"
  "schedule"
  "servicemedical"
  "user"
)

echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}BookingCare ECR Repositories Setup${NC}"
echo -e "${YELLOW}========================================${NC}"
echo ""
echo -e "Region: ${GREEN}${AWS_REGION}${NC}"
echo -e "Total services: ${GREEN}${#SERVICES[@]}${NC}"
echo ""

# Check if AWS CLI is installed
if ! command -v aws &> /dev/null; then
    echo -e "${RED}Error: AWS CLI is not installed${NC}"
    echo "Please install AWS CLI: https://aws.amazon.com/cli/"
    exit 1
fi

# Check if AWS credentials are configured
if ! aws sts get-caller-identity &> /dev/null; then
    echo -e "${RED}Error: AWS credentials are not configured${NC}"
    echo "Please run: aws configure"
    exit 1
fi

echo -e "${GREEN}✓ AWS CLI is configured${NC}"
echo ""

# Confirm before proceeding
read -p "Do you want to create ECR repositories in region ${AWS_REGION}? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 0
fi

echo ""
echo -e "${YELLOW}Creating ECR repositories...${NC}"
echo ""

SUCCESS_COUNT=0
SKIPPED_COUNT=0
FAILED_COUNT=0

for service in "${SERVICES[@]}"; do
  REPO_NAME="bookingcare-${service}"
  
  # Check if repository already exists
  if aws ecr describe-repositories \
      --repository-names "${REPO_NAME}" \
      --region "${AWS_REGION}" &> /dev/null; then
    echo -e "${YELLOW}⏭️  ${REPO_NAME} - Already exists${NC}"
    ((SKIPPED_COUNT++))
  else
    # Create repository
    if aws ecr create-repository \
        --repository-name "${REPO_NAME}" \
        --region "${AWS_REGION}" \
        --image-scanning-configuration scanOnPush=true \
        --encryption-configuration encryptionType=AES256 &> /dev/null; then
      echo -e "${GREEN}✓ ${REPO_NAME} - Created successfully${NC}"
      ((SUCCESS_COUNT++))
      
      # Set lifecycle policy to keep only last 10 images
      aws ecr put-lifecycle-policy \
          --repository-name "${REPO_NAME}" \
          --region "${AWS_REGION}" \
          --lifecycle-policy-text '{
            "rules": [
              {
                "rulePriority": 1,
                "description": "Keep last 10 images",
                "selection": {
                  "tagStatus": "any",
                  "countType": "imageCountMoreThan",
                  "countNumber": 10
                },
                "action": {
                  "type": "expire"
                }
              }
            ]
          }' &> /dev/null
    else
      echo -e "${RED}✗ ${REPO_NAME} - Failed to create${NC}"
      ((FAILED_COUNT++))
    fi
  fi
done

echo ""
echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}Summary${NC}"
echo -e "${YELLOW}========================================${NC}"
echo -e "Created: ${GREEN}${SUCCESS_COUNT}${NC}"
echo -e "Skipped: ${YELLOW}${SKIPPED_COUNT}${NC}"
echo -e "Failed: ${RED}${FAILED_COUNT}${NC}"
echo ""

if [ $FAILED_COUNT -eq 0 ]; then
  echo -e "${GREEN}✓ All repositories are ready!${NC}"
  echo ""
  echo "Next steps:"
  echo "1. Configure GitHub Secrets:"
  echo "   - AWS_ACCESS_KEY_ID"
  echo "   - AWS_SECRET_ACCESS_KEY"
  echo "   - EC2_HOST"
  echo "   - EC2_USER"
  echo "   - EC2_SSH_KEY"
  echo ""
  echo "2. Update AWS_REGION in deployment.yml if needed (current: ${AWS_REGION})"
  echo ""
  echo "3. Test deployment by pushing to release branch"
else
  echo -e "${RED}Some repositories failed to create. Please check the errors above.${NC}"
  exit 1
fi
