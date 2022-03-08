# SECRETS, PLEASE PROVIDE THESE VALUES IN A TFVARS FILE
variable "SUBSCRIPTION_ID" {}
variable "TENANT_ID" {}
variable "EMAIL_SMTP" {}
variable "EMAIL_USERNAME" {}
variable "EMAIL_PASSWORD" {}
variable "EMAIL_FROM_ADDRESS" {}
variable "ADMIN_EMAIL" {}

# GLOBAL VARIABLES
variable "RESOURCE_GROUP" {
  default = "AzureHybridCloudLabEnvironment"
}
variable "FUNCTION_APP_NAME" {
  default = "ive-lab"
}
variable "ENVIRONMENT" {
  default = "dev"
}
variable "LOCATION" {
  default = "EastAsia"
}

