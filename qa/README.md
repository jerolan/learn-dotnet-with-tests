# Part 4: Q&A and Appendices

This part closes the roadmap. The Q&A chapters answer the questions that do not need a new project. The appendices cover the 🟠 topics that sit outside the main TDD path; each one names what is demonstrated and what is deferred.

## Q&A

1. 🟠 [Testing Questions](testing-questions.md) ([ES](testing-questions.es.md)) - Unit, integration and acceptance tests, fakes versus mocks, async tests, isolation, coverage and error paths.
2. 🟠 [Design Questions](design-questions.md) ([ES](design-questions.es.md)) - Where a rule belongs, when to extract an abstraction, CQRS, caching, logging and DI lifetimes.
3. 🟠 [.NET Questions](dotnet-questions.md) ([ES](dotnet-questions.es.md)) - Records versus classes, IEnumerable versus IQueryable, Task versus ValueTask, exceptions versus results, Span and ref.

## Appendices

- 🟠 [Snapshot testing with Verify](snapshot-verify/snapshot-verify.md) ([ES](snapshot-verify/snapshot-verify.es.md)) - Approve a formatted output with a verified file and review its diffs.
- 🟠 [End-to-end testing with Playwright](e2e-playwright/e2e-playwright.md) ([ES](e2e-playwright/e2e-playwright.es.md)) - Start Kestrel on a free port and drive the task board in Chromium.
- 🟠 [Performance testing with NBomber](performance-nbomber/performance-nbomber.md) ([ES](performance-nbomber/performance-nbomber.es.md)) - Run a fixed load against an API and read throughput and percentiles.
- 🟠 [Test data with Bogus](test-data-bogus/test-data-bogus.md) ([ES](test-data-bogus/test-data-bogus.es.md)) - Generate valid orders from seeded rules and keep literal values for the edges.
- 🟠 [Integration testing tools: Respawn, Testcontainers and Docker](integration-testing-tools/integration-testing-tools.md) ([ES](integration-testing-tools/integration-testing-tools.es.md)) - Start PostgreSQL in Docker and reset it between tests.
- 🟠 [Containers for the TaskTracker API](containers/containers.md) ([ES](containers/containers.es.md)) - Build a multi-stage image, run it with SQLite on a volume and check OpenAPI from outside.
- 🟠 [CI/CD beyond the test job](ci-cd/ci-cd.md) ([ES](ci-cd/ci-cd.es.md)) - Cache packages, keep TRX results and publish the API as an artifact gated on tests.
- 🟠 [Monitoring and telemetry](monitoring-telemetry/monitoring-telemetry.md) ([ES](monitoring-telemetry/monitoring-telemetry.es.md)) - Record a tagged counter and capture it with a MeterListener.
- 🟠 [Cloud deployment](cloud/cloud.md) ([ES](cloud/cloud.es.md)) - Map the published artifact to a host: configuration, secrets, migrations, probes, rollback and a deploy job.
- 🟠 [Streaming with IAsyncEnumerable](streaming/streaming.md) ([ES](streaming/streaming.es.md)) - Yield tasks as they arrive, cancel halfway and keep the items delivered before a failure.
- 🟠 [NoSQL with MongoDB](nosql/nosql.md) ([ES](nosql/nosql.es.md)) - Store documents in MongoDB, start it with Testcontainers and fix a case-sensitive sort with a collation.
- 🟠 [gRPC](grpc/grpc.md) ([ES](grpc/grpc.es.md)) - Define a proto contract, generate the service and client, and call it through TestServer.
- 🟠 [GraphQL](graphql/graphql.md) ([ES](graphql/graphql.es.md)) - Publish a query and a mutation over one endpoint and test the documents over HTTP.
- 🟠 [SignalR hub lifecycle and delivery limits](signalr/signalr.md) ([ES](signalr/signalr.es.md)) - Companion to Part 3 v15, focused on connection lifecycle and delivery limits.
- 🟠 [AsyncAPI](asyncapi/asyncapi.md) ([ES](asyncapi/asyncapi.es.md)) - Describe the TaskCreated event as a contract and validate it with the official CLI.
- 🟠 [SDK libraries](sdk-libraries/sdk-libraries.md) ([ES](sdk-libraries/sdk-libraries.es.md)) - Build a typed HTTP client, test it with a stub handler and pack it as a NuGet package.
- 🟠 [DevOps and infrastructure as code](devops-iac/devops-iac.md) ([ES](devops-iac/devops-iac.es.md)) - Compile a Bicep template, give the app an identity and read secrets from a vault.

## Roadmap coverage

| Roadmap section | Landing spot | Status |
|---|---|---|
| C# language, .NET CLI | [Part 1](../fundamentals/install/install.md) | covered |
| SOLID, DRY, YAGNI, KISS | [Composition](../fundamentals/composition/composition.md) and testing Q&A | covered |
| Dependency Injection | [Dependency Injection](../fundamentals/dependency-injection/dependency-injection.md), [v8](../build-an-app/v8-di/v8-di.md) | covered |
| Unit Testing: MSTest, Moq | [Mocking](../fundamentals/mock/mock.md), [Part 2](../testing/acceptance-tests/acceptance-tests.md) | covered |
| Unit Testing: Bogus | [Test data with Bogus](test-data-bogus/test-data-bogus.md) | covered |
| Integration Testing: WebApplicationFactory, TestServer | [Acceptance Tests](../testing/acceptance-tests/acceptance-tests.md), [v4](../build-an-app/v4-ef-core/v4-ef-core.md) | covered |
| Integration Testing: Respawn | [Integration tools](integration-testing-tools/integration-testing-tools.md) | covered |
| Integration Testing: Testcontainers, Docker (roadmap image) | [Integration tools](integration-testing-tools/integration-testing-tools.md) | covered |
| ASP.NET Core basics | [v1](../build-an-app/v1-intro/v1-intro.md) to [v15](../build-an-app/v15-signalr/v15-signalr.md) | covered |
| RDBMS, SQL, EF Core, Dapper | [v4](../build-an-app/v4-ef-core/v4-ef-core.md), [v5](../build-an-app/v5-dapper/v5-dapper.md) | covered |
| REST APIs, OpenAPI/Swagger | [v2](../build-an-app/v2-json-routing/v2-json-routing.md), [v12](../build-an-app/v12-openapi/v12-openapi.md) | covered |
| AsyncAPI | [AsyncAPI](asyncapi/asyncapi.md) | covered |
| Logging | [v9](../build-an-app/v9-logging/v9-logging.md) | covered |
| Caching | [v10](../build-an-app/v10-caching/v10-caching.md) | covered |
| Messaging | [v11](../build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.md) | covered |
| Task scheduling | [v11](../build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.md) | covered |
| .NET libraries | [SDK libraries](sdk-libraries/sdk-libraries.md) | covered |
| SignalR | [Primary integration: v15](../build-an-app/v15-signalr/v15-signalr.md); [companion appendix](signalr/signalr.md) covers hub lifecycle and delivery limits | covered |
| Snapshot, E2E, performance | [Snapshot](snapshot-verify/snapshot-verify.md), [Playwright](e2e-playwright/e2e-playwright.md), [NBomber](performance-nbomber/performance-nbomber.md) | covered |
| Containers, CI/CD, monitoring, cloud, DevOps | [Containers](containers/containers.md), [CI/CD](ci-cd/ci-cd.md), [Monitoring](monitoring-telemetry/monitoring-telemetry.md), [Cloud](cloud/cloud.md), [DevOps](devops-iac/devops-iac.md) | covered |
| Streaming, NoSQL, gRPC, GraphQL | [gRPC](grpc/grpc.md), [GraphQL](graphql/graphql.md), [NoSQL](nosql/nosql.md), [Streaming](streaming/streaming.md) | covered |
| General development skills | README preface and testing Q&A | covered |

**Roadmap source:** The [roadmap project](https://github.com/Elfocrash/.NET-Backend-Developer-Roadmap) text map `map.json` lists WebApplicationFactory, TestServer and Respawn; the roadmap image adds Testcontainers and Docker. Part 2 and Part 3 cover WebApplicationFactory/TestServer, and the [integration tools appendix](integration-testing-tools/integration-testing-tools.md) covers Respawn, Testcontainers and Docker.

