terraform {
  required_version = ">= 1.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # Uncomment this block to use S3 backend for remote state
  # backend "s3" {
  #   bucket         = "bookingcare-terraform-state"
  #   key            = "ec2/terraform.tfstate"
  #   region         = "ap-southeast-1"
  #   encrypt        = true
  #   dynamodb_table = "bookingcare-terraform-locks"
  # }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "BookingCare"
      Environment = var.environment
      ManagedBy   = "Terraform"
      Owner       = "BookingCare-Team"
    }
  }
}
