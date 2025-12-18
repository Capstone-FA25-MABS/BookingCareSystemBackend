###############################################################################
# EC2 Instance
###############################################################################

resource "aws_instance" "main" {
  ami                    = var.ami_id
  instance_type          = var.instance_type
  key_name               = var.key_name
  subnet_id              = var.subnet_id
  vpc_security_group_ids = [var.security_group_id]
  iam_instance_profile   = var.instance_profile_name

  root_block_device {
    volume_type           = var.root_volume_type
    volume_size           = var.root_volume_size
    iops                  = var.root_volume_type == "gp3" || var.root_volume_type == "io1" || var.root_volume_type == "io2" ? var.root_volume_iops : null
    throughput            = var.root_volume_type == "gp3" ? var.root_volume_throughput : null
    delete_on_termination = true
    encrypted             = true

    tags = merge(
      var.tags,
      {
        Name = "${var.project_name}-${var.environment}-root-volume"
      }
    )
  }

  user_data = file(var.user_data_script)

  monitoring = var.enable_monitoring

  metadata_options {
    http_endpoint               = "enabled"
    http_tokens                 = "required"
    http_put_response_hop_limit = 1
    instance_metadata_tags      = "enabled"
  }

  tags = merge(
    var.tags,
    {
      Name = "${var.project_name}-${var.environment}-ec2"
    }
  )

  lifecycle {
    ignore_changes = [ami, user_data]
  }
}

###############################################################################
# Additional EBS Volume for Docker
###############################################################################

resource "aws_ebs_volume" "docker" {
  availability_zone = var.availability_zone
  size              = var.docker_volume_size
  type              = var.docker_volume_type
  iops              = var.docker_volume_type == "gp3" || var.docker_volume_type == "io1" || var.docker_volume_type == "io2" ? 3000 : null
  throughput        = var.docker_volume_type == "gp3" ? 125 : null
  encrypted         = true

  tags = merge(
    var.tags,
    {
      Name = "${var.project_name}-${var.environment}-docker-volume"
    }
  )
}

resource "aws_volume_attachment" "docker" {
  device_name = "/dev/sdf"
  volume_id   = aws_ebs_volume.docker.id
  instance_id = aws_instance.main.id
}

###############################################################################
# Elastic IP (Optional) - DISABLED TO SAVE COSTS
# Using dynamic public IP instead - IP will change on instance restart
###############################################################################

# resource "aws_eip" "main" {
#   count    = var.enable_eip ? 1 : 0
#   domain   = "vpc"
#   instance = aws_instance.main.id
#
#   tags = merge(
#     var.tags,
#     {
#       Name = "${var.project_name}-${var.environment}-eip"
#     }
#   )
# }
