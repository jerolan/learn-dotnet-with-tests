targetScope = 'resourceGroup'

@description('Short prefix for the resource names.')
@minLength(3)
@maxLength(20)
param namePrefix string

@description('Azure region for the resources.')
param location string = resourceGroup().location

@description('App Service plan SKU.')
@allowed([
  'B1'
  'S1'
])
param planSku string = 'B1'

resource plan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${namePrefix}-plan'
  location: location
  sku: {
    name: planSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2022-09-01' = {
  name: '${namePrefix}-api'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|6.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__TaskTracker'
          value: '@Microsoft.KeyVault(SecretUri=${vault.properties.vaultUri}secrets/tasktracker-db)'
        }
      ]
    }
  }
}

resource vault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: '${namePrefix}-kv'
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

resource secretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(vault.id, app.id, 'Key Vault Secrets User')
  scope: vault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output appHostName string = app.properties.defaultHostName
output vaultUri string = vault.properties.vaultUri
output principalId string = app.identity.principalId
