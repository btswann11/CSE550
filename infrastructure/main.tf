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

resource "azurerm_service_plan" "ut_serviceplan" {
    name                = "cse550-universal-translator-serviceplan"
    location            = azurerm_resource_group.cse550_ut_rg.location
    resource_group_name = azurerm_resource_group.cse550_ut_rg.name
    os_type = "Linux"
    sku_name = "F1"
}

resource "azurerm_linux_function_app" "ut_function" {
  name                = "example-linux-function-app"
  resource_group_name = azurerm_resource_group.cse550_ut_rg.name
  location            = azurerm_resource_group.cse550_ut_rg.location

  storage_account_name       = azurerm_storage_account.ut_storage.name
  storage_account_access_key = azurerm_storage_account.ut_storage.primary_access_key
  service_plan_id            = azurerm_service_plan.ut_serviceplan.id

  app_settings = {
    "AzureSignalRConnectionString" = azurerm_signalr_service.ut_signalr.primary_connection_string
  }

  site_config {}
}