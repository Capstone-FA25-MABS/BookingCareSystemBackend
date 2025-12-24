###############################################################################
# Security Group for EC2
###############################################################################

resource "aws_security_group" "ec2" {
  name        = "${var.project_name}-${var.environment}-ec2-sg"
  description = "Security group for BookingCare EC2 instance"
  vpc_id      = var.vpc_id

  # SSH access
  ingress {
    description = "SSH from allowed IPs"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = var.allowed_ssh_cidr
  }

  # HTTP access
  ingress {
    description = "HTTP access"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = var.allowed_http_cidr
  }

  # HTTPS access
  ingress {
    description = "HTTPS access"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = var.allowed_http_cidr
  }

  # API Gateway
  ingress {
    description = "API Gateway"
    from_port   = 5000
    to_port     = 5001
    protocol    = "tcp"
    cidr_blocks = var.allowed_http_cidr
  }

  # Frontend (Client)
  ingress {
    description = "Frontend Client"
    from_port   = 5173
    to_port     = 5173
    protocol    = "tcp"
    cidr_blocks = var.allowed_http_cidr
  }

  # Frontend (Admin)
  ingress {
    description = "Frontend Admin"
    from_port   = 5174
    to_port     = 5174
    protocol    = "tcp"
    cidr_blocks = var.allowed_http_cidr
  }

  # NOTE: Microservices ports (6000-6020, 6100-6120) are NOT exposed
  # They communicate internally via Docker network
  # Only API Gateway is exposed to handle external requests

  # RabbitMQ Management (OPTIONAL - only for admin access)
  # Consider restricting to admin IPs only in production
  ingress {
    description = "RabbitMQ Management UI"
    from_port   = 15672
    to_port     = 15672
    protocol    = "tcp"
    cidr_blocks = var.allowed_ssh_cidr  # Changed from allowed_http_cidr to allowed_ssh_cidr
  }

  # Grafana
  ingress {
    description = "Grafana Dashboard"
    from_port   = 3000
    to_port     = 3000
    protocol    = "tcp"
    cidr_blocks = var.allowed_http_cidr
  }

  # Prometheus
  ingress {
    description = "Prometheus"
    from_port   = 9090
    to_port     = 9090
    protocol    = "tcp"
    cidr_blocks = var.allowed_http_cidr
  }

  # Jaeger UI
  ingress {
    description = "Jaeger UI"
    from_port   = 16686
    to_port     = 16686
    protocol    = "tcp"
    cidr_blocks = var.allowed_http_cidr
  }

  # SQL Server - Discount Service
  ingress {
    description = "SQL Server Discount"
    from_port   = 1434
    to_port     = 1434
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # SQL Server - Saga Service
  ingress {
    description = "SQL Server Saga"
    from_port   = 1400
    to_port     = 1400
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # SQL Server - User Service
  ingress {
    description = "SQL Server User"
    from_port   = 1445
    to_port     = 1445
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # SQL Server - Schedule Service
  ingress {
    description = "SQL Server Schedule"
    from_port   = 1446
    to_port     = 1446
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # SQL Server - Doctor Service
  ingress {
    description = "SQL Server Doctor"
    from_port   = 1447
    to_port     = 1447
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # SQL Server - Hospital Service
  ingress {
    description = "SQL Server Hospital"
    from_port   = 1448
    to_port     = 1448
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # SQL Server - Auth Service
  ingress {
    description = "SQL Server Auth"
    from_port   = 1449
    to_port     = 1449
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # SQL Server - Appointment Service
  ingress {
    description = "SQL Server Appointment"
    from_port   = 1450
    to_port     = 1450
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # SQL Server - Payment Service
  ingress {
    description = "SQL Server Payment"
    from_port   = 1451
    to_port     = 1451
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # SQL Server - ServiceMedical Service
  ingress {
    description = "SQL Server ServiceMedical"
    from_port   = 1452
    to_port     = 1452
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # SQL Server - AI Service
  ingress {
    description = "SQL Server AI"
    from_port   = 1453
    to_port     = 1453
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # MongoDB
  ingress {
    description = "MongoDB"
    from_port   = 27017
    to_port     = 27017
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # Outbound traffic
  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(
    var.tags,
    {
      Name = "${var.project_name}-${var.environment}-ec2-sg"
    }
  )
}

###############################################################################
# IAM Role for EC2
###############################################################################

resource "aws_iam_role" "ec2" {
  name = "${var.project_name}-${var.environment}-ec2-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ec2.amazonaws.com"
        }
      }
    ]
  })

  tags = merge(
    var.tags,
    {
      Name = "${var.project_name}-${var.environment}-ec2-role"
    }
  )
}

resource "aws_iam_role_policy_attachment" "ec2_ssm" {
  role       = aws_iam_role.ec2.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

resource "aws_iam_role_policy_attachment" "ec2_cloudwatch" {
  role       = aws_iam_role.ec2.name
  policy_arn = "arn:aws:iam::aws:policy/CloudWatchAgentServerPolicy"
}

resource "aws_iam_role_policy" "ec2_s3_access" {
  name = "${var.project_name}-${var.environment}-ec2-s3-policy"
  role = aws_iam_role.ec2.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject",
          "s3:ListBucket"
        ]
        Resource = [
          "arn:aws:s3:::mabs-capstone-fa25-booking-care-s3-bucket",
          "arn:aws:s3:::mabs-capstone-fa25-booking-care-s3-bucket/*"
        ]
      }
    ]
  })
}

resource "aws_iam_instance_profile" "ec2" {
  name = "${var.project_name}-${var.environment}-ec2-profile"
  role = aws_iam_role.ec2.name

  tags = merge(
    var.tags,
    {
      Name = "${var.project_name}-${var.environment}-ec2-profile"
    }
  )
}
