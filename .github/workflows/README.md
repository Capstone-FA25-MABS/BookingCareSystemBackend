# GitHub Actions Workflows

Dự án này có 2 workflows chính:

## 1. Quality Check Pipeline (`quality-check.yml`)

**Mục đích**: Kiểm tra chất lượng code mỗi khi có push hoặc PR vào nhánh `main` hoặc `develop`.

**Chức năng**:
- Format check
- Build solution
- Run unit tests
- Upload coverage reports

**Trigger**:
- Push to `main` hoặc `develop`
- Pull request to `main` hoặc `develop`

## 2. Deployment Pipeline (`deployment.yml`)

**Mục đích**: Deploy các microservices lên EC2 khi merge vào nhánh `release`.

### Workflow Steps:

#### Step 1: Detect Changes
- Sử dụng `dorny/paths-filter` để detect service nào có thay đổi code
- Kiểm tra cả thư mục service cụ thể và thư mục `Shared`
- Output: danh sách services có thay đổi

#### Step 2: Build and Push
- **Chỉ build** các services có thay đổi code
- Generate semantic version tự động (format: `X.Y.Z`)
- Version tăng dần mỗi lần deploy (increment patch version)
- Build Docker image với version mới
- Push lên Amazon ECR với 2 tags:
  - `bookingcare-{service}:{version}` (ví dụ: `bookingcare-user:1.0.5`)
  - `bookingcare-{service}:latest`
- Tạo Git tag tương ứng (ví dụ: `user-v1.0.5`)

#### Step 3: Deploy
- **Chỉ deploy** các services có thay đổi code
- SSH vào EC2 instance
- Login vào ECR
- Pull image version mới nhất
- Stop container cũ
- Run container mới với image version mới
- Cleanup old images (giữ lại 3 versions gần nhất)
- Verify deployment thành công

#### Step 4: Deployment Summary
- Generate deployment report
- Hiển thị danh sách services đã deploy và skip

### Versioning Strategy

**Semantic Versioning**: `MAJOR.MINOR.PATCH`

- **MAJOR**: Thay đổi breaking changes (manual increment)
- **MINOR**: Thêm feature mới backward-compatible (manual increment)
- **PATCH**: Bug fixes và small changes (auto-increment)

**Auto-increment logic**:
1. Lấy tag gần nhất của service (format: `{service}-vX.Y.Z`)
2. Nếu không có tag, bắt đầu từ `1.0.0`
3. Nếu có tag, increment PATCH version
4. Tạo tag mới và push lên repository

**Ví dụ**:
```
user-v1.0.0 → user-v1.0.1 → user-v1.0.2
auth-v1.0.0 → auth-v1.0.1
```

### Optimization Features

#### 1. Selective Build & Deploy
Chỉ build và deploy services có thay đổi code:

```yaml
# Service Auth có thay đổi → Build + Deploy
# Service User không thay đổi → Skip

Changes detected:
- src/Services/BookingCare.Services.Auth/**  ✅
- src/Services/BookingCare.Services.User/**  ⏭️
```

#### 2. Shared Code Detection
Nếu code trong `src/Shared/**` thay đổi → **TẤT CẢ services** sẽ được build và deploy:

```yaml
filters: |
  auth:
    - 'src/Services/BookingCare.Services.Auth/**'
    - 'src/Shared/**'  # Shared libraries
```

#### 3. Image Cleanup
Tự động xóa old images trên EC2, chỉ giữ lại 3 versions gần nhất để tiết kiệm disk space.

## Required GitHub Secrets

Cần configure các secrets sau trong GitHub repository:

### AWS Credentials
```
AWS_ACCESS_KEY_ID         # AWS Access Key
AWS_SECRET_ACCESS_KEY     # AWS Secret Key
```

### EC2 Credentials
```
EC2_HOST                  # EC2 instance public IP hoặc hostname
EC2_USER                  # SSH username (thường là 'ec2-user' hoặc 'ubuntu')
EC2_SSH_KEY              # Private SSH key để connect tới EC2
```

## EC2 Setup Requirements

Trên EC2 instance cần có:

1. **Docker** đã được cài đặt và running
2. **AWS CLI** đã được cài đặt và configured
3. **Docker Network** đã được tạo:
   ```bash
   docker network create bookingcare-network
   ```
4. **ECR Login Permission**: EC2 instance cần có IAM role với quyền pull images từ ECR

### Setup EC2 Instance

```bash
# 1. Install Docker
sudo yum update -y
sudo yum install docker -y
sudo service docker start
sudo usermod -a -G docker ec2-user

# 2. Install AWS CLI
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install

# 3. Configure AWS credentials (nếu không dùng IAM role)
aws configure

# 4. Create Docker network
docker network create bookingcare-network

# 5. Test ECR login
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin {ECR_REGISTRY}
```

## ECR Repositories Setup

Cần tạo ECR repositories cho tất cả services:

```bash
# Danh sách repositories cần tạo:
bookingcare-apigateway
bookingcare-ai
bookingcare-analytics
bookingcare-appointment
bookingcare-auth
bookingcare-blog
bookingcare-communication
bookingcare-content
bookingcare-discount
bookingcare-doctor
bookingcare-favorites
bookingcare-hospital
bookingcare-notification
bookingcare-payment
bookingcare-review
bookingcare-saga
bookingcare-schedule
bookingcare-servicemedical
bookingcare-user
```

### Tạo repositories bằng AWS CLI:

```bash
#!/bin/bash
SERVICES=(
  "apigateway" "ai" "analytics" "appointment" "auth" 
  "blog" "communication" "content" "discount" "doctor" 
  "favorites" "hospital" "notification" "payment" "review" 
  "saga" "schedule" "servicemedical" "user"
)

for service in "${SERVICES[@]}"; do
  aws ecr create-repository \
    --repository-name "bookingcare-$service" \
    --region us-east-1
done
```

## Trigger Deployment

### Automatic Trigger
Deploy tự động khi merge vào nhánh `release`:

```bash
git checkout release
git merge develop
git push origin release
```

### Manual Trigger
Có thể trigger manually qua GitHub UI:
1. Vào tab "Actions"
2. Chọn "Deployment Pipeline"
3. Click "Run workflow"
4. Chọn branch `release`

## Monitoring Deployment

### GitHub Actions UI
- Xem real-time logs của từng job
- Check deployment summary
- Review services đã deploy/skip

### EC2 Instance
SSH vào EC2 để check containers:

```bash
# List running containers
docker ps

# View container logs
docker logs bookingcare-{service}

# Check container health
docker inspect bookingcare-{service}
```

## Rollback Strategy

Nếu cần rollback về version cũ:

```bash
# 1. SSH vào EC2
ssh -i {key.pem} ec2-user@{EC2_HOST}

# 2. Stop container hiện tại
docker stop bookingcare-{service}
docker rm bookingcare-{service}

# 3. Pull version cũ từ ECR
docker pull {ECR_REGISTRY}/bookingcare-{service}:{old-version}

# 4. Run container với version cũ
docker run -d \
  --name bookingcare-{service} \
  --restart unless-stopped \
  --network bookingcare-network \
  {ECR_REGISTRY}/bookingcare-{service}:{old-version}
```

## Troubleshooting

### Build Failed
- Check Dockerfile syntax
- Verify dependencies trong `.csproj`
- Check build logs trong GitHub Actions

### Push to ECR Failed
- Verify AWS credentials
- Check ECR repository đã được tạo
- Verify IAM permissions

### Deploy to EC2 Failed
- Check EC2 SSH credentials
- Verify EC2 instance đang running
- Check Docker service trên EC2
- Verify network connectivity

### Container Not Running
```bash
# View container logs
docker logs bookingcare-{service}

# Check container exit code
docker inspect bookingcare-{service} --format='{{.State.ExitCode}}'

# Restart container
docker restart bookingcare-{service}
```

## Best Practices

1. **Testing trước khi merge**: Luôn test kỹ trên nhánh `develop` trước khi merge vào `release`
2. **Incremental deployment**: Deploy từng service một thay vì deploy all cùng lúc
3. **Monitor logs**: Luôn check logs sau khi deploy
4. **Backup database**: Backup database trước khi deploy nếu có DB migrations
5. **Health checks**: Implement health check endpoints cho mỗi service
6. **Gradual rollout**: Deploy lên development environment trước, sau đó mới lên production

## Notes

- Workflow sử dụng `dorny/paths-filter@v2` để detect changes
- Docker cache được sử dụng để tăng tốc độ build
- Git tags được tạo tự động cho mỗi deployment
- Old Docker images trên EC2 được cleanup tự động (giữ lại 3 versions)
