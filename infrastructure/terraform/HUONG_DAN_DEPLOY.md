# Hướng Dẫn Triển Khai Hạ Tầng AWS - BookingCare System

## 📋 Mục Lục

1. [Chuẩn Bị Trước Khi Deploy](#1-chuẩn-bị-trước-khi-deploy)
2. [Thiết Lập AWS Account](#2-thiết-lập-aws-account)
3. [Cài Đặt Tools](#3-cài-đặt-tools)
4. [Cấu Hình AWS CLI](#4-cấu-hình-aws-cli)
5. [Tạo SSH Key Pair](#5-tạo-ssh-key-pair)
6. [Cấu Hình Terraform Variables](#6-cấu-hình-terraform-variables)
7. [Deploy Hạ Tầng](#7-deploy-hạ-tầng)
8. [Verify Deployment](#8-verify-deployment)
9. [Deploy Application](#9-deploy-application)
10. [Monitoring & Troubleshooting](#10-monitoring--troubleshooting)

---

## 1. Chuẩn Bị Trước Khi Deploy

### ✅ Checklist Cần Có

- [ ] AWS Account đã tạo và verify
- [ ] Credit card đã link với AWS (để thanh toán)
- [ ] Budget đã được approve (~15 triệu VNĐ/tháng)
- [ ] Domain name (optional, để trỏ DNS)
- [ ] Source code đã commit lên Git repository

### 📊 Budget Planning

```
Chi phí dự kiến:
- Hạ tầng AWS: ~14,783,250 VNĐ/tháng
- Domain + SSL: ~500,000 VNĐ/năm
- Dự phòng: ~2,000,000 VNĐ/tháng
─────────────────────────────────────
TỔNG: ~16,783,250 VNĐ/tháng
```

### ⏰ Thời Gian Dự Kiến

- **Chuẩn bị + Cài đặt tools**: 1-2 giờ
- **Deploy hạ tầng**: 15-20 phút
- **Deploy application**: 30-45 phút
- **Testing & verification**: 1-2 giờ
- **TỔNG**: ~3-5 giờ

---

## 2. Thiết Lập AWS Account

### Bước 2.1: Tạo AWS Account

1. Truy cập: https://aws.amazon.com/
2. Click **"Create an AWS Account"**
3. Điền thông tin:
   - Email address
   - Account name: `bookingcare-production`
   - Password (mạnh, lưu vào password manager)

4. Chọn **Account Type**: Professional
5. Điền thông tin công ty/cá nhân
6. Nhập thông tin credit card (sẽ charge $1 để verify)
7. Verify phone number (OTP)
8. Chọn **Support Plan**: Basic (miễn phí)

### Bước 2.2: Bảo Mật Root Account

```bash
# 1. Enable MFA (Multi-Factor Authentication)
- AWS Console → Account → Security Credentials
- Enable Virtual MFA device (dùng Google Authenticator hoặc Authy)

# 2. Tạo billing alerts
- AWS Console → Billing → Billing Preferences
- ✅ Receive Free Tier Usage Alerts
- ✅ Receive Billing Alerts
```

### Bước 2.3: Tạo IAM User (Không dùng Root account)

```bash
# Trong AWS Console:
1. Services → IAM → Users → Add User
2. User name: terraform-admin
3. ✅ Access key - Programmatic access
4. ✅ Password - AWS Management Console access
5. Permissions: Attach existing policies:
   - AdministratorAccess (cho đơn giản, production nên giới hạn hơn)
6. Download credentials CSV (LƯU KỸ FILE NÀY!)
```

⚠️ **QUAN TRỌNG**: 
- Root account chỉ dùng khi cần thiết
- Sử dụng IAM user cho mọi thao tác hàng ngày
- Enable MFA cho IAM user cũng như root

### Bước 2.4: Thiết Lập Budget Alerts

```bash
# AWS Console → Billing → Budgets → Create budget

1. Budget name: bookingcare-monthly-budget
2. Budget amount: $600 (15 triệu VNĐ)
3. Thresholds:
   - 50% ($300): Email notification
   - 80% ($480): Email notification
   - 100% ($600): Email notification + SMS
   - 120% ($720): Critical alert
4. Email recipients: your-team@email.com
```

---

## 3. Cài Đặt Tools

### Bước 3.1: Cài Đặt Terraform (macOS)

```bash
# Sử dụng Homebrew
brew tap hashicorp/tap
brew install hashicorp/tap/terraform

# Verify installation
terraform version
# Output: Terraform v1.x.x
```

### Bước 3.2: Cài Đặt AWS CLI

```bash
# Cài đặt AWS CLI v2
brew install awscli

# Verify installation
aws --version
# Output: aws-cli/2.x.x
```

### Bước 3.3: Cài Đặt Git (nếu chưa có)

```bash
# Kiểm tra Git
git --version

# Nếu chưa có:
brew install git
```

### Bước 3.4: Cài Đặt VS Code (Optional, nhưng recommended)

```bash
# Download từ: https://code.visualstudio.com/
# Hoặc dùng Homebrew:
brew install --cask visual-studio-code

# Extensions nên cài:
code --install-extension hashicorp.terraform
code --install-extension amazonwebservices.aws-toolkit-vscode
```

---

## 4. Cấu Hình AWS CLI

### Bước 4.1: Configure AWS Credentials

```bash
# Chạy lệnh configure
aws configure

# Nhập thông tin (từ CSV file đã download ở Bước 2.3):
AWS Access Key ID: AKIA****************
AWS Secret Access Key: ****************************************
Default region name: ap-southeast-1
Default output format: json
```

### Bước 4.2: Verify AWS Configuration

```bash
# Test connection
aws sts get-caller-identity

# Output sẽ hiển thị:
# {
#     "UserId": "AIDA...",
#     "Account": "123456789012",
#     "Arn": "arn:aws:iam::123456789012:user/terraform-admin"
# }
```

### Bước 4.3: Set Environment Variables (Optional)

```bash
# Thêm vào ~/.zshrc hoặc ~/.bashrc
export AWS_REGION=ap-southeast-1
export AWS_DEFAULT_REGION=ap-southeast-1
export AWS_PROFILE=default

# Reload shell
source ~/.zshrc
```

---

## 5. Tạo SSH Key Pair

### Bước 5.1: Tạo Key Pair trong AWS

```bash
# Sử dụng AWS CLI
aws ec2 create-key-pair \
  --key-name bookingcare-ec2-key \
  --region ap-southeast-1 \
  --query 'KeyMaterial' \
  --output text > ~/.ssh/bookingcare-ec2-key.pem

# Set permissions (quan trọng!)
chmod 400 ~/.ssh/bookingcare-ec2-key.pem

# Verify key được tạo
aws ec2 describe-key-pairs \
  --region ap-southeast-1 \
  --key-names bookingcare-ec2-key
```

### Bước 5.2: Backup SSH Key

```bash
# Copy key ra thư mục backup
cp ~/.ssh/bookingcare-ec2-key.pem ~/Documents/aws-keys-backup/

# Hoặc lưu vào password manager (1Password, LastPass, etc.)
# Không commit key vào Git!
```

---

## 6. Cấu Hình Terraform Variables

### Bước 6.1: Lấy Ubuntu AMI ID mới nhất

```bash
# Get latest Ubuntu 22.04 AMI for ap-southeast-1
aws ec2 describe-images \
  --region ap-southeast-1 \
  --owners 099720109477 \
  --filters "Name=name,Values=ubuntu/images/hvm-ssd/ubuntu-jammy-22.04-amd64-server-*" \
  --query 'sort_by(Images, &CreationDate)[-1].ImageId' \
  --output text

# Output (ví dụ): ami-0497a974f8d5dcef8
# ami-0b571a36bf02461b4
# LƯU GIÁ TRỊ NÀY!
```

### Bước 6.2: Tạo terraform.tfvars

```bash
# Di chuyển vào thư mục terraform
cd /Users/hieumaixuan/Documents/capstone-src/BookingCareSystemBackend/infrastructure/terraform

# Copy file mẫu
cp terraform.tfvars.example terraform.tfvars

# Mở file để edit
code terraform.tfvars
# Hoặc: nano terraform.tfvars
```

### Bước 6.3: Điền Thông Tin vào terraform.tfvars

```hcl
# ============================================================================
# REQUIRED VARIABLES - BẮT BUỘC PHẢI ĐIỀN
# ============================================================================

# AMI ID lấy từ Bước 6.1
ami_id = "ami-0497a974f8d5dcef8"  # Ubuntu 22.04 LTS (ap-southeast-1)

# SSH Key name đã tạo ở Bước 5.1
key_name = "bookingcare-ec2-key"

# ============================================================================
# OPTIONAL VARIABLES - TÙY CHỈNH NẾU CẦN
# ============================================================================

# Project information
project_name = "bookingcare"
environment  = "production"

# Network configuration
vpc_cidr           = "10.0.0.0/16"
public_subnet_cidr = "10.0.1.0/24"
availability_zone  = "ap-southeast-1a"

# Instance configuration
instance_type = "c5.4xlarge"  # 16 vCPUs, 32GB RAM

# Storage configuration
root_volume_size   = 100  # GB
docker_volume_size = 200  # GB

# Security configuration - THAY ĐỔI NÀY!
allowed_ssh_cidr = ["123.19.165.238/32"]  # Thay YOUR_PUBLIC_IP bằng IP của bạn
allowed_http_cidr = ["0.0.0.0/0"]         # Cho phép mọi IP truy cập web

# Features
enable_eip        = true   # Elastic IP
enable_monitoring = true   # CloudWatch monitoring

# Tags
tags = {
  Project     = "BookingCare"
  Environment = "Production"
  Team        = "DevOps"
  ManagedBy   = "Terraform"
}
```

### Bước 6.4: Lấy Public IP của bạn

```bash
# Lấy public IP hiện tại
curl -s https://checkip.amazonaws.com

# Output (ví dụ): 42.118.XXX.XXX

# Update terraform.tfvars:
allowed_ssh_cidr = ["42.118.XXX.XXX/32"]
```

⚠️ **BẢO MẬT**: Chỉ cho phép IP của bạn SSH, không dùng `0.0.0.0/0`!

---

## 7. Deploy Hạ Tầng

### Bước 7.1: Initialize Terraform

```bash
# Đảm bảo đang ở thư mục terraform
cd /Users/hieumaixuan/Documents/capstone-src/BookingCareSystemBackend/infrastructure/terraform

# Initialize Terraform
terraform init

# Output mong đợi:
# Initializing modules...
# - compute in modules/compute
# - networking in modules/networking
# - security in modules/security
# 
# Initializing the backend...
# Initializing provider plugins...
# - Installing hashicorp/aws v5.x.x...
# 
# Terraform has been successfully initialized!
```

### Bước 7.2: Validate Configuration

```bash
# Kiểm tra syntax
terraform validate

# Output: Success! The configuration is valid.

# Format code (optional)
terraform fmt -recursive
```

### Bước 7.3: Preview Changes (Plan)

```bash
# Xem trước các thay đổi
terraform plan -out=tfplan

# Đọc kỹ output:
# - Số lượng resources sẽ tạo (should be ~15 resources)
# - Chi tiết từng resource
# - Không có errors
```

**⚠️ Nếu `terraform plan` bị treo/không có output:**

```bash
# BƯỚC 1: Kiểm tra AWS credentials
aws sts get-caller-identity
# Nếu lệnh này bị treo → vấn đề network/credentials

# BƯỚC 2: Test AWS connection
aws ec2 describe-regions --region ap-southeast-1
# Nếu timeout → kiểm tra network/firewall/VPN

# BƯỚC 3: Enable debug mode
export TF_LOG=DEBUG
export TF_LOG_PATH=./terraform-debug.log
terraform plan

# Xem log để tìm lỗi
tail -f terraform-debug.log

# BƯỚC 4: Kiểm tra provider configuration
cat provider.tf
# Đảm bảo region đúng: ap-southeast-1

# BƯỚC 5: Clear Terraform cache và reinit
rm -rf .terraform
rm -rf .terraform.lock.hcl
terraform init
terraform plan

# BƯỚC 6: Tắt debug mode sau khi xong
unset TF_LOG
unset TF_LOG_PATH
```

**Nguyên nhân thường gặp:**
1. ❌ AWS credentials chưa configure hoặc sai
2. ❌ Network issue (firewall, VPN blocking AWS API)
3. ❌ AWS region không khả dụng
4. ❌ IAM permissions không đủ
5. ❌ Terraform cache bị corrupt

**Kiểm tra kỹ plan output:**

```
Plan: 15 to add, 0 to change, 0 to destroy.

Resources to be created:
✅ aws_vpc.main
✅ aws_internet_gateway.main
✅ aws_subnet.public
✅ aws_route_table.public
✅ aws_route_table_association.public
✅ aws_security_group.ec2
✅ aws_iam_role.ec2
✅ aws_iam_role_policy_attachment (2x)
✅ aws_iam_role_policy.ec2_s3_access
✅ aws_iam_instance_profile.ec2
✅ aws_instance.main
✅ aws_ebs_volume.docker
✅ aws_volume_attachment.docker
✅ aws_eip.main
```

### Bước 7.4: Apply Configuration (Deploy!)

```bash
# Deploy hạ tầng
terraform apply tfplan

# Terraform sẽ tạo resources theo thứ tự:
# 1. VPC & networking (1-2 phút)
# 2. Security groups & IAM (1-2 phút)
# 3. EC2 instance & volumes (5-10 phút)
# 4. Elastic IP (30 giây)

# Tổng thời gian: ~10-15 phút
```

**Trong quá trình apply:**
- Không tắt terminal
- Không Ctrl+C (trừ khi thực sự cần thiết)
- Theo dõi progress

### Bước 7.5: Lưu Outputs

```bash
# Sau khi apply thành công, xem outputs
terraform output

# Lưu thông tin quan trọng:
terraform output -json > deployment-info.json

# Hoặc lưu từng output:
echo "Public IP: $(terraform output -raw instance_public_ip)" >> deployment-notes.txt
echo "Elastic IP: $(terraform output -raw elastic_ip)" >> deployment-notes.txt
echo "Instance ID: $(terraform output -raw instance_id)" >> deployment-notes.txt
```

---

## 8. Verify Deployment

### Bước 8.1: Kiểm Tra EC2 Instance

```bash
# Lấy instance details
aws ec2 describe-instances \
  --instance-ids $(terraform output -raw instance_id) \
  --region ap-southeast-1

# Kiểm tra instance đang running
aws ec2 describe-instance-status \
  --instance-ids $(terraform output -raw instance_id) \
  --region ap-southeast-1
```

### Bước 8.2: Test SSH Connection

```bash
# Lấy Elastic IP
ELASTIC_IP=$(terraform output -raw elastic_ip)

# SSH vào instance
ssh -i ~/.ssh/bookingcare-ec2-key.pem ubuntu@$ELASTIC_IP

# Nếu connect thành công, chạy:
whoami  # Output: ubuntu
uname -a  # Kiểm tra OS version
df -h  # Kiểm tra disk space
```

**Nếu không connect được:**

```bash
# 1. Kiểm tra security group
aws ec2 describe-security-groups \
  --group-ids $(terraform output -raw security_group_id) \
  --region ap-southeast-1

# 2. Kiểm tra instance state
aws ec2 describe-instances \
  --instance-ids $(terraform output -raw instance_id) \
  --query 'Reservations[0].Instances[0].State' \
  --region ap-southeast-1

# 3. Kiểm tra IP của bạn
curl https://checkip.amazonaws.com
# Đảm bảo IP này có trong allowed_ssh_cidr
```

### Bước 8.3: Verify User Data Script

```bash
# SSH vào instance rồi check cloud-init logs
ssh -i ~/.ssh/bookingcare-ec2-key.pem ubuntu@$ELASTIC_IP

# Trong instance, chạy:
sudo tail -f /var/log/cloud-init-output.log

# Kiểm tra Docker
docker --version  # Docker version 24.x.x
docker compose version  # Docker Compose version 2.x.x

# Kiểm tra disk mount
df -h
# Sẽ thấy /dev/xvdf mounted tại /var/lib/docker (200GB)

# Kiểm tra AWS CLI
aws --version

# Exit khỏi instance
exit
```

### Bước 8.4: Test Endpoints

```bash
# Lấy Elastic IP
ELASTIC_IP=$(terraform output -raw elastic_ip)

# Test các ports (từ local machine)
nc -zv $ELASTIC_IP 22   # SSH
nc -zv $ELASTIC_IP 80   # HTTP
nc -zv $ELASTIC_IP 5000 # API Gateway
nc -zv $ELASTIC_IP 5173 # Frontend Client

# Hoặc dùng curl
curl -I http://$ELASTIC_IP
# Sẽ thấy connection refused (bình thường, chưa deploy app)
```

---

## 9. Deploy Application

### Bước 9.1: Chuẩn Bị Source Code

```bash
# SSH vào instance
ssh -i ~/.ssh/bookingcare-ec2-key.pem ubuntu@$(terraform output -raw elastic_ip)

# Clone repository (trong instance)
cd ~
git clone https://github.com/hiumx/BookingCareSystemBackend.git
cd BookingCareSystemBackend

# Checkout branch cần deploy
git checkout feature/deployment-api_HieuMX  # Hoặc branch production
```

### Bước 9.2: Cấu Hình Environment Variables

```bash
# Tạo file .env cho backend services
cd ~/BookingCareSystemBackend

# Copy environment template
cp .env.example .env

# Edit .env file
nano .env

# Điền các thông tin:
# - Database credentials
# - Redis password
# - RabbitMQ credentials
# - S3 bucket info
# - JWT secrets
# - etc.
```

### Bước 9.3: Chuẩn Bị Database

```bash
# Import database schema
cd ~/BookingCareSystemBackend

# Nếu có SQL dump file
docker compose up -d sqlserver

# Wait for SQL Server to be ready
sleep 30

# Import database
docker exec -i $(docker ps -qf "name=sqlserver") \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourPassword" \
  < docs/databases/booking_care_db_v4.0.0_30082025.sql
```

### Bước 9.4: Build và Start Services

```bash
# Build all services
cd ~/BookingCareSystemBackend
docker compose build

# Start all services
docker compose up -d

# Kiểm tra logs
docker compose logs -f --tail=100

# Kiểm tra services đang chạy
docker compose ps

# Should see all services running:
# - apigateway
# - redis
# - rabbitmq
# - mongodb
# - sqlserver
# - 17 microservices (analytics, ai, appointment, auth, etc.)
```

### Bước 9.5: Deploy Frontend

```bash
# Clone frontend repository
cd ~
git clone https://github.com/hiumx/booking-care-system-ui.git
cd booking-care-system-ui

# Install dependencies
npm install

# Build for production
npm run build

# Serve with nginx or PM2
npm install -g pm2
pm2 start npm --name "frontend-client" -- run preview -- --port 5173 --host
pm2 start npm --name "frontend-admin" -- run preview -- --port 5174 --host

# Save PM2 configuration
pm2 save
pm2 startup
```

### Bước 9.6: Verify Application

```bash
# Test API Gateway
curl http://localhost:5000/health

# Test Frontend
curl http://localhost:5173

# Từ local machine:
ELASTIC_IP=$(terraform output -raw elastic_ip)
curl http://$ELASTIC_IP:5000/health
```

---

## 10. Monitoring & Troubleshooting

### Bước 10.1: Thiết Lập Monitoring

```bash
# Truy cập Grafana
ELASTIC_IP=$(terraform output -raw elastic_ip)
open http://$ELASTIC_IP:3000

# Default credentials:
# Username: admin
# Password: admin (đổi ngay sau lần đầu login)

# Truy cập Prometheus
open http://$ELASTIC_IP:9090

# Truy cập Jaeger (tracing)
open http://$ELASTIC_IP:16686

# RabbitMQ Management
open http://$ELASTIC_IP:15672
# Username: guest
# Password: guest
```

### Bước 10.2: CloudWatch Monitoring

```bash
# View instance metrics
aws cloudwatch get-metric-statistics \
  --namespace AWS/EC2 \
  --metric-name CPUUtilization \
  --dimensions Name=InstanceId,Value=$(terraform output -raw instance_id) \
  --start-time $(date -u -v-1H +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average \
  --region ap-southeast-1
```

### Bước 10.3: Log Monitoring

```bash
# SSH vào instance
ssh -i ~/.ssh/bookingcare-ec2-key.pem ubuntu@$(terraform output -raw elastic_ip)

# View Docker logs
docker compose logs -f --tail=100 apigateway
docker compose logs -f --tail=100 appointment-service
docker compose logs -f --tail=100 auth-service

# View system logs
sudo tail -f /var/log/syslog
sudo journalctl -u docker -f
```

### Bước 10.4: Common Issues & Solutions

#### Issue 0: Terraform plan/apply bị treo

```bash
# Triệu chứng: Lệnh chạy mãi không có output

# GIẢI PHÁP 1: Kiểm tra AWS credentials
aws sts get-caller-identity
# Phải có output hiển thị UserId, Account, Arn
# Nếu không → chạy: aws configure

# GIẢI PHÁP 2: Test network connectivity
curl -I https://ec2.ap-southeast-1.amazonaws.com
# HTTP/1.1 403 Forbidden (OK - có kết nối)
# Timeout → vấn đề network/firewall

# GIẢI PHÁP 3: Kiểm tra VPN/Proxy
# Nếu đang dùng VPN công ty, thử:
unset HTTP_PROXY
unset HTTPS_PROXY
unset http_proxy
unset https_proxy

# GIẢI PHÁP 4: Verify IAM permissions
aws iam get-user
# Phải có output về user info
# Nếu Access Denied → IAM user thiếu quyền

# GIẢI PHÁP 5: Clean và reinit
cd infrastructure/terraform
rm -rf .terraform .terraform.lock.hcl
terraform init
terraform plan -out=tfplan

# GIẢI PHÁP 6: Chạy với debug để xem lỗi
export TF_LOG=TRACE
terraform plan 2>&1 | tee debug.log
# Xem file debug.log để tìm lỗi cụ thể
unset TF_LOG

# GIẢI PHÁP 7: Timeout khi query AWS API
# Thêm vào ~/.aws/config:
[default]
region = ap-southeast-1
output = json
cli_read_timeout = 300
cli_connect_timeout = 60
```

#### Issue 1: Không SSH được vào instance

```bash
# Solution:
1. Kiểm tra security group có rule cho port 22
2. Kiểm tra IP của bạn có trong allowed_ssh_cidr
3. Kiểm tra key permission: chmod 400 ~/.ssh/bookingcare-ec2-key.pem
4. Kiểm tra instance đang running
```

#### Issue 2: Services không start

```bash
# Solution:
1. Kiểm tra Docker daemon: sudo systemctl status docker
2. Kiểm tra disk space: df -h
3. Kiểm tra Docker volumes: docker volume ls
4. Xem logs: docker compose logs -f
5. Restart services: docker compose restart
```

#### Issue 3: High CPU/Memory

```bash
# Solution:
1. Kiểm tra resource usage: docker stats
2. Scale down non-critical services
3. Optimize application code
4. Consider upgrading instance type
```

#### Issue 4: Chi phí cao hơn dự kiến

```bash
# Solution:
1. Check AWS Cost Explorer
2. Kiểm tra Data Transfer (có thể cao)
3. Stop/terminate unused resources
4. Enable cost optimization recommendations
```

---

## 📋 Post-Deployment Checklist

### Bảo Mật

- [ ] Đổi default passwords (Grafana, RabbitMQ, Database)
- [ ] Enable HTTPS với Let's Encrypt/AWS Certificate Manager
- [ ] Giới hạn SSH access chỉ cho IP cố định
- [ ] Enable AWS GuardDuty (threat detection)
- [ ] Thiết lập AWS Secrets Manager cho sensitive data
- [ ] Enable VPC Flow Logs
- [ ] Review IAM permissions (principle of least privilege)

### Backup & Disaster Recovery

- [ ] Thiết lập automated snapshots cho EBS volumes
- [ ] Backup database định kỳ (daily)
- [ ] Test restore procedure
- [ ] Document disaster recovery plan
- [ ] Thiết lập Cross-Region backup (optional)

### Monitoring & Alerts

- [ ] Thiết lập CloudWatch alarms:
  - CPU > 80%
  - Memory > 85%
  - Disk > 80%
  - HTTP 5xx errors
- [ ] Thiết lập SNS notifications
- [ ] Configure log aggregation
- [ ] Set up uptime monitoring (UptimeRobot, Pingdom)

### Performance

- [ ] Enable CloudFront CDN cho static assets
- [ ] Configure auto-scaling (nếu cần)
- [ ] Optimize database queries
- [ ] Enable Redis caching
- [ ] Review and optimize Docker images

### Documentation

- [ ] Document deployment process
- [ ] Create runbook cho common issues
- [ ] Document API endpoints
- [ ] Create architecture diagram
- [ ] Document backup/restore procedures

---

## 🆘 Support & Resources

### AWS Support

- **Basic Support**: Miễn phí (documentation, forums)
- **Developer Support**: $29/tháng (business hours support)
- **Business Support**: $100/tháng (24/7 phone/chat support)

### Useful Links

- [AWS Console](https://console.aws.amazon.com/)
- [AWS Cost Calculator](https://calculator.aws/)
- [Terraform AWS Provider Docs](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)
- [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/)

### Emergency Contacts

```
DevOps Team: [email/slack channel]
AWS Support: https://console.aws.amazon.com/support/
On-call Engineer: [phone number]
```

---

## 🎉 Congratulations!

Nếu bạn đã hoàn thành tất cả các bước trên, hệ thống BookingCare của bạn đã sẵn sàng chạy trên AWS!

### Next Steps:

1. **Testing**: Thực hiện integration testing và load testing
2. **Performance Tuning**: Optimize dựa trên metrics
3. **Security Audit**: Review lại security configuration
4. **Documentation**: Cập nhật documentation
5. **Monitoring**: Theo dõi hệ thống 24/7 trong tuần đầu

### Maintenance Schedule

- **Daily**: Check monitoring dashboards
- **Weekly**: Review CloudWatch metrics và costs
- **Monthly**: Update dependencies, security patches
- **Quarterly**: Disaster recovery drill, capacity planning

---

**Good luck! 🚀**

Nếu gặp vấn đề, tham khảo phần Troubleshooting hoặc liên hệ AWS Support.
