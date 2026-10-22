# DevOps and infrastructure as code

[You can find the template for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/devops-iac)

An environment created by clicking through a portal has no review, no diff and no way to rebuild it after a mistake. Staging and production drift, and the person who knows which checkbox was different is on vacation.

Infrastructure as code puts the resources in a file. This appendix writes a Bicep template for the app service, its identity and a key vault, compiles it, and names the parts a real subscription would add.

## The template

`qa/devops-iac/main.bicep`:

```bicep
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
```

The plan and the app are enough to run; the identity is what lets the app read the vault without a password in the template. The vault and the role assignment follow:

```bicep
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
```

`enableRbacAuthorization: true` makes the vault use role assignments instead of access policies. The `4633458b` role is *Key Vault Secrets User*: read secrets, nothing else. The app setting references a secret named `tasktracker-db`; an operator creates that secret once, outside the template.

## Compile it

```sh
bicep build main.bicep
```

The command exits 0 and writes `main.json`, the ARM template the deployment engine consumes. Compilation catches a misspelled resource type, a property in the wrong object, a missing required parameter and an expression that does not type-check. It does not catch a role that is too broad, a secret that does not exist, or a runtime the region no longer offers.

The template pins `DOTNETCORE|6.0` because the book targets .NET 6. .NET 6 is out of support; a new deployment should use a supported LTS or the container image from the containers appendix. The runtime is one line in one file, which is the point of keeping it here.

## Parameters and environments

`namePrefix` has no default, so a deployment cannot silently reuse the wrong name. `planSku` allows `B1` or `S1`; anything else fails before a resource is created.

Put environment values in parameter files, not in branches or copy-pasted templates:

```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "namePrefix": { "value": "tasktracker-prod" },
    "planSku": { "value": "S1" }
  }
}
```

The same template deploys staging and production. The difference is a small file that can be reviewed. Secrets do not belong in that file; the vault reference and the managed identity exist for that reason.

## What-if before deploy

```sh
az deployment group what-if \
  --resource-group rg-tasktracker \
  --template-file main.bicep \
  --parameters main.prod.parameters.json
```

`what-if` shows creates, updates and deletes before they happen. Run it on the pull request that changes the template, so a reviewer sees the infrastructure diff next to the code diff. The deploy itself runs after approval:

```sh
az deployment group create \
  --resource-group rg-tasktracker \
  --template-file main.bicep \
  --parameters main.prod.parameters.json
```

Neither command runs in this repository: there is no subscription or credential here.

## Drift and state

Bicep is declarative against Azure Resource Manager. ARM compares the template with the current resources and applies the difference; there is no local state file to lose. That also means a resource changed in the portal will be reverted by the next deployment, which is the behavior you want from a source of truth.

Terraform keeps a state file instead. It supports more providers, and the state becomes an artifact you must store, lock and protect. Choose by the environments you actually operate, not by the syntax you prefer.

Existing resources can be imported, but import is a separate exercise. Do not point a new template at a production resource group until `what-if` shows only the changes you expect.

## Put the template in CI

- Compile on every pull request: `bicep build` is fast and catches syntax and type errors without credentials.
- Run `what-if` on `main` with the deployment identity, and keep the output as evidence.
- Deploy only after the environment approval, using the same template and parameter file that produced the `what-if`.
- Lint as well as compile. `bicep lint` enforces rules such as explicit location and no hard-coded secrets.
- Version the templates with the application. A deploy that needs a new app setting should change both in one pull request.

## What is deferred

There is no subscription, resource group, state backend, `what-if` output or deployment here. A hands-on lab would:

- Create a resource group and run `what-if` against an empty group.
- Deploy the template, create the vault secret, and confirm the app can read it through its identity.
- Change the SKU in the parameter file and watch `what-if` propose an update instead of a replacement.
- Delete a resource in the portal and run the deployment again to see it restored.

## Wrapping up

What we have covered:

- A Bicep template for an App Service plan, a Linux app with a system identity, a key vault and a least-privilege role assignment.
- Compilation vs runtime: what `bicep build` proves and what it cannot.
- Parameter files per environment and secrets kept out of them.
- `what-if` before deploy, and the approval that follows.
- Declarative drift correction, Terraform state as the alternative, and the CI checks that keep a template honest.
- The subscription, secret creation and import steps that this repository cannot perform.
