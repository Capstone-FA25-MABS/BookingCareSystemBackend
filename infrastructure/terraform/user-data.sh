#!/bin/bash
set -e

###############################################################################
# BookingCare EC2 Instance User Data Script
# This script will run on first boot to set up the EC2 instance
# 
# Flow:
# 1. Update packages
# 2. Install Docker & Git
# 3. Clone source from public repo
# 4. Create Docker volumes
# 5. Restore data (if backup exists)
# 6. Start application
###############################################################################

# Log all output
exec > >(tee /var/log/user-data.log|logger -t user-data -s 2>/dev/console) 2>&1

echo "========================================="
echo "BookingCare EC2 Setup Started"
echo "========================================="
echo "Timestamp: $(date)"
echo ""

###############################################################################
# Step 1: Update System Packages
###############################################################################

echo "═══════════════════════════════════════════════════════════"
echo "Step 1: Updating system packages..."
echo "═══════════════════════════════════════════════════════════"

apt-get update -y
apt-get upgrade -y

echo "✓ System packages updated successfully"
echo ""

###############################################################################
# Step 2: Install Docker & Git
###############################################################################

echo "═══════════════════════════════════════════════════════════"
echo "Step 2: Installing Docker and Git..."
echo "═══════════════════════════════════════════════════════════"

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

echo "✓ Essential tools installed"

# Install Docker
echo ""
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
apt-get update -y
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Start and enable Docker
echo "Starting and enabling Docker service..."
systemctl start docker
systemctl enable docker

# Verify Docker is running
if systemctl is-active --quiet docker; then
    echo "✓ Docker service is running"
else
    echo "✗ ERROR: Docker service failed to start"
    exit 1
fi

# Add ubuntu user to docker group
usermod -aG docker ubuntu

echo ""
echo "Docker version:"
docker --version
docker compose version

echo "✓ Docker installed and configured successfully"
echo ""

###############################################################################
# Step 3: Clone Source Code from Public Repository
###############################################################################

echo "═══════════════════════════════════════════════════════════"
echo "Step 3: Cloning source code from GitHub..."
echo "═══════════════════════════════════════════════════════════"

# Create project directory
PROJECT_DIR="/home/ubuntu/booking-care-integration"
BACKUP_DIR="/home/ubuntu/backups"

mkdir -p $PROJECT_DIR
mkdir -p $BACKUP_DIR
mkdir -p /home/ubuntu/logs

echo "Cloning repository..."
REPO_URL="https://github.com/Capstone-FA25-MABS/booking-care-integration.git"
BRANCH="main"

cd /home/ubuntu
git clone -b $BRANCH $REPO_URL booking-care-integration

if [ -d "$PROJECT_DIR" ]; then
    echo "✓ Repository cloned successfully"
    cd $PROJECT_DIR
    git log -1 --oneline
    
    # Make scripts executable
    chmod +x scripts/*.sh
    echo "✓ Scripts made executable"
else
    echo "✗ ERROR: Failed to clone repository"
    exit 1
fi

# Set ownership
chown -R ubuntu:ubuntu /home/ubuntu/booking-care-integration
chown -R ubuntu:ubuntu /home/ubuntu/backups
chown -R ubuntu:ubuntu /home/ubuntu/logs

echo "✓ Source code ready"
echo ""

###############################################################################
# Step 4: Configure System Settings
###############################################################################

echo "═══════════════════════════════════════════════════════════"
echo "Step 4: Configuring system settings..."
echo "═══════════════════════════════════════════════════════════"

# Increase file descriptors
cat >> /etc/security/limits.conf <<EOF
* soft nofile 65535
* hard nofile 65535
EOF

# Increase max map count (for databases)
echo "vm.max_map_count=262144" >> /etc/sysctl.conf
sysctl -p

# Configure swap (8GB)
if [ ! -f /swapfile ]; then
    echo "Creating swap file (8GB)..."
    fallocate -l 8G /swapfile
    chmod 600 /swapfile
    mkswap /swapfile
    swapon /swapfile
    echo '/swapfile none swap sw 0 0' >> /etc/fstab
    echo 'vm.swappiness=10' >> /etc/sysctl.conf
    sysctl -p
    echo "✓ Swap configured"
fi

echo "✓ System settings configured"
echo ""

###############################################################################
# Step 5: Create Docker Volumes for Data Persistence
###############################################################################

echo "═══════════════════════════════════════════════════════════"
echo "Step 5: Creating Docker volumes..."
echo "═══════════════════════════════════════════════════════════"

# Volume list
VOLUMES=(
    "bookingcaresystembackend_rabbitmq_data"
    "bookingcaresystembackend_redis_data"
    "bookingcaresystembackend_mongodb_data"
    "bookingcaresystembackend_sqlserver_discount_data"
    "bookingcaresystembackend_sqlserver_saga_data"
    "bookingcaresystembackend_sqlserver_user_data"
    "bookingcaresystembackend_sqlserver_doctor_data"
    "bookingcaresystembackend_sqlserver_auth_data"
    "bookingcaresystembackend_sqlserver_appointment_data"
    "bookingcaresystembackend_sqlserver_hospital_data"
    "bookingcaresystembackend_sqlserver_schedule_data"
    "bookingcaresystembackend_sqlserver_payment_data"
    "bookingcaresystembackend_sqlserver_servicemedical_data"
    "bookingcaresystembackend_sqlserver_ai_data"
)

CREATED_COUNT=0

for volume in "${VOLUMES[@]}"; do
    if docker volume inspect "$volume" > /dev/null 2>&1; then
        echo "  Volume already exists: $volume"
    else
        docker volume create "$volume" > /dev/null
        echo "  ✓ Created volume: $volume"
        ((CREATED_COUNT++))
    fi
done

echo ""
echo "✓ Created $CREATED_COUNT new volumes"
echo "✓ Total volumes: ${#VOLUMES[@]}"
echo ""

###############################################################################
# Step 6: Check and Restore Data from Backup
###############################################################################

echo "═══════════════════════════════════════════════════════════"
echo "Step 6: Checking for data backup..."
echo "═══════════════════════════════════════════════════════════"

# Check if backup exists in S3 or local directory
BACKUP_EXISTS=false
LATEST_BACKUP=""

# Check for local backup files
if [ -d "$BACKUP_DIR" ]; then
    LATEST_BACKUP=$(ls -t $BACKUP_DIR/*_databases.tar.gz 2>/dev/null | head -1)
    if [ -n "$LATEST_BACKUP" ]; then
        BACKUP_EXISTS=true
        echo "✓ Found local backup: $LATEST_BACKUP"
    fi
fi

# If backup exists, restore it
if [ "$BACKUP_EXISTS" = true ]; then
    echo ""
    echo "Starting data restore process..."
    
    # Extract backup if it's compressed
    BACKUP_FILENAME=$(basename "$LATEST_BACKUP")
    BACKUP_DIR_NAME="${BACKUP_FILENAME%_databases.tar.gz}"
    
    cd $BACKUP_DIR
    if [ -f "$BACKUP_FILENAME" ]; then
        tar xzf "$BACKUP_FILENAME"
        echo "✓ Backup extracted"
    fi
    
    # Start infrastructure services first
    echo ""
    echo "Starting infrastructure services..."
    cd $PROJECT_DIR
    
    docker compose up -d rabbitmq redis mongodb \
        sqlserver-discount sqlserver-saga sqlserver-user \
        sqlserver-doctor sqlserver-auth sqlserver-appointment \
        sqlserver-hospital sqlserver-schedule sqlserver-payment \
        sqlserver-servicemedical sqlserver-ai
    
    echo "Waiting for services to be healthy (60 seconds)..."
    sleep 60
    
    # Run restore script
    echo ""
    echo "Restoring data..."
    cd $PROJECT_DIR/scripts
    
    if [ -x "./restore-databases.sh" ]; then
        ./restore-databases.sh "$BACKUP_DIR/$BACKUP_DIR_NAME" || {
            echo "⚠ Warning: Data restore failed, continuing with fresh installation"
        }
    else
        echo "⚠ Warning: Restore script not found, skipping data restore"
    fi
    
    echo "✓ Data restore process completed"
else
    echo "ℹ No backup found - will start with fresh data"
    echo "  To restore data later:"
    echo "  1. Upload backup to: $BACKUP_DIR"
    echo "  2. Run: cd $PROJECT_DIR/scripts && ./restore-databases.sh /path/to/backup"
fi

echo ""

###############################################################################
# Step 7: Start Application Services
###############################################################################

echo "═══════════════════════════════════════════════════════════"
echo "Step 7: Starting BookingCare application..."
echo "═══════════════════════════════════════════════════════════"

cd $PROJECT_DIR

# Check if .env file exists
if [ ! -f ".env" ]; then
    echo "⚠ Warning: .env file not found"
    if [ -f ".env.example" ]; then
        echo "Creating .env from .env.example..."
        cp .env.example .env
        echo "✓ .env file created"
        echo "⚠ Please update .env file with actual credentials!"
    else
        echo "✗ ERROR: No .env.example file found"
        echo "Please create .env file manually before starting services"
        exit 1
    fi
fi

# Start all services
echo ""
echo "Starting all services..."
docker compose up -d

echo ""
echo "Waiting for services to start (30 seconds)..."
sleep 30

# Check service status
echo ""
echo "Service status:"
docker compose ps

echo ""
echo "✓ Application started successfully"
echo ""

###############################################################################
# Setup Firewall
###############################################################################

echo "═══════════════════════════════════════════════════════════"
echo "Configuring firewall..."
echo "═══════════════════════════════════════════════════════════"

ufw --force enable
ufw default deny incoming
ufw default allow outgoing

# Essential ports
ufw allow 22/tcp           # SSH
ufw allow 80/tcp           # HTTP
ufw allow 443/tcp          # HTTPS

# Application ports (public access)
ufw allow 5000:5001/tcp    # API Gateway - Main entry point
ufw allow 5173:5174/tcp    # Frontend Apps (User & Admin UI)

# Monitoring & Management (optional - can be restricted to admin IPs)
ufw allow 3000/tcp         # Grafana Dashboard
ufw allow 9090/tcp         # Prometheus
ufw allow 16686/tcp        # Jaeger UI
ufw allow 15672/tcp        # RabbitMQ Management (admin only)

# NOTE: Microservices ports (6000-6020, 6100-6120) are NOT exposed
# They communicate internally via Docker network for security

ufw reload

echo "✓ Firewall configured"
echo ""

###############################################################################
# Create Management Scripts
###############################################################################

echo "═══════════════════════════════════════════════════════════"
echo "Creating management scripts..."
echo "═══════════════════════════════════════════════════════════"

# Create update script
cat > /home/ubuntu/update-app.sh <<'UPDATE_SCRIPT'
#!/bin/bash
set -e

echo "========================================="
echo "BookingCare Update Script"
echo "========================================="

PROJECT_DIR="/home/ubuntu/booking-care-integration"

cd $PROJECT_DIR

# Pull latest changes
echo "Pulling latest changes..."
git pull origin main

# Restart services
echo "Restarting services..."
docker compose down
docker compose up -d

echo "Waiting for services to start..."
sleep 30

docker compose ps

echo "✓ Update completed"
UPDATE_SCRIPT

# Create backup script
cat > /home/ubuntu/backup-data.sh <<'BACKUP_SCRIPT'
#!/bin/bash
set -e

echo "========================================="
echo "BookingCare Backup Script"
echo "========================================="

PROJECT_DIR="/home/ubuntu/booking-care-integration"

cd $PROJECT_DIR/scripts
./backup-databases.sh /home/ubuntu/backups

echo "✓ Backup completed"
echo "Backup location: /home/ubuntu/backups"
BACKUP_SCRIPT

# Create status check script
cat > /home/ubuntu/check-status.sh <<'STATUS_SCRIPT'
#!/bin/bash

echo "========================================="
echo "BookingCare System Status"
echo "========================================="
echo ""

# Service status
echo "Docker Compose Services:"
cd /home/ubuntu/booking-care-integration
docker compose ps
echo ""

# Resource usage
echo "Resource Usage:"
docker stats --no-stream
echo ""

# Disk space
echo "Disk Space:"
df -h
echo ""

# Memory
echo "Memory:"
free -h
STATUS_SCRIPT

# Make scripts executable
chmod +x /home/ubuntu/*.sh
chown ubuntu:ubuntu /home/ubuntu/*.sh

echo "✓ Management scripts created:"
echo "  - /home/ubuntu/update-app.sh - Update application"
echo "  - /home/ubuntu/backup-data.sh - Backup data"
echo "  - /home/ubuntu/check-status.sh - Check system status"
echo ""

###############################################################################
# Final Summary
###############################################################################

echo "═══════════════════════════════════════════════════════════"
echo "BookingCare EC2 Setup Completed Successfully! 🎉"
echo "═══════════════════════════════════════════════════════════"
echo ""
echo "Timestamp: $(date)"
echo ""

echo "✓ Setup Summary:"
echo "  [✓] System packages updated"
echo "  [✓] Docker & Git installed and verified"
echo "  [✓] Source code cloned from GitHub"
echo "  [✓] Docker volumes created (14 volumes)"
echo "  [✓] Data backup checked and restored (if available)"
echo "  [✓] Application services started"
echo "  [✓] Firewall configured"
echo "  [✓] Management scripts created"
echo ""

echo "📂 Project Structure:"
echo "  - Project: /home/ubuntu/booking-care-integration"
echo "  - Backups: /home/ubuntu/backups"
echo "  - Logs: /home/ubuntu/logs"
echo ""

echo "🔧 Management Scripts:"
echo "  - update-app.sh     - Update and restart application"
echo "  - backup-data.sh    - Backup all databases"
echo "  - check-status.sh   - Check system and service status"
echo ""

# Get EC2 public IP
EC2_PUBLIC_IP=$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4 2>/dev/null || echo "your-ec2-ip")

echo "📊 Access Points:"
echo "  - User UI:      http://${EC2_PUBLIC_IP}:5173"
echo "  - Admin UI:     http://${EC2_PUBLIC_IP}:5174"
echo "  - API Gateway:  http://${EC2_PUBLIC_IP}:5000"
echo ""

echo "⚠️  Important Next Steps:"
echo "  1. Update .env file with production credentials:"
echo "     sudo nano /home/ubuntu/booking-care-integration/.env"
echo ""
echo "  2. To upload backup data:"
echo "     scp backup.tar.gz ubuntu@${EC2_PUBLIC_IP}:/home/ubuntu/backups/"
echo "     ssh ubuntu@${EC2_PUBLIC_IP}"
echo "     cd /home/ubuntu/booking-care-integration/scripts"
echo "     ./restore-databases.sh /home/ubuntu/backups/YYYYMMDD_HHMMSS"
echo ""
echo "  3. Check service status:"
echo "     cd /home/ubuntu/booking-care-integration"
echo "     docker compose ps"
echo "     docker compose logs -f"
echo ""
echo "  4. Create regular backups (add to crontab):"
echo "     crontab -e"
echo "     # Add: 0 2 * * * /home/ubuntu/backup-data.sh"
echo ""

echo "📖 Documentation:"
echo "  - Backup Guide: /home/ubuntu/booking-care-integration/docs/BACKUP_RESTORE_GUIDE.md"
echo "  - Deployment Checklist: /home/ubuntu/booking-care-integration/docs/EC2_DEPLOYMENT_CHECKLIST.md"
echo "  - Scripts README: /home/ubuntu/booking-care-integration/scripts/README.md"
echo ""

echo "🔍 Current Service Status:"
cd /home/ubuntu/booking-care-integration
docker compose ps 2>/dev/null || echo "  Services starting... (check in a few minutes)"
echo ""

echo "📝 Installed Software:"
echo "  - Docker: $(docker --version)"
echo "  - Docker Compose: $(docker compose version)"
echo "  - Git: $(git --version)"
echo ""

echo "═══════════════════════════════════════════════════════════"
echo "Setup log available at: /var/log/user-data.log"
echo "═══════════════════════════════════════════════════════════"
echo ""

# Create a completion marker
touch /var/log/user-data-complete
date > /var/log/user-data-complete-time

echo "Setup completed at: $(date)" >> /var/log/user-data-complete
