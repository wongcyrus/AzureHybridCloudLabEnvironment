resource "azurerm_eventhub_namespace" "lab-pc" {
  name                = "lab-pc-namespace"
  location            = var.LOCATION
  resource_group_name = var.RESOURCE_GROUP.name
  sku                 = "Basic"
}

resource "azurerm_eventhub" "lab-pc" {
  name                = "${var.PREFIX}-lab-pc-eventhub"
  resource_group_name = var.RESOURCE_GROUP.name
  namespace_name      = azurerm_eventhub_namespace.lab-pc.name
  partition_count     = 2
  message_retention   = 1
}

resource "azurerm_eventhub_authorization_rule" "lab-pc" {
  resource_group_name = var.RESOURCE_GROUP.name
  namespace_name      = azurerm_eventhub_namespace.lab-pc.name
  eventhub_name       = azurerm_eventhub.lab-pc.name
  name                = "azure_function"
  listen              = true
  send                = true
  manage              = true
}

resource "azurerm_iothub" "lab-pc" {
  name                = "${var.PREFIX}-lab-pc-ioTHub"
  resource_group_name = var.RESOURCE_GROUP.name
  location            = var.LOCATION

  sku {
    name     = "S1"
    capacity = "1"
  }

  endpoint {
    type              = "AzureIotHub.EventHub"
    connection_string = azurerm_eventhub_authorization_rule.lab-pc.primary_connection_string
    name              = "eventhub"
  }

  route {
    name           = "eventhubdevicemessages"
    source         = "DeviceMessages"
    condition      = "true"
    endpoint_names = ["eventhub"]
    enabled        = true
  }

  route {
    name           = "eventhubtwinchangeevents"
    source         = "TwinChangeEvents"
    condition      = "true"
    endpoint_names = ["eventhub"]
    enabled        = true
  }

  route {
    name           = "eventhubdeviceconnectionstateevents"
    source         = "DeviceConnectionStateEvents"
    condition      = "true"
    endpoint_names = ["eventhub"]
    enabled        = true
  }

  enrichment {
    key            = "tenant"
    value          = "$twin.tags.Tenant"
    endpoint_names = ["eventhub"]
  }

  cloud_to_device {
    max_delivery_count = 30
    default_ttl        = "PT1H"
    feedback {
      time_to_live       = "PT1H10M"
      max_delivery_count = 15
      lock_duration      = "PT30S"
    }
  }

  tags = {
    purpose = var.ENVIRONMENT
  }
}

resource "azurerm_iothub_shared_access_policy" "lab-pc" {
  name                = "lab-pc"
  resource_group_name = var.RESOURCE_GROUP.name
  iothub_name         = azurerm_iothub.lab-pc.name
  registry_read       = true
  registry_write      = true
  service_connect     = true
  device_connect      = true
}

resource "azurerm_iothub_endpoint_eventhub" "lab-pc" {
  resource_group_name = var.RESOURCE_GROUP.name
  iothub_id           = azurerm_iothub.lab-pc.id
  name                = "lab-pc"
  connection_string   = azurerm_eventhub_authorization_rule.lab-pc.primary_connection_string
}