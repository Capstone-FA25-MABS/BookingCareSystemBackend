variable "aws_region" {
  description = "AWS region to deploy resources"
  type        = string
  default     = "ap-southeast-1"
}

variable "environment" {
  description = "Environment name (dev, staging, production)"
  type        = string
  default     = "production"
}

variable "project_name" {
  description = "Project name for resource naming"
  type        = string
  default     = "bookingcare"
}

variable "instance_type" {
  description = "EC2 instance type"
  type        = string
  default     = "c5.4xlarge"
}

variable "ami_id" {
  description = "AMI ID for EC2 instance (Ubuntu 22.04 LTS)"
  type        = string
  # Ubuntu 22.04 LTS in ap-southeast-1
  default = "ami-0c55b159cbfafe1f0"
}

variable "key_name" {
  description = "SSH key pair name for EC2 access"
  type        = string
}

variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidr" {
  description = "CIDR block for public subnet"
  type        = string
  default     = "10.0.1.0/24"
}

variable "availability_zone" {
  description = "Availability zone for resources"
  type        = string
  default     = "ap-southeast-1a"
}

variable "root_volume_size" {
  description = "Size of root EBS volume in GB"
  type        = number
  default     = 100
}

variable "root_volume_type" {
  description = "Type of root EBS volume"
  type        = string
  default     = "gp3"
}

variable "root_volume_iops" {
  description = "IOPS for root EBS volume (only for gp3, io1, io2)"
  type        = number
  default     = 3000
}

variable "root_volume_throughput" {
  description = "Throughput for root EBS volume in MB/s (only for gp3)"
  type        = number
  default     = 125
}

variable "docker_volume_size" {
  description = "Size of additional EBS volume for Docker data in GB"
  type        = number
  default     = 200
}

variable "docker_volume_type" {
  description = "Type of Docker EBS volume"
  type        = string
  default     = "gp3"
}

variable "allowed_ssh_cidr" {
  description = "CIDR blocks allowed to SSH to EC2"
  type        = list(string)
  default     = ["0.0.0.0/0"] # Change this to your IP for security
}

variable "allowed_http_cidr" {
  description = "CIDR blocks allowed to access HTTP/HTTPS"
  type        = list(string)
  default     = ["0.0.0.0/0"]
}

variable "enable_monitoring" {
  description = "Enable detailed CloudWatch monitoring"
  type        = bool
  default     = true
}

variable "enable_eip" {
  description = "Allocate Elastic IP for EC2 instance"
  type        = bool
  default     = true
}

variable "tags" {
  description = "Additional tags for resources"
  type        = map(string)
  default     = {}
}
