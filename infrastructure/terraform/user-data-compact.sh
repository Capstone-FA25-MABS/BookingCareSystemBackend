#!/bin/bash
set -e
exec > >(tee /var/log/user-data.log|logger -t user-data -s 2>/dev/console) 2>&1

echo "BookingCare EC2 Setup Started at $(date)"

# Step 1: Update packages
apt-get update -y && apt-get upgrade -y

# Step 2: Install Docker & Git
apt-get install -y curl wget ca-certificates gnupg lsb-release git
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null
apt-get update -y
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl start docker && systemctl enable docker
usermod -aG docker ubuntu

# Step 3: Clone repository
cd /home/ubuntu
git clone https://github.com/Capstone-FA25-MABS/booking-care-integration.git
chown -R ubuntu:ubuntu booking-care-integration
cd booking-care-integration

# Step 4: System configuration
sysctl -w fs.file-max=65536
echo "fs.file-max = 65536" >> /etc/sysctl.conf
ulimit -n 65536
echo "ubuntu soft nofile 65536" >> /etc/security/limits.conf
echo "ubuntu hard nofile 65536" >> /etc/security/limits.conf
fallocate -l 4G /swapfile && chmod 600 /swapfile && mkswap /swapfile && swapon /swapfile
echo '/swapfile none swap sw 0 0' >> /etc/fstab
sysctl -w vm.max_map_count=262144
echo "vm.max_map_count=262144" >> /etc/sysctl.conf

# Step 5: Setup Docker volume
mkfs -t ext4 /dev/nvme1n1
mkdir -p /var/lib/docker
mount /dev/nvme1n1 /var/lib/docker
echo '/dev/nvme1n1 /var/lib/docker ext4 defaults,nofail 0 2' >> /etc/fstab
systemctl restart docker

# Step 6: Create volumes
chmod +x scripts/create-volumes.sh
./scripts/create-volumes.sh

# Step 7: Restore data if exists
if [ -d "/home/ubuntu/backups" ]; then
    echo "Backup found, restoring..."
    chmod +x scripts/restore-databases.sh
    LATEST_BACKUP=$(ls -td backups/databases/* 2>/dev/null | head -1)
    if [ -n "$LATEST_BACKUP" ]; then
        ./scripts/restore-databases.sh "$LATEST_BACKUP"
    fi
fi

# Step 8: Start application
docker compose up -d
sleep 30
docker compose ps

# Create management scripts
cat > /home/ubuntu/update-app.sh << 'SCRIPT'
#!/bin/bash
cd /home/ubuntu/booking-care-integration
git pull
docker compose pull
docker compose up -d
docker compose ps
SCRIPT

cat > /home/ubuntu/backup-data.sh << 'SCRIPT'
#!/bin/bash
cd /home/ubuntu/booking-care-integration/scripts
./backup-databases.sh
SCRIPT

cat > /home/ubuntu/check-status.sh << 'SCRIPT'
#!/bin/bash
cd /home/ubuntu/booking-care-integration
docker compose ps
df -h
free -h
SCRIPT

chmod +x /home/ubuntu/*.sh
chown ubuntu:ubuntu /home/ubuntu/*.sh

# Configure UFW
ufw --force enable
ufw default deny incoming
ufw default allow outgoing
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 5000:5001/tcp
ufw allow 5173:5174/tcp
ufw allow 3000/tcp
ufw allow 9090/tcp
ufw allow 16686/tcp

echo "✅ DEPLOYMENT COMPLETE at $(date)"
echo "API Gateway: http://$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4):5000"
echo "Frontend User: http://$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4):5173"
echo "Frontend Admin: http://$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4):5174"
echo "Grafana: http://$(curl -s http://169.254.169.254/latest/meta-data/public-ipv4):3000"
