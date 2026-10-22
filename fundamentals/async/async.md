# Async/Await

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/async)

Given a weather source that returns a temperature asynchronously, `WeatherReport.DescribeAsync("Madrid")` should return `"Madrid: 20C"` without blocking while the source works. The finished solution is in `fundamentals/async`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Async`, an MSTest project in `tests/Async.Tests`, a solution and a project reference.

## How it works

An `async` method returns a `Task` or `Task<T>`. At `await`, it returns control to its caller while the awaited task is incomplete, then continues when that task completes. The caller can await the task without blocking a thread.

## How to test

Keep the weather report separate from the side effect that retrieves a temperature by giving it an `ITemperatureSource`. A fake source can return a completed task, so the test checks the formatted result without delay, network access or timing assumptions. The test itself returns `async Task` and awaits the report.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Async.Tests;

[TestClass]
public sealed class WeatherReportTests
{
    [TestMethod]
    public async Task DescribeAsync_ReturnsTheFormattedReport()
    {
        var source = new FakeSource((_, _) => Task.FromResult(20));
        var report = new WeatherReport(source);

        Assert.AreEqual("Madrid: 20C", await report.DescribeAsync("Madrid"));
    }
}
```

The fake returns a completed task. No delay, no network and no timing assumption.

## Try to run the test

```text
error CS0246: The type or namespace name 'WeatherReport' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace Async;

public interface ITemperatureSource
{
    Task<int> GetAsync(string city, CancellationToken cancellationToken);
}

public sealed class WeatherReport
{
    private readonly ITemperatureSource _source;

    public WeatherReport(ITemperatureSource source)
    {
        _source = source;
    }

    public Task<string> DescribeAsync(string city, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(string.Empty);
    }
}
```

The test compiles and fails with `Expected:<Madrid: 20C>. Actual:<>`.

## Write enough code to make it pass

```csharp
public async Task<string> DescribeAsync(string city, CancellationToken cancellationToken = default)
{
    var temperature = await _source.GetAsync(city, cancellationToken);

    return $"{city}: {temperature}C";
}
```

Green. The method is marked `async` and returns `Task<string>`. Inside, `await` unwraps the `Task<int>` from the source into an `int`. The caller receives a task that completes with the formatted string.

## Refactor

The method is already the shape you want: async all the way, one await, no blocking.

Some rules that prevent most async bugs:

- Do not call `.Result` or `.Wait()` on a task. Use `await`. Blocking can deadlock and wraps exceptions in `AggregateException`.
- Use `async Task` for asynchronous methods. `async void` is only for event handlers; its exceptions are not observable by the caller.
- Do not wrap naturally asynchronous I/O in `Task.Run`. The I/O API already returns a task. `Task.Run` is for CPU-bound work that should move off the current thread.
- Pass `CancellationToken` through the call chain. A token that stops at the public method is not useful to the database or HTTP call underneath.
- In library code, `ConfigureAwait(false)` avoids capturing the caller's synchronization context. In application code with a UI context, the default is usually what you want. The book uses the default for clarity.

## Repeat for new requirements

### Exceptions from the source

Write the test:

```csharp
[TestMethod]
public async Task DescribeAsync_PropagatesTheSourceException()
{
    var source = new FakeSource((_, _) =>
        Task.FromException<int>(new InvalidOperationException("offline")));
    var report = new WeatherReport(source);

    await Assert.ThrowsExceptionAsync<InvalidOperationException>(
        () => report.DescribeAsync("Madrid"));
}
```

It passes. `Task.FromException<int>` creates a faulted task. When the method awaits it, the `InvalidOperationException` is thrown inside the method and propagates to the caller.

This is the async contract: an exception in an awaited task surfaces at the `await`, not as an `AggregateException` from `.Result`. If the method does not catch it, the returned task becomes faulted with that exception.

### Cancellation

Write the test:

```csharp
[TestMethod]
public async Task DescribeAsync_CanceledToken_Throws()
{
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    var source = new FakeSource((_, token) =>
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(20);
    });
    var report = new WeatherReport(source);

    await Assert.ThrowsExceptionAsync<OperationCanceledException>(
        () => report.DescribeAsync("Madrid", cts.Token));
}
```

It passes. The method passes the cancellation token to the source; the source calls `ThrowIfCancellationRequested` and throws `OperationCanceledException`, which the method propagates.

Cancellation is cooperative. A token does not stop a thread; the code that receives it has to check it or pass it to an API that does. Pass the token down to the I/O call and let the cancellation reach the operation that can honor it.

## Wrapping up

What we have covered:

- `async` methods that return `Task` or `Task<T>`.
- `await` to unwrap a task without blocking the thread.
- `async Task` test methods.
- Exception propagation from an awaited task.
- `CancellationToken` and cooperative cancellation.
- The habits that prevent `.Result` deadlocks, `async void` surprises and unnecessary `Task.Run`.

The chapter follows the asynchronous I/O idea from the [Concurrency chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/concurrency): work runs independently and the caller waits for a result. .NET turns that into `Task` and `await`.

Next: [Files and I/O](../io/io.md).

