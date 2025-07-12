# Outputs for Universal Translator Infrastructure
# These outputs provide important information about the deployed resources

output "resource_group_name" {
  description = "The name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "resource_group_id" {
  description = "The ID of the resource group"
  value       = azurerm_resource_group.main.id
}

output "storage_account_name" {
  description = "The name of the storage account"
  value       = azurerm_storage_account.main.name
}

output "storage_account_connection_string" {
  description = "The connection string for the storage account"
  value       = azurerm_storage_account.main.primary_connection_string
  sensitive   = true
}

output "storage_table_name" {
  description = "The name of the storage table"
  value       = var.storage_table_name
}

output "signalr_service_name" {
  description = "The name of the SignalR service"
  value       = azurerm_signalr_service.main.name
}

output "signalr_connection_string" {
  description = "The connection string for SignalR service"
  value       = azurerm_signalr_service.main.primary_connection_string
  sensitive   = true
}

output "function_app_name" {
  description = "The name of the function app"
  value       = azurerm_function_app_flex_consumption.main.name
}

output "function_app_default_hostname" {
  description = "The default hostname of the function app"
  value       = azurerm_function_app_flex_consumption.main.default_hostname
}

output "function_app_url" {
  description = "The URL of the function app"
  value       = "https://${azurerm_function_app_flex_consumption.main.default_hostname}"
}

output "cognitive_services_name" {
  description = "The name of the cognitive services account"
  value       = azurerm_cognitive_account.translator.name
}

output "cognitive_services_endpoint" {
  description = "The endpoint for the cognitive services account"
  value       = azurerm_cognitive_account.translator.endpoint
}

output "cognitive_services_key" {
  description = "The primary key for the cognitive services account"
  value       = azurerm_cognitive_account.translator.primary_access_key
  sensitive   = true
}

output "cognitive_services_location" {
  description = "The location of the cognitive services account"
  value       = azurerm_cognitive_account.translator.location
}

# Deployment information
output "deployment_info" {
  description = "Summary of deployed resources"
  value = {
    project_name     = var.project_name
    environment      = var.environment
    location         = var.location
    resource_group   = azurerm_resource_group.main.name
    function_app_url = "https://${azurerm_function_app_flex_consumption.main.default_hostname}"
    deployment_time  = timestamp()
  }
}
