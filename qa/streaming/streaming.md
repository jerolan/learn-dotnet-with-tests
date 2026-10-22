# Streaming with IAsyncEnumerable

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/streaming)

A method that returns `Task<List<TaskItem>>` has to read every row before the caller sees the first one. For a thousand tasks that is fine. For an export of a million, the memory grows with the result and the client waits for the end before it can start.

`IAsyncEnumerable<T>` returns items as they become available. This appendix writes a source that yields tasks, tests the full read, cancels halfway through, and checks what the caller keeps when the source throws after yielding.

## The source

`src/TaskStream/TaskSource.cs`:

```csharp
using System.Runtime.CompilerServices;

namespace TaskStream;

public sealed class TaskSource
{
    private readonly IReadOnlyList<TaskItem> _tasks;

    public TaskSource(IReadOnlyList<TaskItem> tasks)
    {
        _tasks = tasks;
    }

    public async IAsyncEnumerable<TaskItem> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var task in _tasks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            yield return task;
        }
    }
}
```

`yield return` suspends the method until the caller asks for the next item. `await Task.Yield()` stands in for real asynchronous work, such as a database read or an HTTP call. The cancellation check runs before each item, so a consumer that stops asking does not leave the source working through the rest.

`[EnumeratorCancellation]` lets the token travel with the enumerator when the caller uses `WithCancellation`. Without it, the token passed to the method and the token attached to the enumeration are separate.

## Write the test first

```csharp
[TestMethod]
public async Task ReadsEveryItemInOrder()
{
    var tasks = new[]
    {
        new TaskItem(Guid.NewGuid(), "First"),
        new TaskItem(Guid.NewGuid(), "Second"),
        new TaskItem(Guid.NewGuid(), "Third")
    };
    var source = new TaskSource(tasks);

    var seen = new List<TaskItem>();

    await foreach (var task in source.ReadAsync())
    {
        seen.Add(task);
    }

    CollectionAssert.AreEqual(tasks, seen);
}
```

`await foreach` is the consumer side of the same shape. The test drives the enumerator to the end and checks the order.

## Stop halfway

The second test cancels after the first item:

```csharp
using var cancellation = new CancellationTokenSource();
var seen = new List<TaskItem>();

await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
{
    await foreach (var task in source.ReadAsync(cancellation.Token))
    {
        seen.Add(task);
        cancellation.Cancel();
    }
});

Assert.AreEqual(1, seen.Count);
```

If the source ignores the token, the test says so:

```text
Assert.ThrowsException failed. No exception thrown. OperationCanceledException exception was expected.
```

The stream ended normally because nothing checked the token. Put `ThrowIfCancellationRequested` back inside the loop and the enumeration stops after the item that triggered the cancellation.

## What the caller keeps when the source fails

The third test uses a local iterator that yields one value and then throws:

```csharp
private static async IAsyncEnumerable<int> FailsAfterOne()
{
    yield return 1;
    await Task.Yield();
    throw new InvalidOperationException("The source failed.");
}
```

```csharp
var seen = new List<int>();

await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
{
    await foreach (var item in FailsAfterOne())
    {
        seen.Add(item);
    }
});

CollectionAssert.AreEqual(new[] { 1 }, seen);
```

Items already yielded were delivered; the exception arrives on the next move. That is different from a `Task<List<T>>` that fails before returning anything. A consumer of a stream has to handle a partial result.

```text
Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 36 ms
```

## Streaming over HTTP

A minimal API can return `IAsyncEnumerable<T>` directly:

```csharp
app.MapGet("/tasks/stream", (TaskSource source, CancellationToken cancellationToken) =>
    source.ReadAsync(cancellationToken));
```

`System.Text.Json` enumerates the sequence and writes a JSON array. Elements are written as they arrive, but the result is still one JSON document; a consumer that needs each message on its own line wants NDJSON or server-sent events.

For NDJSON, write one serialized object per line and flush the response after each:

```csharp
foreach (var task in source.ReadAsync(cancellationToken))
{
    await JsonSerializer.SerializeAsync(response.Body, task, cancellationToken);
    await response.WriteAsync("\n", cancellationToken);
    await response.Body.FlushAsync(cancellationToken);
}
```

Server-sent events add the `text/event-stream` content type and a `data:` prefix per message. Both formats push bytes before the source finishes; neither is a replacement for a durable queue when a disconnected client must not lose messages.

## Backpressure and lifetimes

The consumer pulls one item at a time. Awaiting the body of `await foreach` slows the source down; calling `ToListAsync` or `ToArray` first throws that away and buffers everything.

The enumerator lives for the length of the loop. Do not return an iterator that captures a scoped `DbContext` and expect it to work after the request scope ends. Materialize inside the scope or keep the stream inside it.

`IAsyncEnumerable` is also how EF Core exposes a query without loading it: `AsAsyncEnumerable`. Use it when the caller can process rows incrementally, not when the next step needs the whole set.

## What is deferred

There is no HTTP streaming test, no SSE endpoint and no measured backpressure here. A hands-on lab would expose NDJSON, read it with `HttpCompletionOption.ResponseHeadersRead`, assert that the first line arrives before the source completes, and disconnect halfway to prove the server stops.

## Wrapping up

What we have covered:

- An async iterator with `yield return` and `[EnumeratorCancellation]`.
- A full read, a cancellation stop, and a failure after a partial result.
- The JSON array, NDJSON and SSE shapes on the HTTP side.
- Backpressure, enumerator lifetime and EF Core's `AsAsyncEnumerable`.
- The HTTP and disconnection tests that a real streaming endpoint needs.

Next: cloud deployment.
