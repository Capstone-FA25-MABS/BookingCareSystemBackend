#!/bin/bash
set -e

###############################################################################
# BookingCare EC2 Instance User Data Script
# This script will run on first boot to set up the EC2 instance
###############################################################################

# Log all output
exec > >(tee /var/log/user-data.log|logger -t user-data -s 2>/dev/console) 2>&1

echo "========================================="
echo "Starting BookingCare EC2 Setup"
echo "========================================="

# Update system packages
echo "Updating system packages..."
apt-get update
apt-get upgrade -y

# Install essential tools
echo "Installing essential tools..."
apt-get install -y \
    curl \
    wget \
    git \
    vim \
    htop \
    net-tools \
    unzip \
    jq \
    ca-certificates \
    gnupg \
    lsb-release \
    software-properties-common

###############################################################################
# Install Docker
###############################################################################

echo "Installing Docker..."

# Add Docker's official GPG key
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg

# Add Docker repository
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker Engine
apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Start and enable Docker
systemctl start docker
systemctl enable docker

# Add ubuntu user to docker group
usermod -aG docker ubuntu

echo "Docker installed successfully!"
docker --version
docker compose version

###############################################################################
# Configure Docker Storage
###############################################################################

echo "Configuring Docker storage..."

# Wait for the EBS volume to be attached
sleep 10

# Check if the volume is attached
if [ -e /dev/nvme1n1 ] || [ -e /dev/xvdf ]; then
    # Determine the device name
    DEVICE=""
    if [ -e /dev/nvme1n1 ]; then
        DEVICE="/dev/nvme1n1"
    elif [ -e /dev/xvdf ]; then
        DEVICE="/dev/xvdf"
    fi

    echo "Found device: $DEVICE"

    # Check if the device is already formatted
    if ! blkid $DEVICE; then
        echo "Formatting $DEVICE..."
        mkfs.ext4 $DEVICE
    fi

    # Create mount point
    mkdir -p /var/lib/docker

    # Mount the volume
    mount $DEVICE /var/lib/docker

    # Add to fstab for persistent mounting
    DEVICE_UUID=$(blkid -s UUID -o value $DEVICE)
    if ! grep -q $DEVICE_UUID /etc/fstab; then
        echo "UUID=$DEVICE_UUID /var/lib/docker ext4 defaults,nofail 0 2" >> /etc/fstab
    fi

    echo "Docker storage configured on $DEVICE"
else
    echo "WARNING: Additional EBS volume not found. Using root volume for Docker storage."
fi

# Restart Docker to use new storage location
systemctl restart docker

###############################################################################
# Install Docker Compose (standalone)
###############################################################################

echo "Installing Docker Compose standalone..."
DOCKER_COMPOSE_VERSION=$(curl -s https://api.github.com/repos/docker/compose/releases/latest | jq -r .tag_name)
curl -L "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose
ln -sf /usr/local/bin/docker-compose /usr/bin/docker-compose

###############################################################################
# Install AWS CLI
###############################################################################

echo "Installing AWS CLI..."
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
./aws/install
rm -rf aws awscliv2.zip

###############################################################################
# Install Node.js (for frontend if needed)
###############################################################################

echo "Installing Node.js..."
curl -fsSL https://deb.nodesource.com/setup_20.x | bash -
apt-get install -y nodejs

echo "Node.js installed successfully!"
node --version
npm --version

###############################################################################
# Configure System Settings
###############################################################################

echo "Configuring system settings..."

# Increase file descriptors
cat >> /etc/security/limits.conf <<EOF
* soft nofile 65535
* hard nofile 65535
EOF

# Increase max map count for Elasticsearch (if needed)
echo "vm.max_map_count=262144" >> /etc/sysctl.conf
sysctl -p

# Configure swap (optional)
if [ ! -f /swapfile ]; then
    echo "Creating swap file..."
    fallocate -l 8G /swapfile
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    echo '/swapfile none swap sw 0 0' >> /etc/fstab
    echo 'vm.swappiness=10' >> /etc/sysctl.conf
    sysctl -p
fi

###############################################################################
# Install CloudWatch Agent (optional)
###############################################################################

echo "Installing CloudWatch Agent..."
wget https://s3.amazonaws.com/amazoncloudwatch-agent/ubuntu/amd64/latest/amazon-cloudwatch-agent.deb
dpkg -i -E ./amazon-cloudwatch-agent.deb
rm amazon-cloudwatch-agent.deb

###############################################################################
# Create directory structure
###############################################################################

echo "Creating directory structure..."
mkdir -p /home/ubuntu/bookingcare
mkdir -p /home/ubuntu/bookingcare/logs
mkdir -p /home/ubuntu/bookingcare/data
chown -R ubuntu:ubuntu /home/ubuntu/bookingcare

###############################################################################
# Setup firewall (UFW)
###############################################################################

echo "Configuring firewall..."
ufw --force enable
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp    # SSH
ufw allow 80/tcp    # HTTP
ufw allow 443/tcp   # HTTPS
ufw allow 5000:5001/tcp    # API Gateway
ufw allow 5173:5174/tcp    # Frontend
ufw allow 6000:6020/tcp    # Microservices HTTP
ufw allow 6100:6120/tcp    # Microservices gRPC
ufw allow 3000/tcp         # Grafana
ufw allow 9090/tcp         # Prometheus
ufw allow 15672/tcp        # RabbitMQ Management
ufw allow 16686/tcp        # Jaeger UI
ufw reload

###############################################################################
# Setup automatic security updates
###############################################################################

echo "Configuring automatic security updates..."
apt-get install -y unattended-upgrades
dpkg-reconfigure -plow unattended-upgrades

###############################################################################
# Install monitoring tools
###############################################################################

echo "Installing monitoring tools..."
apt-get install -y \
    sysstat \
    iotop \
    iftop \
    nethogs

###############################################################################
# Create deployment script
###############################################################################

cat > /home/ubuntu/deploy.sh <<'DEPLOY_SCRIPT'
#!/bin/bash
set -e

echo "========================================="
echo "BookingCare Deployment Script"
echo "========================================="

PROJECT_DIR="/home/ubuntu/bookingcare"
REPO_URL="https://github.com/hiumx/BookingCareSystemBackend.git"
BRANCH="${BRANCH:-develop}"

# Navigate to project directory
cd $PROJECT_DIR

# Check if repository exists
if [ -d ".git" ]; then
    echo "Pulling latest changes from $BRANCH branch..."
    git fetch origin
    git checkout $BRANCH
    git pull origin $BRANCH
else
    echo "Cloning repository (branch: $BRANCH)..."
    git clone -b $BRANCH $REPO_URL .
fi

# Check if docker-compose.yml exists
if [ ! -f "docker-compose.yml" ] && [ ! -f "docker-compose-full.yml" ]; then
    echo "ERROR: docker-compose.yml not found!"
    exit 1
fi

# Use docker-compose-full.yml if it exists
COMPOSE_FILE="docker-compose.yml"
if [ -f "docker-compose-full.yml" ]; then
    COMPOSE_FILE="docker-compose-full.yml"
fi

echo "Using compose file: $COMPOSE_FILE"

# Stop existing containers
echo "Stopping existing containers..."
docker compose -f $COMPOSE_FILE down || true

# Pull latest images (if any)
echo "Pulling latest Docker images..."
docker compose -f $COMPOSE_FILE pull || true

# Build and start containers
echo "Building and starting containers..."
docker compose -f $COMPOSE_FILE build --no-cache
docker compose -f $COMPOSE_FILE up -d

# Wait for services to be healthy
echo "Waiting for services to start..."
sleep 30

# Show running containers
echo "Running containers:"
docker compose -f $COMPOSE_FILE ps

# Show resource usage
echo ""
echo "Docker resource usage:"
docker stats --no-stream

# Show logs
echo ""
echo "Recent logs:"
docker compose -f $COMPOSE_FILE logs --tail=50

echo ""
echo "========================================="
echo "Deployment complete!"
echo "To view logs: docker compose -f $COMPOSE_FILE logs -f"
echo "========================================="
DEPLOY_SCRIPT

chmod +x /home/ubuntu/deploy.sh
chown ubuntu:ubuntu /home/ubuntu/deploy.sh

###############################################################################
# Create system info script
###############################################################################

cat > /home/ubuntu/system-info.sh <<'INFO_SCRIPT'
#!/bin/bash

echo "========================================="
echo "BookingCare System Information"
echo "========================================="
echo ""
echo "System:"
echo "  - OS: $(lsb_release -d | cut -f2)"
echo "  - Kernel: $(uname -r)"
echo "  - Uptime: $(uptime -p)"
echo ""
echo "CPU:"
echo "  - Model: $(lscpu | grep 'Model name' | cut -f 2 -d ':' | awk '{$1=$1}1')"
echo "  - Cores: $(nproc)"
echo "  - Usage: $(top -bn1 | grep "Cpu(s)" | awk '{print $2}')%"
echo ""
echo "Memory:"
free -h
echo ""
echo "Disk Usage:"
df -h
echo ""
echo "Docker:"
echo "  - Version: $(docker --version)"
echo "  - Compose Version: $(docker-compose --version)"
echo "  - Running Containers: $(docker ps -q | wc -l)"
echo ""
echo "Network:"
ip addr show | grep "inet " | grep -v 127.0.0.1
INFO_SCRIPT

chmod +x /home/ubuntu/system-info.sh
chown ubuntu:ubuntu /home/ubuntu/system-info.sh

###############################################################################
# Final Setup
###############################################################################

echo "Setting ownership..."
chown -R ubuntu:ubuntu /home/ubuntu

###############################################################################
# AUTO-DEPLOY APPLICATION ON FIRST BOOT
###############################################################################

echo "========================================="
echo "Starting Auto-Deployment..."
echo "========================================="

# Clone and deploy the application
cd /home/ubuntu/bookingcare

REPO_URL="https://github.com/hiumx/BookingCareSystemBackend.git"
BRANCH="develop"

echo "Cloning repository (branch: $BRANCH)..."
sudo -u ubuntu git clone -b $BRANCH $REPO_URL . || {
    echo "Failed to clone repository. Check if the repo is accessible."
}

# Check if docker-compose file exists
if [ -f "docker-compose-full.yml" ]; then
    COMPOSE_FILE="docker-compose-full.yml"
elif [ -f "docker-compose.yml" ]; then
    COMPOSE_FILE="docker-compose.yml"
else
    echo "WARNING: No docker-compose file found. Skipping auto-deployment."
    COMPOSE_FILE=""
fi

if [ -n "$COMPOSE_FILE" ]; then
    echo "Found compose file: $COMPOSE_FILE"
    echo "Starting Docker containers..."
    
    cd /home/ubuntu/bookingcare
    
    # Pull images first (if any)
    sudo -u ubuntu docker compose -f $COMPOSE_FILE pull || true
    
    # Build and start containers
    sudo -u ubuntu docker compose -f $COMPOSE_FILE build
    sudo -u ubuntu docker compose -f $COMPOSE_FILE up -d
    
    # Wait for services to be ready
    sleep 30
    
    echo "Deployed containers:"
    sudo -u ubuntu docker compose -f $COMPOSE_FILE ps
fi

echo "========================================="
echo "BookingCare EC2 Setup Complete!"
echo "========================================="
echo ""
echo "Installed Software:"
echo "  - Docker: $(docker --version)"
echo "  - Docker Compose: $(docker compose version)"
echo "  - AWS CLI: $(aws --version)"
echo "  - Node.js: $(node --version)"
echo "  - npm: $(npm --version)"
echo "  - Git: $(git --version)"
echo ""
echo "Application Status:"
if [ -n "$COMPOSE_FILE" ]; then
    echo "  - Repository: Cloned from $REPO_URL"
    echo "  - Branch: $BRANCH"
    echo "  - Compose file: $COMPOSE_FILE"
    echo "  - Containers: Running (check with 'docker compose ps')"
else
    echo "  - Status: Manual deployment required"
fi
echo ""
echo "Useful Commands:"
echo "  - View system info: /home/ubuntu/system-info.sh"
echo "  - Re-deploy application: /home/ubuntu/deploy.sh"
echo "  - View application logs: cd /home/ubuntu/bookingcare && docker compose logs -f"
echo "  - Stop application: cd /home/ubuntu/bookingcare && docker compose down"
echo ""
echo "Access URLs (replace <instance-ip> with your actual IP):"
echo "  - API Gateway: http://<instance-ip>:5000"
echo "  - Frontend Client: http://<instance-ip>:5173"
echo "  - Frontend Admin: http://<instance-ip>:5174"
echo "  - RabbitMQ Management: http://<instance-ip>:15672"
echo "  - Grafana: http://<instance-ip>:3000"
echo "  - Prometheus: http://<instance-ip>:9090"
echo "  - Jaeger UI: http://<instance-ip>:16686"
echo ""
echo "========================================="

# Create a completion marker
touch /var/log/user-data-complete
date > /var/log/user-data-complete-time
