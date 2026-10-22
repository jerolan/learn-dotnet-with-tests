# .NET Questions

These are language and framework choices that come up while writing the chapters. Each answer names the trade-off and the chapter where the choice is exercised.

## Record, class or struct?

Use a class when the type has identity and mutable state, such as a service or a session. Use a record when the value is immutable data and two instances with the same fields should compare equal. Use a struct for a small value that behaves like a number and is cheap to copy.

[Classes and Objects](../fundamentals/classes/classes.md) models a bank account with a class. [Structs and Records](../fundamentals/records/records.md) compares the copy of a struct with the value equality of a record.

## When is a record struct the right choice?

When you want the value-type copy of a struct and the generated equality and printing of a record. It fits small coordinates, money-like values and identifiers. It does not fit a large object or a type with mutable reference fields.

## IEnumerable or IQueryable?

Use `IEnumerable<T>` when the sequence is in memory or the provider is LINQ to Objects. Use `IQueryable<T>` when a provider such as EF Core should translate the query. Do not expose `IQueryable` from a repository unless the caller is meant to compose queries.

[IEnumerable](../fundamentals/enumerable/enumerable.md) shows lazy sequences; [IQueryable](../fundamentals/queriable/queriable.md) shows the expression tree and the provider.

## Task or ValueTask?

Use `Task` by default. `ValueTask` avoids an allocation when the operation often completes synchronously, but it has stricter rules: await it once, do not store it, do not use it concurrently. Most application code should return `Task` and leave `ValueTask` to hot library paths.

[Tasks](../fundamentals/tasks/tasks.md) and [Async/Await](../fundamentals/async/async.md) use `Task` throughout.

## Why is async void dangerous?

An `async void` method cannot be awaited, and an exception thrown inside it has no task for the caller to observe. It is only for event handlers. Asynchronous methods should return `Task` or `Task<T>`.

## Exceptions or a Result type?

Use exceptions for contract violations and unexpected failures. Use a try-pattern or a result value when invalid input is an expected outcome the caller must handle. [Exceptions](../fundamentals/exceptions/exceptions.md) uses a domain exception; the [Tasks](../fundamentals/tasks/tasks.md) chapter returns values from normal operations.

A `Result<T>` type is a design choice, not a language requirement. If the codebase uses one, keep the boundary consistent; do not mix three styles in the same layer.

## decimal or double?

Use `decimal` for money, prices and any value that must match base-10 arithmetic. Use `double` for measurements, geometry and scientific computation where binary floating point is acceptable. [Decimals](../fundamentals/decimal/decimal.md) shows the difference with `0.1 + 0.2`.

## string or StringBuilder?

Use `string` for a fixed number of concatenations and for values that fit in one expression. Use `StringBuilder` in a loop that appends many pieces, where repeated string allocation would be wasteful. [Control Flow](../fundamentals/control-flow/control-flow.md) uses a builder in the repeat loop.

## Array, List, IReadOnlyList?

An array has a fixed length and is the simplest storage for a known count. A `List<T>` grows and is the default working collection. Return `IReadOnlyList<T>` or `IReadOnlyCollection<T>` from a public API when callers should read but not mutate.

[Collections](../fundamentals/collections/collections.md) uses all three and explains the choice.

## When do I use Span or Memory?

Use `Span<T>` for a synchronous, stack-only view over a contiguous buffer, such as parsing a slice without allocating. Use `Memory<T>` when the view must be stored in a field or used across an await. They are performance tools; do not introduce them into application code without a measured reason.

## ref, in or out?

`ref` when the method must mutate the caller's variable. `out` for the try-pattern, where the method returns success and a value. `in` for a large readonly struct you do not want to copy. For ordinary values, return a value. [ref](../fundamentals/ref/ref.md) covers all three.

## When should a class be sealed?

Seal a class when inheritance is not part of its design and you want the compiler to prevent accidental derivation. It also documents the intent. Do not seal a type that is a deliberate extension point; do consider sealing concrete implementations that exist to satisfy an interface.

## DateOnly and TimeOnly?

Use `DateOnly` for a calendar date with no time or zone, such as a birthday or a due date. Use `TimeOnly` for a time of day without a date. Use `DateTimeOffset` when the instant and its offset matter. Do not model a calendar date as a `DateTime` at midnight in the server's zone.

## with or mutation?

Use `with` on a record to create a changed copy and keep the original intact. Use a mutable class when the object's identity persists through changes and other code holds references to it. [The with keyword](../fundamentals/with/with.md) shows non-destructive updates and the shallow-copy caveat.

## Should I use var everywhere?

Use `var` when the right-hand side makes the type obvious, such as `var total = price * rate`. Write the type when the reader would have to guess, especially for a method result whose name does not announce its type. [Declarations](../fundamentals/declarations/declarations.md) makes the rule concrete.

