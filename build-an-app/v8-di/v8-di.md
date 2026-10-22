# v8: Lifetimes and a Decorator

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v8-di)

The API has more dependencies now: a context, a reader, a writer, a validator and the JWT services. Version 8 makes the lifetimes visible and adds a decorator that counts reads without changing the Dapper reader.

The finished solution is in `build-an-app/v8-di`.

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

Against v7, the route returns 404. The test describes the lifetime contract before the endpoint exists.

## Add the lifetime services

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

and expose them:

```csharp
app.MapGet("/diagnostics", (RequestContext request, AppInstance app, ReadCounter counter) =>
    new DiagnosticsResponse(app.Id, request.Id, counter.Count)).RequireAuthorization();
```

Green. Two requests share the singleton id and get different scoped ids.

## How it works

A scoped service is created once per HTTP request. The same instance is reused everywhere in that request and disposed at the end. `TaskDbContext` and the repositories are scoped for that reason: one unit of work, one context.

A singleton is created once per application. `AppInstance` and `ReadCounter` are singletons; every request sees the same instance. A singleton must be thread-safe, because concurrent requests use it at the same time. `ReadCounter` uses `Interlocked.Increment` and `Volatile.Read` for that reason.

A transient is created every time it is resolved. It is useful for cheap, stateless services, but resolving one many times inside a request can create more objects than expected.

The dangerous combination is a singleton that captures a scoped service. The singleton keeps the scoped instance alive after the request ends, and a later request uses a disposed context. That is a captive dependency. The container can validate this if you enable scope validation; ASP.NET Core does it in Development by default.

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

## Refactor

The registrations are now the composition root of the application. Keep them together and make the lifetime choice explicit:

- Scoped for anything that owns a unit of work: `TaskDbContext`, the repositories, the validator.
- Singleton for stateless configuration and process-wide counters, with thread-safe state.
- Transient for cheap, stateless helpers that should not be shared.

The decorator is a small example of a cross-cutting concern. A logging, caching or metrics decorator can wrap the same interface without editing `DapperTaskReader`. The next chapters use the same shape for logging and caching.

Avoid the service locator pattern in handlers. Asking for `IServiceProvider` and resolving services by hand hides the dependencies and makes the tests less direct. The endpoint parameters are the dependency list.

## Wrapping up

What we have covered:

- Scoped, singleton and transient lifetimes in one API.
- A scoped request id and a singleton app id exposed by diagnostics.
- A thread-safe singleton counter.
- A decorator that wraps `ITaskReader` and counts reads.
- Factory registration to avoid a recursive decorator resolution.
- Captive dependencies and the composition root.

Next: v9: structured logging.

