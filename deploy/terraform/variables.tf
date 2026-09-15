variable "region" {
  description = "AWS region for the demo stack."
  type        = string
  default     = "us-east-1"
}

variable "vpc_id" {
  description = "VPC the API security group is attached to."
  type        = string
  default     = "vpc-0123456789abcdef0"
}
