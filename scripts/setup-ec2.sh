#!/bin/bash

# Script to setup EC2 instance for BookingCare deployment
# Run this script on your EC2 instance
# Usage: bash setup-ec2.sh

set -e

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}BookingCare EC2 Instance Setup${NC}"
echo -e "${YELLOW}========================================${NC}"
echo ""

# Detect OS
if [ -f /etc/os-release ]; then
    . /etc/os-release
    OS=$ID
else
    echo -e "${RED}Cannot detect OS${NC}"
    exit 1
fi

echo -e "Detected OS: ${GREEN}${OS}${NC}"
echo ""

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# 1. Update system
echo -e "${BLUE}[1/6] Updating system packages...${NC}"
if [[ "$OS" == "ubuntu" || "$OS" == "debian" ]]; then
    sudo apt-get update -y
    sudo apt-get upgrade -y
elif [[ "$OS" == "amzn" || "$OS" == "rhel" || "$OS" == "centos" ]]; then
    sudo yum update -y
fi
echo -e "${GREEN}✓ System updated${NC}"
echo ""

# 2. Install Docker
echo -e "${BLUE}[2/6] Installing Docker...${NC}"
if command_exists docker; then
    echo -e "${YELLOW}⏭️  Docker is already installed${NC}"
    docker --version
else
    if [[ "$OS" == "ubuntu" || "$OS" == "debian" ]]; then
        # Install Docker on Ubuntu/Debian
        sudo apt-get install -y apt-transport-https ca-certificates curl software-properties-common
        curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
        sudo add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"
        sudo apt-get update -y
        sudo apt-get install -y docker-ce docker-ce-cli containerd.io
    elif [[ "$OS" == "amzn" ]]; then
        # Install Docker on Amazon Linux
        sudo yum install -y docker
    elif [[ "$OS" == "rhel" || "$OS" == "centos" ]]; then
        # Install Docker on RHEL/CentOS
        sudo yum install -y yum-utils
        sudo yum-config-manager --add-repo https://download.docker.com/linux/centos/docker-ce.repo
        sudo yum install -y docker-ce docker-ce-cli containerd.io
    fi
    
    # Start Docker service
    sudo systemctl start docker
    sudo systemctl enable docker
    
    echo -e "${GREEN}✓ Docker installed${NC}"
    docker --version
fi
echo ""

# 3. Add user to docker group
echo -e "${BLUE}[3/6] Configuring Docker permissions...${NC}"
if groups $USER | grep -q docker; then
    echo -e "${YELLOW}⏭️  User already in docker group${NC}"
else
    sudo usermod -aG docker $USER
    echo -e "${GREEN}✓ User added to docker group${NC}"
    echo -e "${YELLOW}⚠️  Please logout and login again for docker group changes to take effect${NC}"
fi
echo ""

# 4. Install AWS CLI
echo -e "${BLUE}[4/6] Installing AWS CLI...${NC}"
if command_exists aws; then
    echo -e "${YELLOW}⏭️  AWS CLI is already installed${NC}"
    aws --version
else
    if [[ "$OS" == "ubuntu" || "$OS" == "debian" ]]; then
        # Install AWS CLI on Ubuntu/Debian
        sudo apt-get install -y unzip
        curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
        unzip awscliv2.zip
        sudo ./aws/install
        rm -rf aws awscliv2.zip
    elif [[ "$OS" == "amzn" ]]; then
        # Install AWS CLI on Amazon Linux
        sudo yum install -y aws-cli
    elif [[ "$OS" == "rhel" || "$OS" == "centos" ]]; then
        # Install AWS CLI on RHEL/CentOS
        sudo yum install -y unzip
        curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
        unzip awscliv2.zip
        sudo ./aws/install
        rm -rf aws awscliv2.zip
    fi
    
    echo -e "${GREEN}✓ AWS CLI installed${NC}"
    aws --version
fi
echo ""

# 5. Create Docker network
echo -e "${BLUE}[5/6] Creating Docker network...${NC}"
if sudo docker network ls | grep -q bookingcare-network; then
    echo -e "${YELLOW}⏭️  Docker network 'bookingcare-network' already exists${NC}"
else
    sudo docker network create bookingcare-network
    echo -e "${GREEN}✓ Docker network 'bookingcare-network' created${NC}"
fi
echo ""

# 6. Configure AWS credentials (if not using IAM role)
echo -e "${BLUE}[6/6] Configuring AWS credentials...${NC}"
if [ -f ~/.aws/credentials ]; then
    echo -e "${YELLOW}⏭️  AWS credentials already configured${NC}"
else
    echo -e "${YELLOW}Do you want to configure AWS credentials now?${NC}"
    echo -e "${YELLOW}(Skip this if you're using IAM role for EC2)${NC}"
    read -p "Configure AWS credentials? (y/n) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        aws configure
        echo -e "${GREEN}✓ AWS credentials configured${NC}"
    else
        echo -e "${YELLOW}⏭️  Skipped AWS credentials configuration${NC}"
        echo -e "${YELLOW}Make sure your EC2 instance has an IAM role with ECR permissions${NC}"
    fi
fi
echo ""

# Test Docker
echo -e "${BLUE}Testing Docker...${NC}"
if sudo docker run --rm hello-world > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Docker is working correctly${NC}"
else
    echo -e "${RED}✗ Docker test failed${NC}"
fi
echo ""

# Test AWS CLI (if credentials configured)
echo -e "${BLUE}Testing AWS CLI...${NC}"
if aws sts get-caller-identity > /dev/null 2>&1; then
    echo -e "${GREEN}✓ AWS CLI is configured correctly${NC}"
    echo -e "${YELLOW}AWS Account:${NC}"
    aws sts get-caller-identity
else
    echo -e "${YELLOW}⚠️  AWS CLI is not configured or IAM role is not attached${NC}"
fi
echo ""

# Summary
echo -e "${YELLOW}========================================${NC}"
echo -e "${YELLOW}Setup Complete!${NC}"
echo -e "${YELLOW}========================================${NC}"
echo ""
echo "Installed components:"
echo -e "  ${GREEN}✓${NC} Docker $(docker --version | cut -d' ' -f3)"
echo -e "  ${GREEN}✓${NC} AWS CLI $(aws --version | cut -d' ' -f1 | cut -d'/' -f2)"
echo -e "  ${GREEN}✓${NC} Docker network: bookingcare-network"
echo ""
echo "Next steps:"
echo "1. If you added user to docker group, logout and login again:"
echo "   ${YELLOW}exit${NC}"
echo ""
echo "2. Test ECR login:"
echo "   ${YELLOW}aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <ECR_REGISTRY>${NC}"
echo ""
echo "3. Configure GitHub Secrets in your repository:"
echo "   - EC2_HOST: $(curl -s http://169.254.169.254/latest/meta-data/public-ipv4 2>/dev/null || echo 'YOUR_EC2_PUBLIC_IP')"
echo "   - EC2_USER: $USER"
echo "   - EC2_SSH_KEY: (your SSH private key)"
echo ""
echo "4. Deploy your application!"
echo ""

# Create a test script
cat > ~/test-deployment.sh << 'TESTEOF'
#!/bin/bash
# Quick test script for deployment

echo "Testing Docker..."
docker ps

echo ""
echo "Testing AWS ECR login..."
AWS_REGION="us-east-1"
ECR_REGISTRY=$(aws ecr describe-repositories --region $AWS_REGION --query 'repositories[0].repositoryUri' --output text | cut -d'/' -f1)

if [ -n "$ECR_REGISTRY" ]; then
    aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ECR_REGISTRY
    echo "✓ ECR login successful"
else
    echo "⚠️  No ECR repositories found"
fi

echo ""
echo "Docker networks:"
docker network ls

echo ""
echo "Running containers:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
TESTEOF

chmod +x ~/test-deployment.sh
echo -e "${GREEN}✓ Created test script: ~/test-deployment.sh${NC}"
echo ""
