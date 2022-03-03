resource "azurerm_resource_group" "func-rg" {
  name     = var.RESOURCE_GROUP
  location = var.LOCATION
}

resource "random_string" "prefix" {
  length  = 4
  special = false
  lower   = true
  upper   = false
}

resource "random_string" "storage_name" {
  length  = 24
  upper   = false
  lower   = true
  number  = true
  special = false
}

resource "azurerm_storage_account" "storage" {
  name                     = random_string.storage_name.result
  resource_group_name      = azurerm_resource_group.func-rg.name
  location                 = var.LOCATION
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "deployments" {
  name                  = "function-releases"
  storage_account_name  = azurerm_storage_account.storage.name
  container_access_type = "private"
}

resource "azurerm_storage_table" "computer" {
  name                 = "Computer"
  storage_account_name = azurerm_storage_account.storage.name
}

resource "azurerm_storage_table" "ssh_Connection" {
  name                 = "SshConnection"
  storage_account_name = azurerm_storage_account.storage.name
}

resource "azurerm_storage_table" "session" {
  name                 = "Session"
  storage_account_name = azurerm_storage_account.storage.name
}

resource "azurerm_storage_queue" "retry" {
  name                 = "retry"
  storage_account_name = azurerm_storage_account.example.name
}

module "iot" {
  source                         = "./modules/iot"
  LOCATION                       = var.LOCATION
  RESOURCE_GROUP                 = azurerm_resource_group.func-rg
  ENVIRONMENT                    = var.ENVIRONMENT
  PREFIX                         = random_string.prefix.result
  STORAGE_ACC_NAME               = azurerm_storage_account.storage.name
  PRIMARY_BLOB_CONNECTION_STRING = azurerm_storage_account.storage.primary_blob_connection_string
  depends_on                     = [azurerm_resource_group.func-rg]
}

module "func" {
  source                              = "./modules/func"
  FUNCTION_APP_NAME                   = var.FUNCTION_APP_NAME
  LOCATION                            = var.LOCATION
  RESOURCE_GROUP                      = azurerm_resource_group.func-rg
  ENVIRONMENT                         = var.ENVIRONMENT
  PREFIX                              = random_string.prefix.result
  STORAGE_ACC_NAME                    = azurerm_storage_account.storage.name
  STORAGE_ACC_KEY                     = azurerm_storage_account.storage.primary_access_key
  STORAGE_CONNECTION_STRING           = azurerm_storage_account.storage.primary_blob_connection_string
  DEPLOYMENTS_NAME                    = azurerm_storage_container.deployments.name
  IOT_HUB_PRIMARY_CONNECTION_STRING   = module.iot.iot_hub_primary_connection_string
  EVENT_HUB_PRIMARY_CONNECTION_STRING = module.iot.event_hub_primary_connection_string
  EVENTHUB_NAME                       = module.iot.eventhub_name
  IOTHUB_NAME                         = module.iot.iothub_name
  EMAIL_SMTP                          = var.EMAIL_SMTP
  EMAIL_USERNAME                      = var.EMAIL_USERNAME
  EMAIL_PASSWORD                      = var.EMAIL_PASSWORD
  EMAIL_FROM_ADDRESS                  = var.EMAIL_FROM_ADDRESS
  ADMIN_EMAIL                         = var.ADMIN_EMAIL
  depends_on                          = [azurerm_resource_group.func-rg]
}
