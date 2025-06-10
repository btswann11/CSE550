$rg_name = "rg-cse550-universal-translator"
$location = "eastus"
$signalr_name = "signalr-cse550-universal-translator"

$storage_name = "cse550utstorage"
$functionAppName = "cse550utfunctionapp"

# Create a resource group
az group create --name $rg_name --location $location

# Create a SignalR service
az signalr create --name $signalr_name --resource-group $rg_name --location $location --sku Standard_S1

# get connection string
$signalr_connection_string = az signalr key list --name $signalr_name --resource-group $rg_name --query "primaryConnectionString" -o tsv

# create storage account
az storage account create --name $storage_name --resource-group $rg_name --location $location --sku Standard_LRS

# create function app
az functionapp create `
    --name $functionAppName `
    --storage-account $storage_name `
    --resource-group $rg_name `
    --plan "Consumption" `
    --runtime dotnet `
    --runtime-version 8.0 `
    --functions-version 4 `
    --os-type Linux

# Configure the function app with the SignalR connection string
az functionapp config appsettings set `
    --name $functionAppName `
    --resource-group $rg_name `
    --settings "AzureSignalRConnectionString=$signalr_connection_string"