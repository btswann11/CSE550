targetScope = 'subscription'

param location string = 'eastus'
param rgName string = 'ut_rg'

resource ut_rg 'Microsoft.Resources/resourceGroups@2024-11-01' = {
  name: rgName
  location: location
}



resource ut_signalr 'Microsoft.SignalRService/signalR@2024-10-01-preview' = {
  name: 'ut_signalr'
  location: location


}
