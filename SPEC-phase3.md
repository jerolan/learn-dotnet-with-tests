# Spec: Phase 3 - Build the application

## The application

`TaskTracker` is a small task API. A task has an integer id, a title, a status (open or done) and a creation timestamp. Later versions add due dates, ownership and tags. The point is to grow one application across the Part 3 chapters, not to invent a new domain per topic.

## Rules

- Every version lives in `build-an-app/vN-name` and is a standalone solution. Running `dotnet test` inside that folder tests it without any other version.
- Every version ships `vN-name.md` and `vN-name.es.md` with the same sections in
  the canonical order from `fundamentals/TEMPLATE.md`.
- A version starts from the previous behavior and adds one concern. The text explains the delta and why it belongs at that point.
- The README Part 3 index lists the versions one to one.
- Acceptance tests use `WebApplicationFactory<Program>` at the HTTP boundary. Unit tests are used where the chapter is about a unit, such as a validator, a cache decorator or a handler.
- The stack stays `net6.0`, MSTest 2.2.8 and Moq 4.20.70. ASP.NET Core, EF Core and the Microsoft extensions use `6.0.x` packages. New packages are pinned in the chapter that introduces them.

## Version map

| # | Folder | Scope | Exit criteria |
|---|---|---|---|
| 1 | `v1-intro` | Minimal API, in-memory store, create/list/get, a basic title guard | Acceptance tests create, list, get and reject a blank title |
| 2 | `v2-json-routing` | Request and response DTOs, route constraints, status codes, a consistent error body | Tests pin routing, 404/409/400 and JSON naming |
| 3 | `v3-configuration` | `appsettings`, the options pattern, environment overrides | A test overrides the options and the API honors the limit |
| 4 | `v4-ef-core` | EF Core 6, SQLite, `DbContext`, a migration and a repository | Tests run against SQLite; the app creates and reads tasks through EF |
| 5 | `v5-dapper` | Dapper read queries and SQL mapping | List and get read through Dapper while commands still work |
| 6 | `v6-validation` | FluentValidation for request DTOs and a consistent validation response | Invalid requests return 400 with field errors |
| 7 | `v7-auth` | JWT bearer authentication and a token endpoint | Anonymous requests get 401; a valid token gets 200 |
| 8 | `v8-di` | Service lifetimes, a decorator and an explicit composition root | Tests prove the scoped and singleton lifetimes and the decorator behavior |
| 9 | `v9-logging` | Structured logging, scopes and a correlation id | A test captures the log fields for a request |
| 10 | `v10-caching` | `IMemoryCache` for reads, invalidation on writes and a TTL | Tests prove a cache hit and invalidation after a write |
| 11 | `v11-messaging-scheduling` | A channel queue, a `BackgroundService` consumer and a scheduled cleanup | Tests drain the queue and run the scheduled work deterministically |
| 12 | `v12-openapi` | Swashbuckle, XML comments, examples and a generated OpenAPI document | A test reads the document and finds the task paths and schemas |
| 13 | `v13-resilience` | Polly retry and timeout around an outbound notifier | A fake handler proves retries and a bounded failure |
| 14 | `v14-cqrs` | MediatR commands and queries with handlers | Tests cover handlers directly and through the API |
| 15 | `v15-signalr` | SignalR broadcasts task events already processed by the app's event queue | An authenticated SignalR client receives a created task; unauthenticated negotiation is rejected |

## v15 SignalR integration

- Keep v14's API, storage, authentication and event queue as the starting point.
- Map an authenticated `/hubs/tasks` endpoint.
- Have `TaskEventProcessor` send processed task events through `IHubContext<TaskUpdatesHub>`; do not publish a second event directly from the HTTP endpoint.
- Verify the push with the .NET SignalR client through `WebApplicationFactory` and TestServer's long-polling transport.
- Keep the existing Part 4 SignalR sample as a companion appendix about hub lifecycle and delivery limits; Part 3 v15 is the book's primary SignalR path.

## Completion

- All 15 versions build and pass `dotnet test` on their own.
- Every version has the bilingual pair and is indexed in the README.
- The v15 app runs end to end, including an authenticated SignalR client receiving a task event.
- CI is green on the full tree.

## Deliberate simplifications

- SQLite is the database for the book. The SQL chapter uses the same file so the examples stay runnable without a server.
- The JWT uses a symmetric key from configuration for teaching. A production deployment would use an identity provider and key rotation.
- The message queue is in process. A production deployment would use a broker; the channel keeps the lesson deterministic and testable.
- SignalR uses the in-process event consumer and one server instance. The companion appendix explains delivery and scaling limits.

