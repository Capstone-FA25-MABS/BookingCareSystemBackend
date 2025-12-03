# 🚀 Quick Start - Deploy với Free Tier Test

## Triển khai nhanh trong 5 phút

### 1️⃣ Chuẩn bị (1 phút)

```bash
cd /Users/hieumaixuan/Documents/capstone-src/BookingCareSystemBackend/infrastructure/terraform

# Kiểm tra cấu hình hiện tại
./switch-config.sh status
```

### 2️⃣ Chuyển sang Free Tier Test (30 giây)

```bash
# Switch to test config
./switch-config.sh test

# Xác nhận
./switch-config.sh status
```

**Output mong đợi:**
```
Environment: test
Instance Type: t3.micro
Root Volume: 20GB
Docker Volume: 10GB
```

### 3️⃣ Review Infrastructure (1 phút)

```bash
# Xem những gì sẽ được tạo
terraform plan

# Check output
# - 15 resources sẽ được tạo
# - Instance type: t3.micro
# - Total storage: 30GB
```

### 4️⃣ Deploy (30 giây + 5-10 phút setup)

```bash
# Deploy infrastructure
terraform apply

# Gõ: yes
```

**Terraform sẽ tạo:**
- ✅ VPC với public subnet
- ✅ EC2 instance (t3.micro)
- ✅ Security Group với các ports đã mở
- ✅ Elastic IP
- ✅ IAM Role và Instance Profile
- ✅ EBS Volumes (20GB + 10GB)

### 5️⃣ Lấy thông tin kết nối (ngay sau apply)

```bash
# Lấy Elastic IP
terraform output elastic_ip

# Hoặc xem tất cả outputs
terraform output
```

### 6️⃣ Theo dõi quá trình setup (5-10 phút)

```bash
# SSH vào instance (thay <ELASTIC_IP>)
ssh -i ~/.ssh/bookingcare-key.pem ubuntu@<ELASTIC_IP>

# Xem log setup (real-time)
sudo tail -f /var/log/user-data.log

# Chờ đến khi thấy: "BookingCare EC2 Setup Complete!"
```

**User-data script sẽ tự động:**
1. Cài Docker, Git, Node.js, AWS CLI
2. Clone repository từ GitHub
3. Build và start Docker containers
4. Cấu hình firewall

### 7️⃣ Kiểm tra Application (1 phút)

```bash
# Đã SSH vào instance rồi

# Kiểm tra containers đang chạy
cd /home/ubuntu/bookingcare
docker compose ps

# Xem logs
docker compose logs -f

# Kiểm tra system info
/home/ubuntu/system-info.sh
```

### 8️⃣ Test Services

Thay `<ELASTIC_IP>` bằng IP thực tế:

```bash
# API Gateway Health Check
curl http://<ELASTIC_IP>:5000/health

# Mở trong browser:
# - Frontend Client: http://<ELASTIC_IP>:5173
# - Frontend Admin: http://<ELASTIC_IP>:5174
# - RabbitMQ: http://<ELASTIC_IP>:15672 (guest/guest)
# - Grafana: http://<ELASTIC_IP>:3000
# - Prometheus: http://<ELASTIC_IP>:9090
# - Jaeger: http://<ELASTIC_IP>:16686
```

---

## 🔧 Troubleshooting nhanh

### ❌ Lỗi: SSH connection refused
```bash
# Đợi thêm 1-2 phút để instance khởi động
# Kiểm tra security group đã có rule SSH cho IP của bạn

# Xem log từ AWS Console > EC2 > Instance > Actions > Monitor and troubleshoot > Get system log
```

### ❌ Lỗi: User-data không chạy
```bash
# SSH vào
ssh -i ~/.ssh/bookingcare-key.pem ubuntu@<ELASTIC_IP>

# Check log
sudo cat /var/log/user-data.log

# Nếu bị lỗi, chạy deploy script thủ công
/home/ubuntu/deploy.sh
```

### ❌ Lỗi: Docker containers không start
```bash
cd /home/ubuntu/bookingcare

# Xem log lỗi
docker compose logs

# Rebuild
docker compose down
docker compose build --no-cache
docker compose up -d
```

### ❌ Lỗi: Out of memory (t3.micro chỉ có 1GB RAM)
```bash
# Check memory
free -h

# Giải pháp: Script đã tự động tạo 8GB swap
# Hoặc stop một số services không cần thiết

# Xem services nào đang chạy
docker compose ps

# Stop service không cần thiết (example)
docker compose stop <service-name>
```

---

## 🧹 Dọn dẹp sau khi test

### Destroy infrastructure
```bash
cd /Users/hieumaixuan/Documents/capstone-src/BookingCareSystemBackend/infrastructure/terraform

# Destroy tất cả
terraform destroy

# Gõ: yes
```

### Quay lại production config
```bash
# Switch back to production
./switch-config.sh production

# Xác nhận
./switch-config.sh status
```

---

## 📊 Checklist

**Trước khi chạy `terraform apply`:**

- [ ] Đã tạo SSH key pair `bookingcare-key` trong AWS Console (ap-southeast-1)
- [ ] Đã download file `.pem` và `chmod 400 ~/.ssh/bookingcare-key.pem`
- [ ] Đã chạy `./switch-config.sh test`
- [ ] Đã review `terraform plan`
- [ ] Repository GitHub public hoặc accessible

**Sau khi apply thành công:**

- [ ] Đã lưu Elastic IP
- [ ] Đã SSH được vào instance
- [ ] User-data script chạy thành công (check log)
- [ ] Docker containers đang chạy
- [ ] Test được các services qua HTTP

**Trước khi destroy:**

- [ ] Đã backup/export data nếu cần
- [ ] Đã test đầy đủ các tính năng
- [ ] Đã note lại các vấn đề (nếu có)

---

## 💰 Chi phí ước tính

### Free Tier (12 tháng đầu)
- EC2 t3.micro: 750h/month = **FREE** ✅
- EBS 30GB: **FREE** ✅
- Data transfer 100GB: **FREE** ✅
- Elastic IP (running): **FREE** ✅

**Tổng: $0/month** (trong Free Tier)

### Nếu vượt Free Tier
- EC2 t3.micro 24/7: ~$7.50/month
- EBS 30GB: ~$2.40/month
- Data transfer: ~$0-9/month
- **Tổng: ~$10-20/month**

---

## ⏱️ Timeline

| Bước | Thời gian | Mô tả |
|------|-----------|-------|
| 1. Setup | 1 phút | Chuẩn bị và switch config |
| 2. Terraform plan | 30 giây | Review changes |
| 3. Terraform apply | 1-2 phút | Tạo infrastructure |
| 4. User-data script | 5-10 phút | Cài đặt packages, clone repo, start containers |
| 5. Test | 2-3 phút | Verify services |
| **Tổng** | **10-15 phút** | Từ setup đến có application chạy |

---

## 📞 Commands cheat sheet

```bash
# Switch config
./switch-config.sh test           # Chuyển sang test
./switch-config.sh production     # Chuyển sang production
./switch-config.sh status         # Xem config hiện tại
./switch-config.sh diff           # So sánh test vs production

# Terraform
terraform init                    # Khởi tạo (lần đầu)
terraform plan                    # Xem thay đổi
terraform apply                   # Deploy
terraform destroy                 # Xóa tất cả
terraform output                  # Xem outputs
terraform output elastic_ip       # Xem IP cụ thể

# SSH & Debug
ssh -i ~/.ssh/bookingcare-key.pem ubuntu@<IP>
sudo tail -f /var/log/user-data.log
sudo cat /var/log/cloud-init-output.log
cd /home/ubuntu/bookingcare && docker compose logs -f

# Re-deploy app
/home/ubuntu/deploy.sh

# System info
/home/ubuntu/system-info.sh
```

---

**Happy Testing! 🎉**

Nếu có vấn đề, check file `FREE_TIER_TESTING.md` để có hướng dẫn chi tiết hơn.
