# Learn .NET with Tests

Welcome to "Learn .NET with Tests," your journey into the world of test-driven development (TDD) within the .NET ecosystem. This guide is designed to help you master the fundamentals of TDD, a development approach that emphasizes writing tests before writing the code itself. By following this guide, you will learn not just how to write tests, but also how to think about software design and development in a way that improves code quality, reduces bugs, and enhances your productivity as a developer.

This guide is inspired by the amazing [Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/) by Chris James. I love the way he explains the concepts and the way he guides the reader through the process of learning TDD.

## What Is Test-Driven Development?

Test-Driven Development is a software development process where you write a test for a piece of functionality before you actually write the code to implement that functionality. The process follows a simple cycle known as Red-Green-Refactor:

- **Red**: Write a test for the next bit of functionality you want to add. Run the test, which should fail because the functionality isn't implemented yet.
- **Green**: Write the minimal amount of code necessary to make the test pass. This often means the code is not perfect.
- **Refactor**: Clean up the new code, ensuring it fits well with the existing codebase, without changing its behavior. The tests should still pass after refactoring.

This cycle encourages developers to write only the code necessary to pass tests, leading to cleaner, more efficient codebases.

## Why Learn TDD with .NET?

.NET is a versatile and powerful platform that supports multiple languages (C#, F#, VB.NET) and runs on various operating systems. It's used for developing a wide range of applications, from web and mobile to desktop and cloud-based apps. Learning TDD within the .NET ecosystem allows you to:

- **Improve Code Quality**: Tests written before the code ensure that your application works as intended and continues to work as you add features or refactor.
- **Enhance Your Design Skills**: TDD encourages you to think about the design of your application upfront, leading to more modular and flexible code.
- **Become More Efficient**: While it might seem slower at first, TDD can save time by catching bugs early and reducing the need for debugging.

## What Will You Learn?

### C# fundamentals

1. 🟢 [Install .NET](fundamentals/install/install.md) ([ES](fundamentals/install/install.es.md)) - Set up your environment to write and run C# code, including installing the .NET SDK and choosing an IDE or editor.
2. 🟢 [Hello, World](fundamentals/hello-world/hello-world.md) ([ES](fundamentals/hello-world/hello-world.es.md)) - Write your first program, separate the greeting from the console, and grow it test first.
3. 🟢 [Types and Variables](fundamentals/types/types.md) ([ES](fundamentals/types/types.es.md)) - Work with integers, doubles and booleans through failing tests and a table of cases.
4. 🟢 [Decimals](fundamentals/decimal/decimal.md) ([ES](fundamentals/decimal/decimal.es.md)) - See why money needs `decimal` and build a `Money` value with an explicit rounding policy.
5. 🟢 [Declarations, Const and Var](fundamentals/declarations/declarations.md) ([ES](fundamentals/declarations/declarations.es.md)) - Learn when a value is a variable, a compile-time constant or a readonly field.
6. 🟢 [Control Flow](fundamentals/control-flow/control-flow.md) ([ES](fundamentals/control-flow/control-flow.es.md)) - Use `for`, `while`, `if` and a switch expression to drive behavior.
7. 🟢 [Exceptions](fundamentals/exceptions/exceptions.md) ([ES](fundamentals/exceptions/exceptions.es.md)) - Make the error path part of the contract with a domain exception.

### Object-Oriented Programming

8. 🟢 [Properties and Methods](fundamentals/methods/methods.md) ([ES](fundamentals/methods/methods.es.md)) - Define methods, properties, optional parameters and named arguments.
9. 🟢 [Classes and Objects](fundamentals/classes/classes.md) ([ES](fundamentals/classes/classes.es.md)) - Model state with a constructor, a private setter and an invariant.
10. 🟢 [Structs and Records](fundamentals/records/records.md) ([ES](fundamentals/records/records.es.md)) - Compare value copying in structs with value equality in records.
11. 🟢 [`with` Keyword](fundamentals/with/with.md) ([ES](fundamentals/with/with.es.md)) - Create changed copies of records without mutating the original.
12. 🟢 [Inheritance and Interfaces](fundamentals/inheritance/inheritance.md) ([ES](fundamentals/inheritance/inheritance.es.md)) - Share state with a base class and behavior with an interface.
13. 🟢 [Composition over Inheritance](fundamentals/composition/composition.md) ([ES](fundamentals/composition/composition.es.md)) - Assemble behavior from small parts instead of multiplying subclasses.

### Advanced Topics

14. 🟢 [Dependency Injection](fundamentals/dependency-injection/dependency-injection.md) ([ES](fundamentals/dependency-injection/dependency-injection.es.md)) - Receive dependencies through the constructor and keep the composition root at the edge.
15. 🟢 [Mocking](fundamentals/mock/mock.md) ([ES](fundamentals/mock/mock.es.md)) - Use Moq at an I/O boundary and verify the interactions that are part of the contract.
16. 🟢 [Collections](fundamentals/collections/collections.md) ([ES](fundamentals/collections/collections.es.md)) - Choose between arrays, lists, dictionaries and sets with a word counter.
17. 🟢 [Generics](fundamentals/generics/generics.md) ([ES](fundamentals/generics/generics.es.md)) - Write a repository once and use it with several entity types.
18. 🟢 [IEnumerable](fundamentals/enumerable/enumerable.md) ([ES](fundamentals/enumerable/enumerable.es.md)) - Build lazy sequences with `yield` and understand deferred execution.
19. 🟢 [LINQ](fundamentals/linq/linq.md) ([ES](fundamentals/linq/linq.es.md)) - Filter, group, order and aggregate sequences with method syntax.
20. 🟢 [IQueryable](fundamentals/queriable/queriable.md) ([ES](fundamentals/queriable/queriable.es.md)) - See how a query becomes an expression tree and why the provider matters.
21. 🟢 [`ref` Keyword](fundamentals/ref/ref.md) ([ES](fundamentals/ref/ref.es.md)) - Pass references, return references and pass readonly structs with `in`.
22. 🟢 [Tasks](fundamentals/tasks/tasks.md) ([ES](fundamentals/tasks/tasks.es.md)) - Combine asynchronous work with `WhenAll` and `WhenAny`.
23. 🟢 [Async/Await](fundamentals/async/async.md) ([ES](fundamentals/async/async.es.md)) - Write asynchronous methods, propagate exceptions and honor cancellation.
24. 🟢 [Files and I/O](fundamentals/io/io.md) ([ES](fundamentals/io/io.es.md)) - Read and write through `TextReader` and `TextWriter` without touching the disk in tests.
25. 🟢 [`using` Keyword](fundamentals/using/using.md) ([ES](fundamentals/using/using.es.md)) - Release resources deterministically with `IDisposable` and `using`.

## Part 2: Testing fundamentals

These chapters turn the testing habits from Part 1 on the test suite itself. They are the bridge to the application in Part 3.

1. 🟢 [Acceptance Tests](testing/acceptance-tests/acceptance-tests.md) ([ES](testing/acceptance-tests/acceptance-tests.es.md)) - Test a feature through HTTP with WebApplicationFactory and keep the test at the user-visible boundary.
2. 🟢 [Scaling Acceptance Tests](testing/scaling-acceptance-tests/scaling-acceptance-tests.md) ([ES](testing/scaling-acceptance-tests/scaling-acceptance-tests.es.md)) - Keep many scenarios readable with a test driver, builders and isolated factories.
3. 🟢 [Working Without Mocks](testing/working-without-mocks/working-without-mocks.md) ([ES](testing/working-without-mocks/working-without-mocks.es.md)) - Replace interaction mocks with a small in-memory fake when the collaborator is yours.
4. 🟢 [Refactoring Checklist](testing/refactoring-checklist/refactoring-checklist.md) ([ES](testing/refactoring-checklist/refactoring-checklist.es.md)) - Change structure with the tests green and a repeatable list of checks.

## Part 3: Build an application

One API, grown version by version. Each version is a standalone solution with its own tests.

1. 🟢 [v1: The API and its first tests](build-an-app/v1-intro/v1-intro.md) ([ES](build-an-app/v1-intro/v1-intro.es.md)) - Create the TaskTracker API with a minimal endpoint set and its first acceptance tests.
2. 🟢 [v2: JSON, routing and a consistent error shape](build-an-app/v2-json-routing/v2-json-routing.md) ([ES](build-an-app/v2-json-routing/v2-json-routing.es.md)) - Pin the wire contract with named errors, camelCase JSON and a PATCH route.
3. 🟢 [v3: Configuration and options](build-an-app/v3-configuration/v3-configuration.md) ([ES](build-an-app/v3-configuration/v3-configuration.es.md)) - Bind a typed options object from appsettings and override it in the test host.
4. 🟢 [v4: EF Core and SQLite](build-an-app/v4-ef-core/v4-ef-core.md) ([ES](build-an-app/v4-ef-core/v4-ef-core.es.md)) - Persist tasks with a code-first model, a migration and a repository.
5. 🟢 [v5: Dapper read queries](build-an-app/v5-dapper/v5-dapper.md) ([ES](build-an-app/v5-dapper/v5-dapper.es.md)) - Split reads and writes: Dapper queries the same SQLite rows that EF stores.
6. 🟢 [v6: Validation](build-an-app/v6-validation/v6-validation.md) ([ES](build-an-app/v6-validation/v6-validation.es.md)) - Move title rules into FluentValidation and return per-field errors.
7. 🟢 [v7: JWT authentication](build-an-app/v7-auth/v7-auth.md) ([ES](build-an-app/v7-auth/v7-auth.es.md)) - Issue a token and protect the task routes with a bearer policy.
8. 🟢 [v8: Lifetimes and a decorator](build-an-app/v8-di/v8-di.md) ([ES](build-an-app/v8-di/v8-di.es.md)) - Expose scoped and singleton services and count reads with a decorator.
9. 🟢 [v9: Structured logging](build-an-app/v9-logging/v9-logging.md) ([ES](build-an-app/v9-logging/v9-logging.es.md)) - Log named fields, add a correlation scope and capture logs in tests.
10. 🟢 [v10: Caching](build-an-app/v10-caching/v10-caching.md) ([ES](build-an-app/v10-caching/v10-caching.es.md)) - Cache reads with IMemoryCache and invalidate on writes.
11. 🟢 [v11: Messaging and scheduling](build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.md) ([ES](build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.es.md)) - Publish events to a channel, consume them in a hosted service and schedule cleanup.
12. 🟢 [v12: OpenAPI](build-an-app/v12-openapi/v12-openapi.md) ([ES](build-an-app/v12-openapi/v12-openapi.es.md)) - Generate the API document, declare Bearer auth and test the contract.
13. 🟢 [v13: Resilience](build-an-app/v13-resilience/v13-resilience.md) ([ES](build-an-app/v13-resilience/v13-resilience.es.md)) - Retry transient notifier failures with a bounded Polly policy.
14. 🟢 [v14: CQRS with MediatR](build-an-app/v14-cqrs/v14-cqrs.md) ([ES](build-an-app/v14-cqrs/v14-cqrs.es.md)) - Move use cases into command and query handlers while keeping HTTP at the edge.

## Part 4: Q&A and appendices

The closing part answers the questions that do not need a new project and maps the remaining roadmap sections to their appendices.

- 🟠 [Part 4 index](qa/README.md) ([ES](qa/README.es.md)) - The coverage map and the appendix list.
- 🟠 [Testing Questions](qa/testing-questions.md) ([ES](qa/testing-questions.es.md)) - Unit, integration and acceptance tests, fakes, async tests, isolation and coverage.
- 🟠 [Design Questions](qa/design-questions.md) ([ES](qa/design-questions.es.md)) - Rules, boundaries, CQRS, caching, logging and DI lifetimes.
- 🟠 [.NET Questions](qa/dotnet-questions.md) ([ES](qa/dotnet-questions.es.md)) - Records, sequences, tasks, money, collections and ref.
- 🟠 [Snapshot Testing with Verify](qa/snapshot-verify/snapshot-verify.md) ([ES](qa/snapshot-verify/snapshot-verify.es.md)) - Approve a formatted output with a verified file and review its diffs.
- 🟠 [Monitoring and Telemetry](qa/monitoring-telemetry/monitoring-telemetry.md) ([ES](qa/monitoring-telemetry/monitoring-telemetry.es.md)) - Record a tagged counter and capture it in a test.
- 🟠 [Integration Testing Tools](qa/integration-testing-tools/integration-testing-tools.md) ([ES](qa/integration-testing-tools/integration-testing-tools.es.md)) - Start PostgreSQL with Testcontainers and reset it with Respawn.
- 🟠 [Performance Testing with NBomber](qa/performance-nbomber/performance-nbomber.md) ([ES](qa/performance-nbomber/performance-nbomber.es.md)) - Run a fixed load and read throughput and latency percentiles.
- 🟠 [Containers for the TaskTracker API](qa/containers/containers.md) ([ES](qa/containers/containers.es.md)) - Build a multi-stage image, persist SQLite on a volume and check OpenAPI from outside.
- 🟠 [Test Data with Bogus](qa/test-data-bogus/test-data-bogus.md) ([ES](qa/test-data-bogus/test-data-bogus.es.md)) - Generate valid orders from seeded rules and keep literal values for the edges.
- 🟠 [CI/CD beyond the test job](qa/ci-cd/ci-cd.md) ([ES](qa/ci-cd/ci-cd.es.md)) - Cache packages, keep test results and publish the API as an artifact.
- 🟠 [Real-Time Updates with SignalR](qa/signalr/signalr.md) ([ES](qa/signalr/signalr.es.md)) - Push a created task to a connected client and test the hub through TestServer.
- 🟠 [gRPC](qa/grpc/grpc.md) ([ES](qa/grpc/grpc.es.md)) - Define a proto contract, generate the service and client, and call it through TestServer.
- 🟠 [GraphQL](qa/graphql/graphql.md) ([ES](qa/graphql/graphql.es.md)) - Publish a query and a mutation over one endpoint and test the documents over HTTP.
