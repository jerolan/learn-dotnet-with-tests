# Containers

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/containers)

A container packages the application and its runtime into one image. This appendix builds the v15 TaskTracker API with a multi-stage Dockerfile, runs it with SQLite on a volume and checks the OpenAPI document from outside the container.

## The Dockerfile

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

The build stage restores before copying the source. A change to the code reuses the restored packages; a change to the project file invalidates the cache and restores again.

The runtime stage contains only the published output and the ASP.NET runtime, not the SDK.

## Build and run

From the repository root:

```sh
docker build -f qa/containers/Dockerfile -t learn-dotnet/tasktracker:local .
docker run --rm -d --name tasktracker-app -p 5080:8080 learn-dotnet/tasktracker:local
```

Check it:

```sh
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5080/swagger/v1/swagger.json
```

The container answers `200` and the document lists the same paths as the local app. Stop it with `docker stop tasktracker-app`; `--rm` removes it.

## Compose

`docker-compose.yml` builds the same image and gives SQLite a named volume:

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

Run it from `qa/containers`:

```sh
docker compose up --build
```

## How it works

The `ConnectionStrings__TaskTracker` environment variable overrides the connection string from `appsettings.json`. In configuration, a double underscore is a section separator: `ConnectionStrings:TaskTracker` becomes `ConnectionStrings__TaskTracker`.

The volume keeps `/data/tasktracker.db` when the container is replaced. Without it, the database lives in the container's writable layer and disappears with the container.

The app listens on `8080` inside the container. The port mapping publishes it on the host, and `ASPNETCORE_URLS` tells Kestrel where to bind. Do not publish `80` or `5000` just because the local profile uses them; choose one contract and keep it.

## Refactor

A production image needs more than this sample:

- Run as a non-root user. Create a user with a fixed UID and own `/app` and `/data`.
- Keep secrets out of the image. Inject the JWT key and connection strings from a secret manager or environment at run time; do not copy an `appsettings.Production.json` with secrets.
- Add a health check. The API can expose `/health` with a database probe, and the orchestrator can restart a failing container.
- Pin the base image by tag or digest. `6.0` is a moving tag; a digest is reproducible.
- Scan the image in CI. A runtime image with a known critical vulnerability should fail the build.
- Use PostgreSQL or another server database before running more than one replica. SQLite with a volume is a single-instance store; two containers cannot safely share the same file over a network volume.

The image built for this appendix is about 365 MB. The SDK stage is much larger but does not ship; only the runtime stage is published.

## Verification

The appendix was built and run locally:

```text
$ docker build -f qa/containers/Dockerfile -t learn-dotnet/tasktracker:local .
$ docker run --rm -d --name tasktracker-app -p 5080:8080 learn-dotnet/tasktracker:local
$ curl -s -o /dev/null -w "%{http_code}" http://localhost:5080/swagger/v1/swagger.json
200
```

That proves the published app starts with the runtime image, applies its migration to the volume path and serves the OpenAPI document.

## Wrapping up

What we have covered:

- A multi-stage Dockerfile with restore and publish layers.
- The runtime image, the port and the SQLite volume.
- The double-underscore environment override.
- Compose for local runs.
- Non-root, secrets, health checks, digest pinning and multi-instance storage.

Next: CI/CD.

