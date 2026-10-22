# Part 4: Q&A and Appendices

This part closes the roadmap. The Q&A chapters answer the questions that do not need a new project. The appendices cover the 🟠 topics that sit outside the main TDD path; each one names what is demonstrated and what is deferred.

## Q&A

1. 🟠 [Testing Questions](testing-questions.md) ([ES](testing-questions.es.md)) - Unit, integration and acceptance tests, fakes versus mocks, async tests, isolation, coverage and error paths.
2. 🟠 Design Questions - where a rule belongs, when to extract an abstraction, CQRS, caching, logging and DI lifetimes. *Planned.*
3. 🟠 .NET Questions - records versus classes, IEnumerable versus IQueryable, Task versus ValueTask, exceptions versus results, Span and ref. *Planned.*

## Appendices

- 🟠 Snapshot testing with Verify. *Planned.*
- 🟠 End-to-end testing with Playwright. *Planned.*
- 🟠 Performance testing with NBomber or K6. *Planned.*
- 🟠 Test data with Bogus. *Planned.*
- 🟠 Integration testing tools: Respawn, Testcontainers and Docker. *Planned.*
- 🟠 Containers for the TaskTracker API. *Planned.*
- 🟠 CI/CD beyond the single test job. *Planned.*
- 🟠 Monitoring and telemetry. *Planned.*
- 🟠 Cloud deployment. *Planned.*
- 🟠 Streaming. *Planned.*
- 🟠 NoSQL. *Planned.*
- 🟠 gRPC. *Planned.*
- 🟠 GraphQL. *Planned.*
- 🟠 SignalR. *Planned.*
- 🟠 AsyncAPI. *Planned.*
- 🟠 SDK libraries. *Planned.*
- 🟠 DevOps and infrastructure as code. *Planned.*

## Roadmap coverage

| Roadmap section | Landing spot | Status |
|---|---|---|
| C# language, .NET CLI | [Part 1](../fundamentals/install/install.md) | covered |
| SOLID, DRY, YAGNI, KISS | [Composition](../fundamentals/composition/composition.md) and testing Q&A | covered |
| Dependency Injection | [Dependency Injection](../fundamentals/dependency-injection/dependency-injection.md), [v8](../build-an-app/v8-di/v8-di.md) | covered |
| Unit Testing: MSTest, Moq | [Mocking](../fundamentals/mock/mock.md), [Part 2](../testing/acceptance-tests/acceptance-tests.md) | covered |
| Unit Testing: Bogus | Bogus appendix | planned |
| Integration Testing: WebApplicationFactory, TestServer | [Acceptance Tests](../testing/acceptance-tests/acceptance-tests.md), [v4](../build-an-app/v4-ef-core/v4-ef-core.md) | covered |
| Integration Testing: Respawn, Testcontainers, Docker | Integration tools appendix | planned |
| ASP.NET Core basics | [v1](../build-an-app/v1-intro/v1-intro.md) to [v14](../build-an-app/v14-cqrs/v14-cqrs.md) | covered |
| RDBMS, SQL, EF Core, Dapper | [v4](../build-an-app/v4-ef-core/v4-ef-core.md), [v5](../build-an-app/v5-dapper/v5-dapper.md) | covered |
| REST APIs, OpenAPI/Swagger | [v2](../build-an-app/v2-json-routing/v2-json-routing.md), [v12](../build-an-app/v12-openapi/v12-openapi.md) | covered |
| AsyncAPI | AsyncAPI appendix | planned |
| Logging | [v9](../build-an-app/v9-logging/v9-logging.md) | covered |
| Caching | [v10](../build-an-app/v10-caching/v10-caching.md) | covered |
| Messaging | [v11](../build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.md) | covered |
| Task scheduling | [v11](../build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.md) | covered |
| .NET libraries | SDK libraries appendix | planned |
| SignalR | SignalR appendix | planned |
| Snapshot, E2E, performance | Snapshot, Playwright and NBomber appendices | planned |
| Containers, CI/CD, monitoring, cloud, DevOps | Delivery appendices | planned |
| Streaming, NoSQL, gRPC, GraphQL | Protocol and storage appendices | planned |
| General development skills | README preface and testing Q&A | covered |

