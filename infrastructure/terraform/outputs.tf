###############################################################################
# Networking Outputs
###############################################################################

output "vpc_id" {
  description = "ID of the VPC"
  value       = module.networking.vpc_id
}

output "subnet_id" {
  description = "ID of the public subnet"
  value       = module.networking.public_subnet_id
}

###############################################################################
# Security Outputs
###############################################################################

output "security_group_id" {
  description = "ID of the EC2 security group"
  value       = module.security.security_group_id
}

output "instance_profile_name" {
  description = "Name of the IAM instance profile"
  value       = module.security.instance_profile_name
}

###############################################################################
# Compute Outputs
###############################################################################

output "instance_id" {
  description = "ID of the EC2 instance"
  value       = module.compute.instance_id
}

output "instance_public_ip" {
  description = "Public IP address of the EC2 instance"
  value       = module.compute.instance_public_ip
}

output "instance_private_ip" {
  description = "Private IP address of the EC2 instance"
  value       = module.compute.instance_private_ip
}

output "elastic_ip" {
  description = "Elastic IP address (if enabled)"
  value       = module.compute.elastic_ip
}

output "instance_state" {
  description = "State of the EC2 instance"
  value       = module.compute.instance_state
}

output "docker_volume_id" {
  description = "ID of the Docker EBS volume"
  value       = module.compute.docker_volume_id
}

###############################################################################
# Connection Information
###############################################################################

output "ssh_connection_command" {
  description = "SSH command to connect to the instance"
  value       = "ssh -i ~/.ssh/${var.key_name}.pem ubuntu@${module.compute.elastic_ip != null ? module.compute.elastic_ip : module.compute.instance_public_ip}"
}

###############################################################################
# Service URLs
###############################################################################

output "api_gateway_url" {
  description = "API Gateway URL"
  value       = "http://${module.compute.elastic_ip != null ? module.compute.elastic_ip : module.compute.instance_public_ip}:5000"
}

output "frontend_client_url" {
  description = "Frontend Client URL"
  value       = "http://${module.compute.elastic_ip != null ? module.compute.elastic_ip : module.compute.instance_public_ip}:5173"
}

output "frontend_admin_url" {
  description = "Frontend Admin URL"
  value       = "http://${module.compute.elastic_ip != null ? module.compute.elastic_ip : module.compute.instance_public_ip}:5174"
}

output "grafana_url" {
  description = "Grafana Dashboard URL"
  value       = "http://${module.compute.elastic_ip != null ? module.compute.elastic_ip : module.compute.instance_public_ip}:3000"
}

output "prometheus_url" {
  description = "Prometheus URL"
  value       = "http://${module.compute.elastic_ip != null ? module.compute.elastic_ip : module.compute.instance_public_ip}:9090"
}

output "jaeger_url" {
  description = "Jaeger UI URL"
  value       = "http://${module.compute.elastic_ip != null ? module.compute.elastic_ip : module.compute.instance_public_ip}:16686"
}

output "rabbitmq_management_url" {
  description = "RabbitMQ Management UI URL"
  value       = "http://${module.compute.elastic_ip != null ? module.compute.elastic_ip : module.compute.instance_public_ip}:15672"
}

