# Design Questions

The code chapters show one solution each. These questions are about choosing between solutions when more than one is defensible.

## Where does a business rule belong?

Put the rule where it cannot be bypassed. A title length that every caller must respect belongs in the request validator at the boundary and, if the domain has other callers, in the domain type as well. A rule that only protects an internal invariant belongs in the type that owns the state.

[Classes and Objects](../fundamentals/classes/classes.md) keeps the overdraft rule inside the account. [Validation](../build-an-app/v6-validation/v6-validation.md) keeps the request rules in FluentValidation. The duplication is acceptable when the two rules protect different things.

## When should I introduce an interface?

Introduce one at a boundary you need to replace or observe: a database, an HTTP dependency, a clock, a queue. Do not create an interface for every class. A single implementation with no test seam adds ceremony and hides the concrete behavior.

[Dependency Injection](../fundamentals/dependency-injection/dependency-injection.md) introduces `IMessageSender` because the test needs a sender it can inspect. [Working Without Mocks](../testing/working-without-mocks/working-without-mocks.md) shows the fake on the other side of that interface.

## Is CQRS worth it?

CQRS is worth it when reads and writes have genuinely different shapes, scaling needs or models. It is not worth it just because the acronym sounds architectural. [v14](../build-an-app/v14-cqrs/v14-cqrs.md) uses command and query handlers over one database; that is a use-case separation, not full CQRS.

Full CQRS adds a second model, eventual consistency and a synchronization story. Adopt it when the read side has outgrown the write model, not before.

## Should a repository return IQueryable?

Return `IQueryable<T>` only when composition is the contract: the caller is expected to add filters and projections. Otherwise return a materialized list or a DTO. An `IQueryable` leaks the provider and lets a caller build a query you did not design.

[IQueryable](../fundamentals/queriable/queriable.md) shows the expression tree and the provider. [Dapper](../build-an-app/v5-dapper/v5-dapper.md) shows the opposite choice: SQL stays inside the reader and the caller gets values.

## How many layers should the application have?

Enough to keep the dependencies pointing in one direction and no more. A controller, a use-case handler and an adapter are often enough. A repository interface that only forwards to `DbContext` and a service that only forwards to the repository add indirection without a decision.

The test is whether a layer owns a concept. If deleting it would merge two unrelated responsibilities, keep it. If it only renames a method, remove it.

## When should I use MediatR?

Use it when you want a uniform dispatch point for use cases or a pipeline for cross-cutting behavior. Do not use it to turn every method into a message. [v14](../build-an-app/v14-cqrs/v14-cqrs.md) sends commands and queries because the handlers make the use cases explicit.

If a controller calls one service and the service is already clear, the mediator adds a hop. The value appears when many use cases share validation, logging or transaction behavior.

## How do I choose a DI lifetime?

Scoped for a unit of work: a `DbContext`, a repository, a request context. Singleton for process-wide, thread-safe state: configuration, a counter, a cache. Transient for cheap stateless helpers.

The dangerous case is a singleton that captures a scoped dependency. [Lifetimes and a Decorator](../build-an-app/v8-di/v8-di.md) makes the lifetimes visible and explains the captive dependency. When in doubt, scoped is the safer default for anything that touches request state.

## Should validation live in the domain or the request?

Both, at different levels. The request validator rejects malformed input with field errors. The domain type protects invariants that must hold no matter who calls it. A domain method cannot assume the HTTP layer validated anything.

[Exceptions](../fundamentals/exceptions/exceptions.md) puts the age range in the domain parser. [Validation](../build-an-app/v6-validation/v6-validation.md) puts the request shape in FluentValidation.

## When does caching pay for itself?

When reads dominate writes, the read is expensive and stale data is acceptable for a known window. The invalidation story is part of the decision: every write must clear every key it can affect.

[v10](../build-an-app/v10-caching/v10-caching.md) shows the read and write decorators. If the invalidation rules are not obvious, the cache is probably too early.

## Where does logging belong?

At the boundaries: requests, dependency calls, background jobs and decisions worth auditing. The domain can accept an abstraction when a business event matters, but it should not know about providers, sinks or formatted strings.

[v9](../build-an-app/v9-logging/v9-logging.md) logs a structured event in the endpoint and a correlation scope in middleware. The domain code in that version has no logger.

## When is a refactor done?

When the tests are green, the names say what the code does, and the next change is easier to make. "Easier" is a judgment, not a metric. If a proposed abstraction has only one implementation and no concept behind it, the refactor is not done; it is postponed.

The [refactoring checklist](../testing/refactoring-checklist/refactoring-checklist.md) is the short version of the process.

## How do I keep an application testable?

Inject what varies, keep decisions in pure functions where possible, and put I/O behind a boundary. A class that creates its own `HttpClient` or reads a static clock is telling the test to use reflection. A constructor that takes an interface lets the test supply a fake.

The test drivers in [Part 3](../build-an-app/v1-intro/v1-intro.md) are the result: every dependency has a seam and every test can choose its own implementation.

