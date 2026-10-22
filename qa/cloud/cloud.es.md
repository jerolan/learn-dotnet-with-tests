# Despliegue en la nube

[Encuentras el código al que se refiere este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/cloud)

El job de CI publica una carpeta y el apéndice de contenedores construye una imagen. Ninguno de los dos está corriendo para un usuario. Un host en la nube agrega lo que una laptop esconde: un puerto público, configuración por entorno, secretos, una base que sobrevive al proceso, health probes y un camino de vuelta cuando un release sale mal.

Este repositorio no tiene una suscripción de nube, así que este apéndice no despliega nada. Nombra las decisiones y muestra el job de deploy que seguiría al artefacto publicado.

## Lo que la app ya necesita

- Un puerto. El contenedor escucha en `8080`; App Service y Container Apps esperan que la app se enlace al puerto que entregan por `PORT` o `WEBSITES_PORT`.
- Configuración desde el entorno. `ConnectionStrings__TaskTracker` y la clave JWT se leen de configuración; los defaults de `appsettings.json` son para local.
- Una ruta de datos escribible. SQLite necesita `/data`; una base administrada necesita una cadena de conexión y acceso de red.
- Un endpoint de salud. El contenedor puede responder `/health` antes de servir tareas.
- Logs en stdout. La plataforma los recoge; un archivo dentro del contenedor desaparece con la instancia.

## Elige un destino

| Destino | Encaja cuando | Te cuesta |
|---|---|---|
| App Service | Una sola web app, slots para staging y swap, poco trabajo de plataforma | Menos control del host; escala y red son features de la plataforma |
| Container Apps | Un contenedor, revisiones y reparto de tráfico, escala a cero | Cold starts; un modelo de revisiones que aprender |
| ECS en Fargate | Casa AWS, task definitions e integración con ALB | Más piezas: cluster, servicio, load balancer, roles |
| Kubernetes | Muchos servicios, cluster existente y equipo de plataforma | El cluster ahora es tu producto |

La elección cambia los comandos, no el contrato de la aplicación. La app sigue necesitando puerto, configuración, salud y logs.

## Configuración y secretos

Mantén lo no secreto en la configuración de la plataforma: nombre de entorno, nivel de log, hosts permitidos. Mantén los secretos en Key Vault, Secrets Manager o el store de secretos de la plataforma, y referéncialos desde los app settings.

```text
ConnectionStrings__TaskTracker = @Microsoft.KeyVault(SecretUri=https://.../secrets/tasktracker-db)
Jwt__Key                        = @Microsoft.KeyVault(SecretUri=https://.../secrets/jwt-key)
```

La app los lee igual que lee `appsettings.json`. El doble guion bajo es el separador de sección.

Prefiere una identidad administrada a una cadena de conexión con password. La app se autentica como la identidad de la plataforma y el secreto nunca existe en la configuración de la app. Si la base solo soporta password, rótalo en el store de secretos y reinicia, no en un commit.

## La base de datos

SQLite con un volumen es un almacén de una sola instancia. Dos contenedores no pueden compartir el archivo de forma segura por un volumen de red, y una plataforma que reemplaza la instancia puede perder el disco. Usa PostgreSQL o SQL Server administrado antes de la segunda réplica.

Corre las migraciones como un paso aparte, no desde cada instancia al arrancar. Dos instancias arrancando juntas compiten por la misma migración. Un job de deploy puede correr `dotnet ef database update` una vez, con la cadena de migración, antes de que la versión nueva reciba tráfico.

Mantén el esquema compatible con la versión anterior durante el rollout: agrega una columna nullable, despliega código que escribe las dos formas, rellena, y quita la forma vieja en un release posterior. Un rollback que no puede leer el esquema nuevo no es un rollback.

## Salud, readiness y arranque

Un liveness probe responde "el proceso está vivo". Un readiness probe responde "puede servir tráfico". No son lo mismo:

- Liveness no debería revisar la base. Una caída de la base reiniciaría cada instancia sana y empeoraría la caída.
- Readiness sí puede revisar la base y los servicios de los que depende. Un readiness que falla saca la instancia del load balancer sin matarla.
- Un startup probe le da tiempo a una migración lenta o a un warm-up antes de que empiece liveness.

La app debería exponer endpoints separados si la plataforma soporta ambos, y el deploy debería esperar readiness antes de mover tráfico.

## Despliega el artefacto

El job de publish sube `tasktracker-api`. Un job de deploy descarga exactamente ese artefacto, entra con OIDC y lo despliega:

```yaml
deploy:
  needs: publish
  if: github.ref == 'refs/heads/main'
  runs-on: ubuntu-latest
  environment: production
  permissions:
    id-token: write
    contents: read
  steps:
    - uses: actions/download-artifact@v4
      with:
        name: tasktracker-api
        path: artifacts/api
    - uses: azure/login@v2
      with:
        client-id: ${{ secrets.AZURE_CLIENT_ID }}
        tenant-id: ${{ secrets.AZURE_TENANT_ID }}
        subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    - name: Package
      run: cd artifacts/api && zip -r ../tasktracker.zip .
    - name: Deploy
      run: >
        az webapp deploy
        --resource-group rg-tasktracker
        --name tasktracker-api
        --src-path artifacts/tasktracker.zip
        --type zip
```

`environment: production` espera a los revisores configurados en ese entorno. `id-token: write` deja que el job cambie el token de GitHub por credenciales de nube de vida corta; ningún password de nube queda en el repositorio.

Para un host de contenedores, el mismo job empujaría la imagen taggeada con el SHA del commit y actualizaría la revisión a ese tag. Un tag `latest` móvil convierte el rollback en una adivinanza.

## Rollback y escala

Los slots de App Service vuelven en segundos. Container Apps puede mover tráfico entre revisiones; Kubernetes conserva el ReplicaSet anterior. En todos los casos, el artefacto o la imagen anterior tienen que seguir existiendo, y la base tiene que aceptarla.

Fija un mínimo de instancias arriba de cero si los cold starts son inaceptables. Escala por la señal que importa: tasa de peticiones o latencia, no solo CPU. Una regla basada en CPU reacciona después de que la cola ya creció.

Vigila el pool de conexiones: diez instancias con un pool de cien abren mil conexiones. El plan de la base tiene un límite, y el fallo se ve como timeouts bajo carga.

## Lo que queda diferido

No hay suscripción, registry, dominio, certificado ni URL viva. Un laboratorio práctico:

- Crearía un resource group y una instancia administrada de PostgreSQL.
- Empujaría la imagen a un registry y desplegaría una Container App con health probe.
- Configuraría OIDC entre el repositorio y la suscripción.
- Desplegaría una revisión mala, vería fallar readiness y volvería al digest anterior.
- Correría un test de carga contra la URL desplegada y lo compararía con el número local de NBomber.

## Para cerrar

Lo que cubrimos:

- Las necesidades de puerto, configuración, datos, salud y logs de la app publicada.
- App Service, Container Apps, ECS y Kubernetes como destinos con distintos trade-offs.
- Secretos en un store, identidad administrada y la base que reemplaza a SQLite.
- Migraciones como paso aparte y cambios de esquema que sobreviven un rollback.
- Un job de deploy que descarga el artefacto probado y entra con OIDC.
- Readiness, slots, revisiones, tags inmutables y límites del pool de conexiones.

Sigue: DevOps e infraestructura como código.
