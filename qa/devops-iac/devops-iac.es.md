# DevOps e infraestructura como código

[Encuentras la plantilla de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/devops-iac)

Un entorno creado haciendo clic en un portal no tiene revisión, ni diff, ni forma de reconstruirse después de un error. Staging y producción se desvían, y la persona que sabe qué checkbox era distinto está de vacaciones.

La infraestructura como código pone los recursos en un archivo. Este apéndice escribe una plantilla Bicep para el app service, su identidad y un key vault, la compila y nombra las piezas que agregaría una suscripción real.

## La plantilla

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

El plan y la app alcanzan para correr; la identidad es lo que deja que la app lea el vault sin un password en la plantilla. El vault y la asignación de rol vienen después:

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

`enableRbacAuthorization: true` hace que el vault use asignaciones de rol en vez de access policies. El rol `4633458b` es *Key Vault Secrets User*: leer secretos, nada más. El app setting referencia un secreto llamado `tasktracker-db`; un operador lo crea una vez, fuera de la plantilla.

## Compílala

```sh
bicep build main.bicep
```

El comando sale con 0 y escribe `main.json`, la plantilla ARM que consume el motor de despliegue. La compilación atrapa un tipo de recurso mal escrito, una propiedad en el objeto equivocado, un parámetro requerido que falta y una expresión que no tipa. No atrapa un rol demasiado amplio, un secreto que no existe ni un runtime que la región ya no ofrece.

La plantilla fija `DOTNETCORE|6.0` porque el libro apunta a .NET 6. .NET 6 está fuera de soporte; un despliegue nuevo debería usar un LTS soportado o la imagen de contenedor del apéndice de contenedores. El runtime es una línea en un archivo, que es la gracia de tenerlo aquí.

## Parámetros y entornos

`namePrefix` no tiene default, así que un despliegue no puede reutilizar el nombre equivocado en silencio. `planSku` permite `B1` o `S1`; cualquier otro valor falla antes de crear un recurso.

Pon los valores de entorno en archivos de parámetros, no en ramas ni en plantillas copiadas:

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

La misma plantilla despliega staging y producción. La diferencia es un archivo chico que se puede revisar. Los secretos no van en ese archivo; la referencia al vault y la identidad administrada existen para eso.

## What-if antes del deploy

```sh
az deployment group what-if \
  --resource-group rg-tasktracker \
  --template-file main.bicep \
  --parameters main.prod.parameters.json
```

`what-if` muestra creates, updates y deletes antes de que pasen. Córrelo en el pull request que cambia la plantilla, para que un revisor vea el diff de infraestructura junto al diff de código. El deploy en sí corre después de la aprobación:

```sh
az deployment group create \
  --resource-group rg-tasktracker \
  --template-file main.bicep \
  --parameters main.prod.parameters.json
```

Ninguno de los dos comandos corre en este repositorio: no hay suscripción ni credencial aquí.

## Drift y estado

Bicep es declarativo contra Azure Resource Manager. ARM compara la plantilla con los recursos actuales y aplica la diferencia; no hay archivo de estado local que perder. Eso también significa que un recurso cambiado en el portal será revertido por el siguiente despliegue, que es el comportamiento que quieres de una fuente de verdad.

Terraform guarda un archivo de estado. Soporta más proveedores, y el estado se vuelve un artefacto que hay que almacenar, bloquear y proteger. Elige por los entornos que realmente operas, no por la sintaxis que prefieres.

Los recursos existentes se pueden importar, pero importar es un ejercicio aparte. No apuntes una plantilla nueva a un resource group de producción hasta que `what-if` muestre solo los cambios que esperas.

## Pon la plantilla en CI

- Compila en cada pull request: `bicep build` es rápido y atrapa errores de sintaxis y tipos sin credenciales.
- Corre `what-if` en `main` con la identidad de despliegue y guarda la salida como evidencia.
- Despliega solo después de la aprobación del entorno, con la misma plantilla y el mismo archivo de parámetros que produjeron el `what-if`.
- Lint además de compilar. `bicep lint` impone reglas como location explícita y nada de secretos hardcodeados.
- Versiona las plantillas con la aplicación. Un deploy que necesita un app setting nuevo debería cambiar los dos en un mismo pull request.

## Lo que queda diferido

No hay suscripción, resource group, backend de estado, salida de `what-if` ni despliegue. Un laboratorio práctico:

- Crearía un resource group y correría `what-if` contra un grupo vacío.
- Desplegaría la plantilla, crearía el secreto en el vault y confirmaría que la app lo lee con su identidad.
- Cambiaría el SKU en el archivo de parámetros y vería a `what-if` proponer un update en vez de un reemplazo.
- Borraría un recurso en el portal y correría el despliegue otra vez para verlo restaurado.

## Para cerrar

Lo que cubrimos:

- Una plantilla Bicep para un plan de App Service, una app Linux con identidad de sistema, un key vault y una asignación de rol de mínimo privilegio.
- Compilación frente a runtime: qué prueba `bicep build` y qué no puede probar.
- Archivos de parámetros por entorno y secretos fuera de ellos.
- `what-if` antes del deploy y la aprobación que sigue.
- Corrección declarativa del drift, el estado de Terraform como alternativa y los chequeos de CI que mantienen honesta una plantilla.
- La suscripción, la creación del secreto y los pasos de importación que este repositorio no puede ejecutar.
