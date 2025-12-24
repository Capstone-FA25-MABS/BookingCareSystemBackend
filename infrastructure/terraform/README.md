# BookingCare System - Terraform Infrastructure (Modular)

This directory contains the modular Terraform Infrastructure as Code (IaC) configuration for deploying the BookingCare microservices system to AWS EC2.

## 📁 Project Structure

```
terraform/
├── main.tf                      # Root module - orchestrates child modules
├── variables.tf                 # Root module input variables
├── outputs.tf                   # Root module outputs
├── provider.tf                  # AWS provider configuration
├── terraform.tfvars.example     # Example variable values
├── user-data.sh                 # EC2 bootstrap script
├── README.md                    # This file
└── modules/
    ├── networking/              # VPC, Subnets, Internet Gateway
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    ├── security/                # Security Groups, IAM Roles
    │   ├── main.tf
    │   ├── variables.tf
    │   └── outputs.tf
    └── compute/                 # EC2 Instance, EBS Volumes
        ├── main.tf
        ├── variables.tf
        └── outputs.tf
```

## 🏗️ Module Architecture

### 1. Networking Module (`modules/networking/`)

Manages network infrastructure:

- **VPC**: Custom VPC with DNS support (10.0.0.0/16)
- **Internet Gateway**: For public internet access
- **Public Subnet**: Single AZ deployment with auto-assign public IP
- **Route Tables**: Routes traffic to internet gateway

**Outputs:**
- `vpc_id`: VPC identifier
- `public_subnet_id`: Public subnet identifier
- `internet_gateway_id`: IGW identifier

### 2. Security Module (`modules/security/`)

Manages security and access control:

**Security Groups:**
- SSH (22), HTTP (80), HTTPS (443)
- API Gateway (5000-5001)
- Frontend Client/Admin (5173-5174)
- Microservices HTTP (6000-6020)
- Microservices gRPC (6100-6120)
- Monitoring: Grafana (3000), Prometheus (9090), Jaeger (16686)
- RabbitMQ Management (15672)

**IAM Configuration:**
- EC2 Instance Role with managed policies:
  - AmazonSSMManagedInstanceCore
  - CloudWatchAgentServerPolicy
- S3 bucket access policy for application data

**Outputs:**
- `security_group_id`: EC2 security group ID
- `instance_profile_name`: IAM instance profile name

### 3. Compute Module (`modules/compute/`)

Manages compute resources:

- **EC2 Instance**: c5.4xlarge (16 vCPUs, 32GB RAM)
- **Root EBS Volume**: 100GB gp3 (3000 IOPS, 125 MB/s)
- **Docker EBS Volume**: 200GB gp3 (3000 IOPS, 125 MB/s)
- **Elastic IP** (optional): Static public IP address
- **CloudWatch Monitoring**: Detailed monitoring enabled

**Outputs:**
- `instance_id`: EC2 instance identifier
- `instance_public_ip`: Public IP address
- `instance_private_ip`: Private IP address
- `elastic_ip`: Elastic IP (if enabled)
- `docker_volume_id`: Docker volume identifier

## 🚀 Quick Start

### Prerequisites

1. **Terraform**: >= 1.0
   ```bash
   terraform version
   ```

2. **AWS CLI**: Configured with credentials
   ```bash
   aws configure
   ```

3. **SSH Key Pair**: Create in AWS EC2 console (Singapore region)
   ```bash
   aws ec2 create-key-pair \
     --key-name bookingcare-ec2-key \
     --region ap-southeast-1 \
     --query 'KeyMaterial' \
     --output text > ~/.ssh/bookingcare-ec2-key.pem
   chmod 400 ~/.ssh/bookingcare-ec2-key.pem
   ```

4. **AMI ID**: Get latest Ubuntu 22.04 LTS AMI
   ```bash
   aws ec2 describe-images \
     --region ap-southeast-1 \
     --owners 099720109477 \
     --filters "Name=name,Values=ubuntu/images/hvm-ssd/ubuntu-jammy-22.04-amd64-server-*" \
     --query 'sort_by(Images, &CreationDate)[-1].ImageId' \
     --output text
   ```

### Deployment Steps

1. **Initialize Terraform**
   ```bash
   cd infrastructure/terraform
   terraform init
   ```

2. **Create Configuration File**
   ```bash
   cp terraform.tfvars.example terraform.tfvars
   # Edit terraform.tfvars with your values
   ```

3. **Validate Configuration**
   ```bash
   terraform validate
   terraform fmt -recursive
   ```

4. **Plan Deployment**
   ```bash
   terraform plan -out=tfplan
   ```

5. **Apply Configuration**
   ```bash
   terraform apply tfplan
   ```

6. **Get Outputs**
   ```bash
   terraform output
   ```

## 📋 Configuration Variables

### Required Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `ami_id` | Ubuntu 22.04 LTS AMI ID | `ami-0497a974f8d5dcef8` |
| `key_name` | EC2 SSH key pair name | `bookingcare-ec2-key` |

### Optional Variables (with defaults)

| Variable | Default | Description |
|----------|---------|-------------|
| `project_name` | `bookingcare` | Project identifier |
| `environment` | `production` | Environment name |
| `aws_region` | `ap-southeast-1` | AWS region (Singapore) |
| `vpc_cidr` | `10.0.0.0/16` | VPC CIDR block |
| `public_subnet_cidr` | `10.0.1.0/24` | Public subnet CIDR |
| `availability_zone` | `ap-southeast-1a` | Availability zone |
| `instance_type` | `c5.4xlarge` | EC2 instance type |
| `root_volume_size` | `100` | Root volume size (GB) |
| `docker_volume_size` | `200` | Docker volume size (GB) |
| `enable_eip` | `true` | Allocate Elastic IP |
| `enable_monitoring` | `true` | Detailed CloudWatch monitoring |
| `allowed_ssh_cidr` | `["0.0.0.0/0"]` | Allowed SSH CIDR blocks |
| `allowed_http_cidr` | `["0.0.0.0/0"]` | Allowed HTTP CIDR blocks |

## 🔧 Module Usage Examples

### Using Individual Modules

You can use modules independently in other Terraform configurations:

```hcl
# Use networking module
module "network" {
  source = "./modules/networking"
  
  project_name       = "myproject"
  environment        = "dev"
  vpc_cidr           = "10.0.0.0/16"
  public_subnet_cidr = "10.0.1.0/24"
  availability_zone  = "ap-southeast-1a"
  tags               = { Terraform = "true" }
}

# Use security module
module "security" {
  source = "./modules/security"
  
  project_name      = "myproject"
  environment       = "dev"
  vpc_id            = module.network.vpc_id
  allowed_ssh_cidr  = ["1.2.3.4/32"]
  allowed_http_cidr = ["0.0.0.0/0"]
  tags              = { Terraform = "true" }
}

# Use compute module
module "ec2" {
  source = "./modules/compute"
  
  project_name          = "myproject"
  environment           = "dev"
  instance_type         = "t3.large"
  ami_id                = "ami-xxxxx"
  key_name              = "my-key"
  subnet_id             = module.network.public_subnet_id
  security_group_id     = module.security.security_group_id
  instance_profile_name = module.security.instance_profile_name
  availability_zone     = "ap-southeast-1a"
  root_volume_size      = 50
  docker_volume_size    = 100
  enable_eip            = true
  enable_monitoring     = true
  user_data_script      = "${path.module}/user-data.sh"
  tags                  = { Terraform = "true" }
}
```

## 📊 Infrastructure Outputs

After successful deployment:

```
Outputs:

# Networking
vpc_id = "vpc-xxxxxxxxxxxxx"
subnet_id = "subnet-xxxxxxxxxxxxx"

# Security
security_group_id = "sg-xxxxxxxxxxxxx"
instance_profile_name = "bookingcare-production-ec2-profile"

# Compute
instance_id = "i-xxxxxxxxxxxxx"
instance_public_ip = "X.X.X.X"
instance_private_ip = "10.0.1.X"
elastic_ip = "X.X.X.X"
docker_volume_id = "vol-xxxxxxxxxxxxx"

# Connection & URLs
ssh_connection_command = "ssh -i ~/.ssh/bookingcare-ec2-key.pem ubuntu@X.X.X.X"
api_gateway_url = "http://X.X.X.X:5000"
frontend_client_url = "http://X.X.X.X:5173"
frontend_admin_url = "http://X.X.X.X:5174"
grafana_url = "http://X.X.X.X:3000"
prometheus_url = "http://X.X.X.X:9090"
jaeger_url = "http://X.X.X.X:16686"
rabbitmq_management_url = "http://X.X.X.X:15672"
```

## 🔒 Security Considerations

1. **SSH Access**: Restrict `allowed_ssh_cidr` to your IP address only
   ```hcl
   allowed_ssh_cidr = ["YOUR_IP/32"]
   ```

2. **Secrets Management**: Use AWS Secrets Manager or Parameter Store
   ```bash
   aws secretsmanager create-secret \
     --name bookingcare/db/password \
     --secret-string "your-password"
   ```

3. **Encryption**: All EBS volumes are encrypted at rest
4. **IMDSv2**: Metadata service uses token-based authentication
5. **IAM Roles**: EC2 uses IAM roles instead of access keys

## 📈 Cost Estimation

Approximate monthly costs (ap-southeast-1 region):

| Resource | Specification | Monthly Cost |
|----------|---------------|--------------|
| EC2 c5.4xlarge | 16 vCPU, 32GB RAM | ~$280 |
| EBS gp3 (300GB) | 100GB + 200GB | ~$30 |
| Elastic IP | 1 IP (attached) | $0 |
| Data Transfer | First 100GB free | Variable |
| **Total** | | **~$310/month** |

## 🧹 Cleanup

To destroy all resources:

```bash
terraform destroy
```

**Warning**: This will permanently delete:
- EC2 instance
- EBS volumes (including all data)
- Elastic IP
- Security groups
- VPC and networking resources

## 📝 Customization

### Change Instance Type

```hcl
instance_type = "c5.2xlarge"  # 8 vCPUs, 16GB RAM
```

### Adjust Storage

```hcl
root_volume_size   = 150  # GB
docker_volume_size = 300  # GB
```

### Disable Elastic IP

```hcl
enable_eip = false
```

### Add Custom Tags

```hcl
tags = {
  Project     = "BookingCare"
  Environment = "Production"
  Team        = "DevOps"
  CostCenter  = "Engineering"
}
```

## 🐛 Troubleshooting

### Module Not Found

```bash
terraform init -upgrade
```

### Permission Denied Errors

Ensure your AWS credentials have permissions for:
- EC2: Full access
- VPC: Full access
- IAM: CreateRole, AttachRolePolicy
- S3: GetObject, PutObject

### Instance Not Accessible

1. Check security group rules
2. Verify Elastic IP association
3. Check route table configuration
4. Review user-data logs: `/var/log/cloud-init-output.log`

## 📚 Additional Resources

- [Terraform AWS Provider Documentation](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)
- [AWS EC2 Instance Types](https://aws.amazon.com/ec2/instance-types/)
- [Terraform Module Development](https://developer.hashicorp.com/terraform/language/modules/develop)
- [AWS VPC Best Practices](https://docs.aws.amazon.com/vpc/latest/userguide/vpc-security-best-practices.html)

## 🤝 Contributing

When modifying this infrastructure:

1. Update module documentation
2. Run `terraform fmt -recursive` before committing
3. Run `terraform validate` to check syntax
4. Document breaking changes
5. Update this README

## 📞 Support

For issues:
- Check Terraform plan output
- Review AWS CloudWatch logs
- Check EC2 user-data logs: `/var/log/cloud-init-output.log`
- Verify security group and IAM permissions

---

**Version**: 2.0.0 (Modular Architecture)  
**Terraform Version**: >= 1.0  
**AWS Provider Version**: ~> 5.0  
**Last Updated**: 2025-01-XX
