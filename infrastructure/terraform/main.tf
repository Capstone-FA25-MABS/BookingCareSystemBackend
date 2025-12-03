###############################################################################
# Networking Module
###############################################################################

module "networking" {
  source = "./modules/networking"

  project_name       = var.project_name
  environment        = var.environment
  vpc_cidr           = var.vpc_cidr
  public_subnet_cidr = var.public_subnet_cidr
  availability_zone  = var.availability_zone
  tags               = var.tags
}

###############################################################################
# Security Module
###############################################################################

module "security" {
  source = "./modules/security"

  project_name      = var.project_name
  environment       = var.environment
  vpc_id            = module.networking.vpc_id
  allowed_ssh_cidr  = var.allowed_ssh_cidr
  allowed_http_cidr = var.allowed_http_cidr
  tags              = var.tags
}

###############################################################################
# Compute Module
###############################################################################

module "compute" {
  source = "./modules/compute"

  project_name           = var.project_name
  environment            = var.environment
  instance_type          = var.instance_type
  ami_id                 = var.ami_id
  key_name               = var.key_name
  subnet_id              = module.networking.public_subnet_id
  security_group_id      = module.security.security_group_id
  instance_profile_name  = module.security.instance_profile_name
  availability_zone      = var.availability_zone
  root_volume_size       = var.root_volume_size
  root_volume_type       = var.root_volume_type
  root_volume_iops       = var.root_volume_iops
  root_volume_throughput = var.root_volume_throughput
  docker_volume_size     = var.docker_volume_size
  docker_volume_type     = var.docker_volume_type
  enable_monitoring      = var.enable_monitoring
  enable_eip             = var.enable_eip
  user_data_script       = "${path.module}/user-data.sh"
  tags                   = var.tags
}

