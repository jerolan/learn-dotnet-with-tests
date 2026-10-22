# v8: Lifetimes and a Decorator

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v8-di)

The API has more dependencies now: a context, a reader, a writer, a validator and the JWT services. Version 8 makes the lifetimes visible and adds a decorator that counts reads without changing the Dapper reader.

The finished solution is in `build-an-app/v8-di`.

## How it works

A scoped service is created once per HTTP request and disposed at the end. `TaskDbContext` and the repositories are scoped for one unit of work. A singleton is created once per app and shared by concurrent requests, so its mutable state must be thread-safe. A transient is created each time it is resolved.

The dangerous combination is a singleton that captures a scoped service. It keeps the scoped instance alive after the request ends, and a later request may use a disposed context. This is a captive dependency; scope validation can detect it, and ASP.NET Core enables it in Development by default.

## How to test

Call `/diagnostics` twice. The app id should stay the same while the request id changes. The test driver authenticates by default, so these requests exercise the protected route through `WebApplicationFactory`.

## Write the test first

```csharp
[TestMethod]
public async Task Diagnostics_UsesAScopedRequestIdAndASingletonAppId()
{
    using var api = new Api();

    var first = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");
    var second = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

    Assert.IsNotNull(first);
    Assert.IsNotNull(second);
    Assert.AreEqual(first.AppId, second.AppId);
    Assert.AreNotEqual(first.RequestId, second.RequestId);
}
```

## Try to run the test

Against v7, the route returns `404 Not Found`. The test describes the lifetime contract before the endpoint exists.

## Write the minimal amount of code for the test to run and check the failing test output

### Add the lifetime services

```csharp
public sealed class RequestContext
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class AppInstance
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class ReadCounter
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Increment()
    {
        Interlocked.Increment(ref _count);
    }
}
```

Register them:

```csharp
builder.Services.AddScoped<RequestContext>();
builder.Services.AddSingleton<AppInstance>();
builder.Services.AddSingleton<ReadCounter>();
```

The services now have the intended lifetimes, but the test still gets 404 because the diagnostics route is missing.

## Write enough code to make it pass

Expose the services through the route:

```csharp
app.MapGet("/diagnostics", (RequestContext request, AppInstance app, ReadCounter counter) =>
    new DiagnosticsResponse(app.Id, request.Id, counter.Count)).RequireAuthorization();
```

Green. Two requests share the singleton id and get different scoped ids.

`ReadCounter` uses `Interlocked.Increment` and `Volatile.Read` so concurrent requests can update and read its shared count safely.

## Refactor

The registrations are now the composition root of the application. Keep them together and make the lifetime choice explicit:

- Scoped for anything that owns a unit of work: `TaskDbContext`, the repositories, the validator.
- Singleton for stateless configuration and process-wide counters, with thread-safe state.
- Transient for cheap, stateless helpers that should not be shared.

The decorator is a small example of a cross-cutting concern. A logging, caching or metrics decorator can wrap the same interface without editing `DapperTaskReader`. The next chapters use the same shape for logging and caching.

Avoid the service locator pattern in handlers. Asking for `IServiceProvider` and resolving services by hand hides the dependencies and makes the tests less direct. The endpoint parameters are the dependency list.

## Repeat for new requirements

### Count reads with a decorator

Write the test:

```csharp
[TestMethod]
public async Task ReadCounter_IsIncrementedByTheDecorator()
{
    using var api = new Api();

    var before = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");
    await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");
    var after = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

    Assert.IsNotNull(before);
    Assert.IsNotNull(after);
    Assert.AreEqual(before.ReadCount + 1, after.ReadCount);
}
```

Before the decorator, the read count stays at zero, so this test fails after the `/tasks` request.

Add the decorator:

```csharp
public sealed class CountingTaskReader : ITaskReader
{
    private readonly ITaskReader _inner;
    private readonly ReadCounter _counter;

    public CountingTaskReader(ITaskReader inner, ReadCounter counter)
    {
        _inner = inner;
        _counter = counter;
    }

    public IReadOnlyList<TaskItem> All()
    {
        _counter.Increment();
        return _inner.All();
    }

    public TaskItem? Find(int id)
    {
        _counter.Increment();
        return _inner.Find(id);
    }
}
```

Register it with a factory so the inner reader is explicit:

```csharp
builder.Services.AddScoped<DapperTaskReader>();
builder.Services.AddScoped<ITaskReader>(services =>
    new CountingTaskReader(
        services.GetRequiredService<DapperTaskReader>(),
        services.GetRequiredService<ReadCounter>()));
```

Green. The endpoints still ask for `ITaskReader`; they do not know a counter wraps it.

A plain `AddScoped<ITaskReader, CountingTaskReader>()` would not work here: the decorator needs another `ITaskReader` to wrap, and the container would resolve the same registration recursively. The factory breaks that cycle by naming the inner concrete type.

## Wrapping up

What we have covered:

- Scoped, singleton and transient lifetimes in one API.
- A scoped request id and a singleton app id exposed by diagnostics.
- A thread-safe singleton counter.
- A decorator that wraps `ITaskReader` and counts reads.
- Factory registration to avoid a recursive decorator resolution.
- Captive dependencies and the composition root.

Next: [v9: structured logging](../v9-logging/v9-logging.md).

