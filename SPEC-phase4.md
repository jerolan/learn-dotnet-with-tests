# Spec: Phase 4 - Q&A and appendices

## Goal

Close the roadmap with LGWT-style Q&A chapters and the 🟠 appendices. Every roadmap section gets a landing spot: a chapter, an appendix, or an explicit deferral note. The Part 4 index records the mapping.

## Rules

- Q&A chapters are prose, bilingual and link to the chapters where the runnable code lives. They do not repeat the code.
- An appendix is prose plus a runnable sample when the topic can be demonstrated cheaply and deterministically. When it cannot, the appendix is still written and ends with an explicit deferral note: what is missing, why, and what a hands-on lab would need.
- Appendices that ship code live in their own folder with their own solution. No appendix references another appendix's project.
- Every Q&A chapter and appendix has the `.md`/`.es.md` pair.
- The Part 4 index in `qa/README.md` maps the roadmap sections to their landing spots one to one.
- New packages stay on `net6.0`-compatible versions and are pinned in the appendix that introduces them.

## Coverage map

| Roadmap section | Landing spot | Status |
|---|---|---|
| C# language, .NET CLI | Part 1, chapters 1-25 | covered |
| SOLID, DRY, YAGNI, KISS | Part 1 (composition, inheritance) and Q&A design questions | covered |
| Dependency Injection | Part 1 `dependency-injection` and `build-an-app/v8-di` | covered |
| Unit Testing: MSTest, Moq | Part 1 and Part 2 | covered |
| Unit Testing: Bogus | Appendix `test-data-bogus` | covered |
| Integration Testing: WebApplicationFactory/TestServer | Part 2 and `v4-ef-core` | covered |
| Integration Testing: Respawn, Testcontainers, Docker | Appendix `integration-testing-tools` | covered |
| ASP.NET Core basics | Part 3, `v1` to `v14` | covered |
| RDBMS, SQL, EF Core, Dapper | Part 3, `v4` and `v5` | covered |
| REST APIs, OpenAPI/Swagger | Part 3, `v2` and `v12` | covered |
| AsyncAPI | Appendix `asyncapi` | planned |
| Logging | Part 3, `v9` | covered |
| Caching | Part 3, `v10` | covered |
| Messaging | Part 3, `v11` | covered |
| Task scheduling | Part 3, `v11` | covered |
| .NET libraries | Appendix `sdk-libraries` | planned |
| SignalR | Appendix `signalr` | planned |
| Snapshot testing (Verify) | Appendix `snapshot-verify` | covered |
| E2E (Playwright) | Appendix `e2e-playwright` | planned |
| Performance (K6/NBomber) | Appendix `performance-nbomber` | covered |
| Containers | Appendix `containers` | covered |
| CI/CD | Appendix `ci-cd` | covered |
| Monitoring/telemetry | Appendix `monitoring-telemetry` | covered |
| Cloud | Appendix `cloud` | planned |
| Streaming | Appendix `streaming` | planned |
| NoSQL | Appendix `nosql` | planned |
| gRPC | Appendix `grpc` | planned |
| GraphQL | Appendix `graphql` | planned |
| DevOps/IaC | Appendix `devops-iac` | planned |
| General development skills | README preface and Q&A | covered |

## Q&A chapters

1. `testing-questions`: unit versus integration versus acceptance, mocks and fakes, async tests, isolation, coverage, time and randomness, when to change the test.
2. `design-questions`: where a rule belongs, when to extract an abstraction, CQRS, caching, logging, DI lifetimes.
3. `dotnet-questions`: records versus classes, IEnumerable versus IQueryable, Task versus ValueTask, exceptions versus results, when to use Span and ref.

## Appendix order

The appendices are grouped by concern, not by difficulty:

- Testing tools: `test-data-bogus`, `integration-testing-tools`, `snapshot-verify`, `e2e-playwright`, `performance-nbomber`.
- Delivery: `containers`, `ci-cd`, `monitoring-telemetry`, `cloud`, `devops-iac`.
- Protocols and storage: `grpc`, `graphql`, `signalr`, `asyncapi`, `streaming`, `nosql`.
- Libraries: `sdk-libraries`.

## Completion

- Every section in the coverage map has a status of covered, appendix or deferred, and the appendix file exists.
- Every Q&A chapter and appendix is bilingual.
- Every appendix that claims a runnable sample builds and its tests pass; deferral notes say what is missing.
- The Part 4 index links every written chapter and appendix.
- CI is green.

