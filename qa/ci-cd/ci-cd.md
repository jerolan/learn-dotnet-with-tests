# CI/CD beyond the test job

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/ci-cd)

A green CI run proves that the tests pass on the commit. It does not prove that you can deploy what passed. The workflow in this repository used to restore every solution from scratch on every push, run the tests and throw the results away when the job ended.

This appendix changes that workflow: it caches the packages, keeps the test results, and publishes the API as an artifact. It stops at the artifact, because there is no cloud account in this repository to deploy to.

## What the workflow did

The test job looked like this:

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 6.0.x
      - run: |
          set -euo pipefail
          for sln in $(find . -name "*.sln" -print); do
            echo "Testing $sln"
            dotnet test "$sln" --nologo
          done
```

`set -euo pipefail` is the part that makes the loop honest: without it, the job reports the exit code of the last command, and a failed solution in the middle of the list disappears. A new chapter adds a solution, and `find` picks it up without editing the workflow.

The cost is paid on every run. Each solution restores its packages again, and the first failing test stops the loop with nothing to inspect afterwards.

## Cache the packages

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      nuget-${{ runner.os }}-
```

The key changes when any project file changes. A version bump or a new package produces a new cache; a change to a `.cs` file reuses the existing one. `restore-keys` gives a partial hit when the exact key is missing, so a build after adding a project still starts from the previous cache.

## Keep the results

```yaml
- name: Restore and test
  run: |
    set -euo pipefail
    mkdir -p artifacts/test-results
    for sln in $(find . -name "*.sln" -print | sort); do
      echo "Testing $sln"
      name="${sln#./}"
      name="${name//\//_}"
      dotnet test "$sln" --nologo \
        --logger "trx;LogFileName=$name.trx" \
        --results-directory artifacts/test-results
    done
- name: Upload test results
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: test-results
    path: artifacts/test-results
```

Each solution writes its own TRX file, named from its path. Strip the leading `./` before replacing slashes: otherwise the result is a hidden file that the artifact upload omits. Replacing slashes keeps every result in one directory as a unique file name. `if: always()` runs the upload even when the test step failed, which is exactly when the results are worth keeping.

## Build once, deploy that build

```yaml
publish:
  needs: test
  if: github.ref == 'refs/heads/main'
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: 6.0.x
    - name: Publish the API
      run: |
        dotnet publish build-an-app/v15-signalr/src/TaskTracker.Api/TaskTracker.Api.csproj \
          -c Release \
          -o artifacts/api \
          /p:UseAppHost=false
    - name: Upload the API
      uses: actions/upload-artifact@v4
      with:
        name: tasktracker-api
        path: artifacts/api
```

`needs: test` makes the publish wait for green tests. The branch condition keeps pull requests from producing deployable artifacts. A deploy job would download `tasktracker-api` and copy those bytes to the target; it should not check out the repository and build again.

The tests ran against the same commit, not the same binary. If you need binary-level confidence, add a smoke test that starts the published artifact and calls one endpoint before the deploy step.

## Run the publish step here

The same command, without Actions:

```sh
dotnet publish build-an-app/v15-signalr/src/TaskTracker.Api/TaskTracker.Api.csproj \
  -c Release \
  -o /tmp/tt-publish \
  /p:UseAppHost=false
ASPNETCORE_URLS=http://localhost:5098 \
  ConnectionStrings__TaskTracker="Data Source=/tmp/tt-publish/tasktracker.db" \
  dotnet /tmp/tt-publish/TaskTracker.Api.dll
```

In another terminal:

```sh
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5098/swagger/v1/swagger.json
```

The output is `200`, and the publish folder is 38 MB. That folder contains the app and its dependencies, not the SDK; `dotnet TaskTracker.Api.dll` is enough to run it.

## Environments, secrets and rollback

A deploy job adds pieces this repository does not have:

- `environment: production` on the job, with required reviewers in the repository settings. The job waits for approval instead of deploying on every push to `main`.
- Secrets stored in that environment, not at repository scope. A staging secret cannot be read by a production job.
- `permissions: id-token: write` and a cloud login that exchanges the GitHub token for short-lived credentials. Long-lived cloud keys in repository secrets are the thing to avoid.
- An immutable tag: the commit SHA, or the image digest. Deploying `latest` makes a rollback guesswork.
- A rollback path that redeploys the previous artifact or digest. The previous version has to still exist.

## What is deferred

There is no cloud target, no credentials and no live URL in this appendix. A hands-on lab would need:

- A target that accepts a container image or a published folder, such as Container Apps, App Service or ECS.
- An OIDC trust between the repository and that target.
- A staging environment, a smoke test against it, and a promotion step to production.
- A rollback drill that deploys the previous SHA and checks the health endpoint.

Without those, a deploy job would only echo commands and call itself successful. The workflow stops at an artifact that can be downloaded and run, which is the part this repository can prove.

## Wrapping up

What we have covered:

- A test loop that fails on the first broken solution and covers every solution in the tree.
- A NuGet cache keyed on project files.
- TRX results uploaded even when the job fails.
- A publish job gated on tests and on `main`, producing a downloadable artifact.
- The local publish and run, with a `200` from the OpenAPI document.
- Environments, short-lived credentials, immutable tags and rollback, with the missing target named.

Next: cloud deployment.
