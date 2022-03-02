output "iot_hub_primary_connection_string" {
  value = azurerm_iothub_shared_access_policy.lab-pc.primary_connection_string
}

output "event_hub_primary_connection_string" {
  value = azurerm_eventhub_authorization_rule.lab-pc.primary_connection_string
}

output "eventhub_name" {
  value = azurerm_eventhub.lab-pc.name
}

