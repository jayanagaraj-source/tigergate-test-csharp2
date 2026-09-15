# IaC FIXTURE - deliberately misconfigured.

terraform {
  required_version = ">= 1.5.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  region = var.region

  # Hardcoded static credentials.
  access_key = "AKIAIOSFODNN7EXAMPLE"
  secret_key = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
}

# Public bucket, no encryption, no versioning, no access logging.
resource "aws_s3_bucket" "exports" {
  bucket = "tigergate-demo-exports"
}

resource "aws_s3_bucket_public_access_block" "exports" {
  bucket                  = aws_s3_bucket.exports.id
  block_public_acls       = false
  block_public_policy     = false
  ignore_public_acls      = false
  restrict_public_buckets = false
}

resource "aws_s3_bucket_acl" "exports" {
  bucket = aws_s3_bucket.exports.id
  acl    = "public-read-write"
}

# Security group open to the world on admin ports.
resource "aws_security_group" "api" {
  name        = "tigergate-demo-api"
  description = "API ingress"
  vpc_id      = var.vpc_id

  ingress {
    description = "SSH from anywhere"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "RDP from anywhere"
    from_port   = 3389
    to_port     = 3389
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    description = "SQL Server from anywhere"
    from_port   = 1433
    to_port     = 1433
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# Publicly reachable, unencrypted database with a hardcoded password.
resource "aws_db_instance" "shop" {
  identifier                  = "tigergate-demo-shop"
  engine                      = "sqlserver-ex"
  instance_class              = "db.t3.micro"
  allocated_storage           = 20
  username                    = "sa"
  password                    = "P@ssw0rd!2019"
  publicly_accessible         = true
  storage_encrypted           = false
  skip_final_snapshot         = true
  backup_retention_period     = 0
  auto_minor_version_upgrade  = false
  deletion_protection         = false
  vpc_security_group_ids      = [aws_security_group.api.id]
}

# Unencrypted queue and an over-broad IAM policy.
resource "aws_sqs_queue" "exports" {
  name                      = "tigergate-demo-exports"
  kms_master_key_id         = null
  sqs_managed_sse_enabled   = false
}

resource "aws_iam_policy" "api" {
  name = "tigergate-demo-api"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = "*"
        Resource = "*"
      }
    ]
  })
}
