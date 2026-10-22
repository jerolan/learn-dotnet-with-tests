# Cloud deployment

[You can find the code referenced by this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/cloud)

The CI job publishes a folder and the containers appendix builds an image. Neither one is running for a user. A cloud host adds the parts a laptop hides: a public port, configuration per environment, secrets, a database that outlives the process, health probes and a way back when a release goes wrong.

There is no cloud subscription in this repository, so this appendix does not deploy anything. It names the decisions and shows the deploy job that would follow the publish artifact.

## What the app already needs

- One port. The container listens on `8080`; App Service and Container Apps expect the app to bind to the port they provide through `PORT` or `WEBSITES_PORT`.
- Configuration from the environment. `ConnectionStrings__TaskTracker` and the JWT key are read from configuration; the defaults in `appsettings.json` are for local runs.
- A writable data path. SQLite needs `/data`; a managed database needs a connection string and network access.
- A health endpoint. The container can answer `/health` before it can serve tasks.
- Logs on stdout. The platform collects them; a file inside the container disappears with the instance.

## Choose a target

| Target | Fits when | Costs you |
|---|---|---|
| App Service | A single web app, slots for staging and swap, minimal platform work | Less control over the host; scale and networking are platform features |
| Container Apps | A container, revisions and traffic splitting, scale to zero | Cold starts; a revision model to learn |
| ECS on Fargate | AWS shop, task definitions and ALB integration | More pieces: cluster, service, load balancer, roles |
| Kubernetes | Many services, existing cluster and platform team | The cluster is now your product |

The choice changes the commands, not the application contract. The app still needs a port, configuration, health and logs.

## Configuration and secrets

Keep non-secret settings in the platform's configuration: environment name, log level, allowed hosts. Keep secrets in Key Vault, Secrets Manager or the platform's secret store, and reference them from app settings.

```text
ConnectionStrings__TaskTracker = @Microsoft.KeyVault(SecretUri=https://.../secrets/tasktracker-db)
Jwt__Key                        = @Microsoft.KeyVault(SecretUri=https://.../secrets/jwt-key)
```

The app reads them the same way it reads `appsettings.json`. The double underscore is the section separator.

Prefer a managed identity over a connection string with a password. The app authenticates as the platform identity, and the secret never exists in the app configuration. If the database only supports a password, rotate it in the secret store and restart, not in a commit.

## The database

SQLite with a volume is a single-instance store. Two containers cannot safely share the file over a network volume, and a platform that replaces the instance can lose the disk. Use a managed PostgreSQL or SQL Server before the second replica.

Run migrations as a separate step, not from every instance at startup. Two instances starting together will race the same migration. A deploy job can run `dotnet ef database update` once, with the migration connection string, before the new app version receives traffic.

Keep the schema compatible with the previous version during the rollout: add a column as nullable, deploy code that writes both shapes, backfill, then remove the old shape in a later release. A rollback that cannot read the new schema is not a rollback.

## Health, readiness and startup

A liveness probe answers "is the process alive". A readiness probe answers "can it serve traffic". They are not the same:

- Liveness should not check the database. A database outage would restart every healthy instance and make the outage worse.
- Readiness can check the database and the required downstream services. A failing readiness removes the instance from the load balancer without killing it.
- A startup probe gives a slow migration or warm-up time before liveness begins.

The app should expose separate endpoints if the platform supports both, and the deploy should wait for readiness before shifting traffic.

## Deploy the artifact

The publish job uploads `tasktracker-api`. A deploy job downloads exactly that artifact, logs in with OIDC, and deploys it:

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

`environment: production` waits for the reviewers configured on that environment. `id-token: write` lets the job exchange the GitHub token for short-lived cloud credentials; no cloud password is stored in the repository.

For a container host, the same job would push the image tagged with the commit SHA and update the revision to that tag. A moving `latest` tag makes a rollback a guess.

## Rollback and scale

App Service slots swap back in seconds. Container Apps can shift traffic between revisions; Kubernetes keeps the previous ReplicaSet. In every case, the previous artifact or image must still exist, and the database must still accept it.

Set a minimum instance count above zero if cold starts are unacceptable. Autoscale on the signal that matters: request rate or latency, not only CPU. A CPU-based rule reacts after the queue has already grown.

Watch the connection pool: ten instances with a pool of one hundred open a thousand connections. The database plan has a limit, and the failure looks like timeouts under load.

## What is deferred

There is no subscription, registry, domain, certificate or live URL here. A hands-on lab would:

- Create a resource group and a managed PostgreSQL instance.
- Push the image to a registry and deploy a Container App with a health probe.
- Configure OIDC between the repository and the subscription.
- Deploy a bad revision, watch readiness fail and roll back to the previous digest.
- Run a load test against the deployed URL and compare it with the NBomber local number.

## Wrapping up

What we have covered:

- The port, configuration, data, health and log needs of the published app.
- App Service, Container Apps, ECS and Kubernetes as targets with different trade-offs.
- Secrets in a store, managed identity and the database that replaces SQLite.
- Migrations as a separate step and schema changes that survive a rollback.
- A deploy job that downloads the tested artifact and logs in with OIDC.
- Readiness, slots, revisions, immutable tags and connection-pool limits.

Next: DevOps and infrastructure as code.
