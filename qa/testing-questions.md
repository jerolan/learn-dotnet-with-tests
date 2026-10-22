# Testing Questions

These are the questions that come up after the first chapters: which test to write, what to replace, and when a red test means the code is wrong. Each answer points to the chapter where the idea is demonstrated.

## What is the difference between a unit, integration and acceptance test?

A unit test checks one piece of behavior with its collaborators replaced. An integration test checks several real pieces together, often with a database or an HTTP stack. An acceptance test checks a feature through the boundary a user uses.

The acceptance tests in [Part 2](../testing/acceptance-tests/acceptance-tests.md) go through HTTP with `WebApplicationFactory`. The repository tests in [v4](../build-an-app/v4-ef-core/v4-ef-core.md) use a real SQLite database. The handler tests in [Part 1](../fundamentals/methods/methods.md) are unit tests.

The labels matter less than the question each test answers. If a test tells you whether the feature works, keep it even if someone calls it an integration test.

## When should I use a mock, a fake or the real thing?

Use the real thing when it is fast, deterministic and part of what you are testing. Use a fake when you own the interface and a small in-memory implementation is enough. Use a mock when the interaction itself is the contract, or when the dependency is an external boundary you cannot run in the test.

[Working Without Mocks](../testing/working-without-mocks/working-without-mocks.md) shows a fake replacing a repository. [Mocking](../fundamentals/mock/mock.md) shows Moq verifying a call. The two chapters are deliberately adjacent: most collaborators deserve a fake, not a mock.

## How do I test asynchronous code without flaky delays?

Control completion instead of waiting for a clock. `TaskCompletionSource<T>` lets a test decide when a task finishes; a fake async dependency returns `Task.FromResult`, `Task.FromException` or a canceled task. The [Tasks chapter](../fundamentals/tasks/tasks.md) uses that technique for `WhenAll` and `WhenAny`.

If a test needs a real timeout, make the timeout generous and assert on an observable condition, not on a fixed sleep. The background-worker test in [v11](../build-an-app/v11-messaging-scheduling/v11-messaging-scheduling.md) waits for an audit entry with a deadline.

## How do I test code that depends on time or randomness?

Put the dependency behind an interface and inject it. A `TimeProvider` or a small `IClock` lets the test move time deliberately; an `IRandomSource` or a seeded random generator makes a sequence reproducible. Then test the behavior at a chosen time or with a chosen sequence.

Do not assert on `DateTime.UtcNow` inside production code. A test that races the clock will eventually fail on a slow CI machine.

## How do I keep tests independent?

Each test should create its own state. The test drivers in Part 3 create a fresh `WebApplicationFactory`, a fresh SQLite connection or a fresh database file. The in-memory store in [v1](../build-an-app/v1-intro/v1-intro.md) lived per factory for the same reason.

Avoid static mutable state and shared collections. If a fixture is expensive, reset it between tests instead of ordering the tests. A test that only passes in a certain order is a test that will fail in parallel.

## What should I do when a test is hard to write?

Treat the difficulty as design feedback. A test that needs five mocks, reflection or a private method call is telling you the code has too many responsibilities or hides a dependency. Change the design: move the decision into a pure function, inject the collaborator, or split the class.

The refactor chapter in [Part 2](../testing/refactoring-checklist/refactoring-checklist.md) is the checklist for that change. Write the test that describes the behavior you want, then make the smallest design change that lets the test read clearly.

## How much coverage do I need?

Enough to make the behavior you care about provable. A line-coverage number can be gamed by tests that execute code without asserting anything. The tests in this book are chosen by behavior, not by a coverage threshold.

Use coverage to find untested branches, then decide whether each branch is a real requirement. A defensive `catch` for an impossible state may not need a test; an error path a user can hit does.

## When should I change the test instead of the code?

Change the test when the requirement changed. Do not change it because the implementation changed and the assertion is now inconvenient. A test that breaks on a rename but passes on a wrong total is testing the wrong thing.

The [refactoring checklist](../testing/refactoring-checklist/refactoring-checklist.md) makes the rule concrete: if a test must change during a refactor, stop. That is a behavior change and needs its own red-green cycle.

## How do I test the error path?

Write the test for the failure first, then make the production code produce the failure you specified. [Exceptions](../fundamentals/exceptions/exceptions.md) uses `Assert.ThrowsException` for a domain exception; the [validation chapter](../build-an-app/v6-validation/v6-validation.md) checks the `errors` map in a Problem Details response. Assert both the failure and the state it left behind.

## Should I test private methods?

No. A private method is an implementation detail. Test it through the public behavior that uses it. If the private method is too complex to test indirectly, extract it into a small public or internal type with a name and test that. The test should survive a change in how the private helper is organized.

## How do I test an HTTP endpoint?

Use `WebApplicationFactory<Program>` and a real `HttpClient` against the in-memory server. Send the request, assert the status and the body, and check the state on the next request. [Acceptance Tests](../testing/acceptance-tests/acceptance-tests.md) shows the first version; [Scaling Acceptance Tests](../testing/scaling-acceptance-tests/scaling-acceptance-tests.md) adds a driver and per-test isolation.

Do not mock `HttpContext` or the endpoint delegate. A test that builds the response by hand proves the mock, not the endpoint.

## How do I test EF Core and Dapper?

Use a real database engine. SQLite in memory or a temporary file is fast and deterministic; the repository tests in [v4](../build-an-app/v4-ef-core/v4-ef-core.md) and [v5](../build-an-app/v5-dapper/v5-dapper.md) use it. Test the migration against the same provider the app uses, and assert the rows, not the SQL text.

For a server database, use a container or Respawn to reset state between tests. The appendix on integration testing tools covers that trade-off.

