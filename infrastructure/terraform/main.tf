# Universal Translator Infrastructure
# This configuration deploys the Azure infrastructure for the Universal Translator application
# Resources: Resource Group, Storage Account, SignalR Service, Function App, Cognitive Services

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.0"
    }
  }
  required_version = ">= 1.0"
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = local.resource_group_name
  location = var.location
  tags     = local.common_tags
}

# Storage Account for application data and function app storage
resource "azurerm_storage_account" "main" {
  name                     = local.storage_account_name_clean
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = var.storage_account_tier
  account_replication_type = var.storage_replication_type

  # Security settings
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = true

  tags = local.common_tags
}

# Storage Container for Function App
resource "azurerm_storage_container" "function_app" {
  name                  = var.storage_container_name
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

# SignalR Service for real-time communication
resource "azurerm_signalr_service" "main" {
  name                = local.signalr_service_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  service_mode        = "Serverless"

  sku {
    name     = var.signalr_sku.name
    capacity = var.signalr_sku.capacity
  }

  # CORS configuration for web clients
  cors {
    allowed_origins = ["*"]
  }

  tags = local.common_tags
}

# App Service Plan for Function App
resource "azurerm_service_plan" "main" {
  name                = local.service_plan_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  os_type             = "Linux"
  sku_name            = var.function_app_sku

  tags = local.common_tags
}

# Function App (Flex Consumption)
resource "azurerm_function_app_flex_consumption" "main" {
  name                = local.function_app_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  # Storage configuration
  storage_container_type      = "blobContainer"
  storage_container_endpoint  = "${azurerm_storage_account.main.primary_blob_endpoint}${azurerm_storage_container.function_app.name}"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.main.primary_access_key

  # Runtime configuration
  runtime_name           = "dotnet-isolated"
  runtime_version        = "8.0"
  maximum_instance_count = var.function_app_max_instances
  instance_memory_in_mb  = var.function_app_memory_mb

  # Application settings
  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME"               = "dotnet-isolated"
    "AzureWebJobsStorage"                    = azurerm_storage_account.main.primary_connection_string
    "AzureSignalRConnectionString"           = azurerm_signalr_service.main.primary_connection_string
    "UT_TRANSLATION_SERVICE_BASE_URI"        = "https://api.cognitive.microsofttranslator.com/"
    "UT_TRANSLATION_SERVICE_API_KEY"         = azurerm_cognitive_account.translator.primary_access_key
    "UT_TRANSLATION_SERVICE_LOCATION"        = azurerm_cognitive_account.translator.location
    "UT_CHATS_STORAGE_CONNECTION_STRING"     = azurerm_storage_account.main.primary_connection_string
    "UT_CHATS_TABLE_NAME"                    = var.storage_table_name
  }

  site_config {
    # CORS configuration
    cors {
      allowed_origins     = ["*"]
      support_credentials = true
    }

    # Application settings
    application_insights_connection_string = azurerm_application_insights.main.connection_string
  }

  tags = local.common_tags
}

# Application Insights for monitoring
resource "azurerm_application_insights" "main" {
  name                = "${local.function_app_name}-insights"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"

  tags = local.common_tags
}

# Cognitive Services for translation
resource "azurerm_cognitive_account" "translator" {
  name                = local.cognitive_account_name
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  kind                = "TextTranslation"
  sku_name            = var.cognitive_services_sku

  tags = local.common_tags
}