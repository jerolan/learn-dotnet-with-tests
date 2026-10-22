# The using Keyword

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/using)

Some objects own a resource that must be released: a file handle, a socket, a database connection. The `using` statement calls `Dispose()` when the scope ends, whether the code finished normally or threw. That guarantee is why you rarely call `Dispose()` by hand.

We will build a small session that records when it opens and closes. The finished solution is in `fundamentals/using`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/UsingKeyword`, an MSTest project in `tests/UsingKeyword.Tests`, a solution and a project reference.

## How it works

`IDisposable` exposes `Dispose()` for releasing an object's resources. A `using` scope becomes a `try`/`finally`, so cleanup runs on normal exit and exceptions. `using var` lasts to the end of its enclosing block; the parenthesized form lets you choose a smaller scope. Garbage collection manages memory, while `Dispose()` releases resources such as file handles and sockets.

```csharp
using (var session = new Session(log))
{
    // session is disposed here
}

using var other = new Session(log);
// other is disposed at the end of the enclosing block
```

## How to test

Give a disposable object a log that records opening and disposal. Assert both that the object is disposed and that cleanup happened at the right point. A thrown exception and repeated disposal are separate lifecycle cases.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UsingKeyword.Tests;

[TestClass]
public sealed class SessionTests
{
    [TestMethod]
    public void Run_DisposesTheSession()
    {
        var log = new List<string>();
        Session? captured = null;

        SessionRunner.Run(log, session => captured = session);

        Assert.IsTrue(captured!.IsDisposed);
        CollectionAssert.AreEqual(new[] { "open", "dispose" }, log);
    }
}
```

## Try to run the test

```text
error CS0246: The type or namespace name 'Session' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace UsingKeyword;

public sealed class Session : IDisposable
{
    private readonly List<string> _log;

    public Session(List<string> log)
    {
        _log = log;
        _log.Add("open");
    }

    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
        _log.Add("dispose");
    }
}

public static class SessionRunner
{
    public static void Run(List<string> log, Action<Session> action)
    {
        var session = new Session(log);
        action(session);
    }
}
```

The test compiles and fails: the session is never disposed, so `IsDisposed` is false and the log only has `open`.

## Write enough code to make it pass

```csharp
public static void Run(List<string> log, Action<Session> action)
{
    using var session = new Session(log);
    action(session);
}
```

Green. At the end of `Run`, the compiler inserts a `finally` block that calls `session.Dispose()`. The method body can throw, return or fall through; the finally block still runs.

## Refactor

The `using var` declaration makes the session lifetime explicit, and the test covers the caller-visible behavior. Run the test again; no extra cleanup helper is needed.

## Repeat for new requirements

### Disposal on an exception

Write the test:

```csharp
[TestMethod]
public void Run_DisposesWhenTheActionThrows()
{
    var log = new List<string>();

    Assert.ThrowsException<InvalidOperationException>(() =>
        SessionRunner.Run(log, _ => throw new InvalidOperationException()));

    CollectionAssert.AreEqual(new[] { "open", "dispose" }, log);
}
```

It passes. The exception leaves `Run`, the finally block runs, and the caller still sees the original exception. `using` cleans up; it does not swallow errors.

### The block form

Write the test:

```csharp
[TestMethod]
public void UsingStatement_DisposesAtTheEndOfTheBlock()
{
    var log = new List<string>();
    Session? captured;

    using (var session = new Session(log))
    {
        captured = session;
        Assert.IsFalse(session.IsDisposed);
    }

    Assert.IsTrue(captured!.IsDisposed);
}
```

It passes. The session is alive inside the block and disposed immediately after the closing brace.

### Dispose twice

Write the test:

```csharp
[TestMethod]
public void Dispose_IsIdempotent()
{
    var log = new List<string>();
    var session = new Session(log);

    session.Dispose();
    session.Dispose();

    CollectionAssert.AreEqual(new[] { "open", "dispose" }, log);
}
```

It fails because `Dispose` logs twice. Make it idempotent:

```csharp
public void Dispose()
{
    if (IsDisposed)
    {
        return;
    }

    IsDisposed = true;
    _log.Add("dispose");
}
```

Green. A second `Dispose()` call is harmless. The convention is that disposal can happen more than once, especially when a `using` block and a finalizer or another owner both release the resource.

### Keep resource ownership clear

A few rules keep ownership clear:

- Do not call `Dispose()` on an object that a `using` statement owns. Pick one owner.
- Do not dispose an object that you return to a caller. Ownership transfers with the return value; the caller decides when to release it.
- Prefer `using` over a manual `try`/`finally`. The compiler's version is harder to get wrong.
- For asynchronous cleanup, implement `IAsyncDisposable` and use `await using`. Streams and database connections in modern .NET often support it.
- Keep the `using` scope as small as the resource's lifetime needs. A declaration at the top of a long method holds the file open longer than necessary.

`using` is also used for a namespace import (`using System.Text;`). The two meanings are different: the statement disposes, the directive imports. The compiler tells them apart by position.

## Wrapping up

What we have covered:

- `IDisposable` and `Dispose()`.
- `using` as a `try`/`finally` that always disposes.
- `using var` declarations and the parenthesized statement.
- Disposal on the exception path.
- Idempotent disposal.
- Managed memory versus unmanaged resources, and `IAsyncDisposable` for async cleanup.

The chapter follows the resource discipline of the [I/O chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/io): whoever opens a resource is responsible for closing it promptly. .NET makes that ownership explicit with `IDisposable` and `using`.

That completes Part 1. Part 2 covers testing fundamentals.

