variable "project_name" {
  description = "Project name for resource naming"
  type        = string
}

variable "environment" {
  description = "Environment name (dev, staging, production)"
  type        = string
}

variable "instance_type" {
  description = "EC2 instance type"
  type        = string
}

variable "ami_id" {
  description = "AMI ID for EC2 instance"
  type        = string
}

variable "key_name" {
  description = "SSH key pair name for EC2 access"
  type        = string
}

variable "subnet_id" {
  description = "ID of the subnet to launch instance in"
  type        = string
}

variable "security_group_id" {
  description = "ID of the security group"
  type        = string
}

variable "instance_profile_name" {
  description = "Name of the IAM instance profile"
  type        = string
}

variable "availability_zone" {
  description = "Availability zone for resources"
  type        = string
}

variable "root_volume_size" {
  description = "Size of root EBS volume in GB"
  type        = number
}

variable "root_volume_type" {
  description = "Type of root EBS volume"
  type        = string
}

variable "root_volume_iops" {
  description = "IOPS for root EBS volume"
  type        = number
}

variable "root_volume_throughput" {
  description = "Throughput for root EBS volume in MB/s"
  type        = number
}

variable "docker_volume_size" {
  description = "Size of Docker EBS volume in GB"
  type        = number
}

variable "docker_volume_type" {
  description = "Type of Docker EBS volume"
  type        = string
}

variable "enable_monitoring" {
  description = "Enable detailed CloudWatch monitoring"
  type        = bool
}

variable "enable_eip" {
  description = "Allocate Elastic IP for EC2 instance"
  type        = bool
}

variable "user_data_script" {
  description = "User data script path"
  type        = string
}

variable "tags" {
  description = "Additional tags for resources"
  type        = map(string)
  default     = {}
}
