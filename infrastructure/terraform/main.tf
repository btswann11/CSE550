provider "azurerm" {
    features {}
}

resource "azurerm_resource_group" "cse550_ut_rg" {
    name     = "cse550-rg"
    location = "East US"  
}

resource "azurerm_signalr_service" "ut_signalr" {
    name = "cse550-universal-translator-signalr"
    location = azurerm_resource_group.cse550_ut_rg.location
    resource_group_name = azurerm_resource_group.cse550_ut_rg.name
    service_mode = "Serverless"
    sku {
        name     = "Standard_S1"
        capacity = 1
    }
}

resource "azurerm_storage_account" "ut_storage" {
    name                     = "cse550utstorage"
    resource_group_name      = azurerm_resource_group.cse550_ut_rg.name
    location                 = azurerm_resource_group.cse550_ut_rg.location
    account_tier             = "Standard"
    account_replication_type = "LRS"
}

resource "azurerm_storage_container" "ut_container" {
  name                  = "cse550-ut-container"
  storage_account_id    = azurerm_storage_account.ut_storage.id
  container_access_type = "private"
}

resource "azurerm_storage_table" "ut_chat_table" {
  name = "ut-chat-table"
  storage_account_name = azurerm_storage_account.ut_storage.name
}

resource "azurerm_service_plan" "ut_serviceplan" {
    name                = "cse550-universal-translator-serviceplan"
    location            = azurerm_resource_group.cse550_ut_rg.location
    resource_group_name = azurerm_resource_group.cse550_ut_rg.name
    os_type = "Linux"
    sku_name = "FC1"
}

resource "azurerm_function_app_flex_consumption" "ut_flex_function_app" {
  name                = "cse550-universal-translator-function-app"
  resource_group_name = azurerm_resource_group.cse550_ut_rg.name
  location            = azurerm_resource_group.cse550_ut_rg.location
  service_plan_id     = azurerm_service_plan.ut_serviceplan.id

  storage_container_type      = "blobContainer"
  storage_container_endpoint  = "${azurerm_storage_account.ut_storage.primary_blob_endpoint}${azurerm_storage_container.ut_container.name}"
  storage_authentication_type = "StorageAccountConnectionString"
  storage_access_key          = azurerm_storage_account.ut_storage.primary_access_key
  runtime_name                = "dotnet-isolated"
  runtime_version             = "8.0"
  maximum_instance_count      = 40
  instance_memory_in_mb       = 1024
  
  site_config {
  }
}

resource "azurerm_cognitive_account" "universal_translator" {
    name                = "cse550-universal-translator"
    location            = azurerm_resource_group.cse550_ut_rg.location
    resource_group_name = azurerm_resource_group.cse550_ut_rg.name
    kind                = "TextTranslation"
    sku_name            = "F0"  
}