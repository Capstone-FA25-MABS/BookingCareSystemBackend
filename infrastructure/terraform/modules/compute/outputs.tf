output "instance_id" {
  description = "ID of the EC2 instance"
  value       = aws_instance.main.id
}

output "instance_public_ip" {
  description = "Public IP address of the EC2 instance"
  value       = aws_instance.main.public_ip
}

output "instance_private_ip" {
  description = "Private IP address of the EC2 instance"
  value       = aws_instance.main.private_ip
}

output "instance_state" {
  description = "State of the EC2 instance"
  value       = aws_instance.main.instance_state
}

# Elastic IP output - DISABLED
# output "elastic_ip" {
#   description = "Elastic IP address (if enabled)"
#   value       = var.enable_eip ? aws_eip.main[0].public_ip : null
# }

output "elastic_ip" {
  description = "Elastic IP disabled - using dynamic public IP"
  value       = null
}

output "docker_volume_id" {
  description = "ID of the Docker EBS volume"
  value       = aws_ebs_volume.docker.id
}
