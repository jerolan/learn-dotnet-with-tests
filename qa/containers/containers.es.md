# Contenedores

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/containers)

Un contenedor empaqueta la aplicación y su runtime en una sola imagen. Este apéndice construye la API TaskTracker de v15 con un Dockerfile multi-stage, la ejecuta con SQLite sobre un volumen y comprueba el documento OpenAPI desde fuera del contenedor.

## El Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY build-an-app/v15-signalr/src/TaskTracker.Api/TaskTracker.Api.csproj build-an-app/v15-signalr/src/TaskTracker.Api/
RUN dotnet restore build-an-app/v15-signalr/src/TaskTracker.Api/TaskTracker.Api.csproj

COPY build-an-app/v15-signalr/src/TaskTracker.Api/ build-an-app/v15-signalr/src/TaskTracker.Api/
RUN dotnet publish build-an-app/v15-signalr/src/TaskTracker.Api/TaskTracker.Api.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__TaskTracker="Data Source=/data/tasktracker.db"

EXPOSE 8080
VOLUME ["/data"]

ENTRYPOINT ["dotnet", "TaskTracker.Api.dll"]
```

La etapa de build restaura antes de copiar el código. Un cambio en el código reutiliza los paquetes restaurados; un cambio en el archivo de proyecto invalida la caché y vuelve a restaurar.

La etapa de runtime contiene solo la salida publicada y el runtime de ASP.NET, no el SDK.

## Construye y ejecuta

Desde la raíz del repositorio:

```sh
docker build -f qa/containers/Dockerfile -t learn-dotnet/tasktracker:local .
docker run --rm -d --name tasktracker-app -p 5080:8080 learn-dotnet/tasktracker:local
```

Compruébalo:

```sh
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5080/swagger/v1/swagger.json
```

El contenedor responde `200` y el documento lista las mismas rutas que la app local. Detenlo con `docker stop tasktracker-app`; `--rm` lo elimina.

## Compose

`docker-compose.yml` construye la misma imagen y le da a SQLite un volumen con nombre:

```yaml
services:
  api:
    build:
      context: ../..
      dockerfile: qa/containers/Dockerfile
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    volumes:
      - taskdata:/data

volumes:
  taskdata:
```

Ejecútalo desde `qa/containers`:

```sh
docker compose up --build
```

## Cómo funciona

La variable de entorno `ConnectionStrings__TaskTracker` reemplaza la cadena de conexión de `appsettings.json`. En configuración, el doble guion bajo separa secciones: `ConnectionStrings:TaskTracker` se escribe `ConnectionStrings__TaskTracker`.

El volumen conserva `/data/tasktracker.db` cuando el contenedor se reemplaza. Sin él, la base de datos vive en la capa escribible del contenedor y desaparece con él.

La app escucha en `8080` dentro del contenedor. El mapeo de puertos la publica en el host y `ASPNETCORE_URLS` le dice a Kestrel dónde enlazarse. No publiques `80` ni `5000` solo porque el perfil local los use; elige un contrato y mantenlo.

## Refactor

Una imagen de producción necesita más que este ejemplo:

- Ejecuta como usuario sin privilegios. Crea un usuario con UID fijo y dale la propiedad de `/app` y `/data`.
- Mantén los secretos fuera de la imagen. Inyecta la clave JWT y las cadenas de conexión desde un gestor de secretos o el entorno en tiempo de ejecución; no copies un `appsettings.Production.json` con secretos.
- Agrega un health check. La API puede exponer `/health` con una sonda a la base de datos y el orquestador puede reiniciar un contenedor que falla.
- Fija la imagen base por tag o digest. `6.0` es un tag móvil; un digest es reproducible.
- Escanea la imagen en CI. Una imagen de runtime con una vulnerabilidad crítica conocida debería romper el build.
- Usa PostgreSQL u otra base de datos de servidor antes de correr más de una réplica. SQLite con un volumen es un almacén de una sola instancia; dos contenedores no pueden compartir el mismo archivo de forma segura por un volumen de red.

La imagen construida para este apéndice pesa unos 365 MB. La etapa del SDK es mucho más grande, pero no se publica; solo sale la etapa de runtime.

## Verificación

El apéndice se construyó y ejecutó en local:

```text
$ docker build -f qa/containers/Dockerfile -t learn-dotnet/tasktracker:local .
$ docker run --rm -d --name tasktracker-app -p 5080:8080 learn-dotnet/tasktracker:local
$ curl -s -o /dev/null -w "%{http_code}" http://localhost:5080/swagger/v1/swagger.json
200
```

Eso demuestra que la app publicada arranca con la imagen de runtime, aplica su migración en la ruta del volumen y sirve el documento OpenAPI.

## Para cerrar

Lo que cubrimos:

- Un Dockerfile multi-stage con capas de restore y publish.
- La imagen de runtime, el puerto y el volumen de SQLite.
- El reemplazo por variables de entorno con doble guion bajo.
- Compose para ejecuciones locales.
- Usuario sin privilegios, secretos, health checks, digest fijo y almacenamiento para varias instancias.

Sigue: CI/CD.
