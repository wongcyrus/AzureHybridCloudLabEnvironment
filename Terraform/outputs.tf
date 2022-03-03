output "function_app_name" {
  value       = module.func.function_app_name
  description = "Deployed function app name"
}

output "function_app_id" {
  value       = module.func.function_app_id
  description = "Deployed function app ID"
}

output "function_app_default_hostname" {
  value       = module.func.function_app_default_hostname
  description = "Deployed function app hostname"
}

output "function_app_storage_connection" {
  value       = module.func.function_app_storage_connection
  description = "Conenction String for the storage account that needs to be set as AzureWebJobsStorage"
  sensitive   = true
}

output "function_key_AddSshConnectionFunction" {
  value = module.func.function_key_AddSshConnectionFunction
}

output "LifeCycleHookUrl" {
  value = "https://${module.func.function_app_default_hostname}/api/AddSshConnectionFunction?code=${module.func.function_key_AddSshConnectionFunction}"
}

output "AzureFunctionBaseUrl" {
  value = "https://${module.func.function_app_default_hostname}"
}

output "GetSessionFunctionKey" {
  value = module.func.function_key_GetSessionFunction
}

output "iot_hub_primary_connection_string" {
  value = nonsensitive(module.iot.iot_hub_primary_connection_string)
}

output "event_hub_primary_connection_string" {
  value = nonsensitive(module.iot.event_hub_primary_connection_string)
}

output "eventhub_name" {
  value = module.iot.eventhub_name
}

output "iothub_name" {
  value = module.iot.iothub_name
}


