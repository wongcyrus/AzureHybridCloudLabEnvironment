### INPUT VARs ###
variable "FUNCTION_APP_NAME" {}
variable "PREFIX" {}
variable "ENVIRONMENT" {}
variable "LOCATION" {}
variable "RESOURCE_GROUP" {}
variable "STORAGE_ACC_NAME" {}
variable "STORAGE_ACC_KEY" {}
variable "STORAGE_CONNECTION_STRING" {}
variable "DEPLOYMENTS_NAME" {}
variable "IOT_HUB_PRIMARY_CONNECTION_STRING" {}
variable "EVENT_HUB_PRIMARY_CONNECTION_STRING" {}
variable "EVENTHUB_NAME" {}
variable "IOTHUB_NAME" {}
variable "EMAIL_SMTP" {}
variable "EMAIL_USERNAME" {}
variable "EMAIL_PASSWORD" {}
variable "EMAIL_FROM_ADDRESS" {}
variable "ADMIN_EMAIL" {}
variable "BASTION_ARC_ADMIN_USERNAME" {}
variable "BASTION_ARC_ADMIN_PASSWORD" {}
variable "BASTION_ARC_LOGIN_SERVER" {}

variable "FUNCTION_APP_FOLDER" {
  default = "../../../PcHubFunctionApp"
}
variable "FUNCTION_APP_PUBLISH_FOLDER" {
  default = "../../../PcHubFunctionApp/bin/Release/net6.0/publish"
}