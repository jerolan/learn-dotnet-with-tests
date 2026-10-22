# v10: Caching with IMemoryCache

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v10-caching)

The list endpoint reads the same rows on every request. Version 10 caches the list and each task in `IMemoryCache`, and the write side invalidates the entries it changes. The read counter from v8 makes the cache visible in a test.

The finished solution is in `build-an-app/v10-caching`.

## How it works

`IMemoryCache` stores the list under `tasks:all` and individual rows under `tasks:{id}` for a configured TTL. The caching reader wraps the read counter, so a cache hit returns before the counter runs. The write decorator removes the list and task keys affected by a change.

## How to test

Read `/diagnostics` before and after two list requests. The read counter shows whether the second request reached Dapper, while HTTP requests keep the test on the public API boundary.

## Write the test first

```csharp
[TestMethod]
public async Task RepeatedListReads_HitTheCache()
{
    using var api = new Api();
    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));

    var before = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

    await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");
    await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    var after = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

    Assert.AreEqual(before!.ReadCount + 1, after!.ReadCount);
}
```

In v9 the counter increments twice, once per read. This test describes the cache contract.

## Try to run the test

Run it against v9. The counter increments twice, so the test fails with `Expected:<1>. Actual:<2>`.

## Write the minimal amount of code for the test to run and check the failing test output

### Add the cache

Register it and bind the TTL:

```csharp
builder.Services.AddMemoryCache();
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));
```

```csharp
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int Seconds { get; set; } = 30;
}
```

Registering the cache does not change the reader yet; both requests still reach the counter.

## Write enough code to make it pass

### The read decorator

```csharp
public sealed class CachingTaskReader : ITaskReader
{
    private const string AllKey = "tasks:all";

    private readonly ITaskReader _inner;
    private readonly IMemoryCache _cache;
    private readonly IOptions<CacheOptions> _options;

    public CachingTaskReader(
        ITaskReader inner,
        IMemoryCache cache,
        IOptions<CacheOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public IReadOnlyList<TaskItem> All()
    {
        return _cache.GetOrCreate(AllKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow =
                TimeSpan.FromSeconds(_options.Value.Seconds);

            return _inner.All();
        })!;
    }

    public TaskItem? Find(int id)
    {
        var key = $"tasks:{id}";

        if (_cache.TryGetValue(key, out TaskItem? cached))
        {
            return cached;
        }

        var task = _inner.Find(id);

        if (task is not null)
        {
            _cache.Set(key, task, TimeSpan.FromSeconds(_options.Value.Seconds));
        }

        return task;
    }
}
```

`GetOrCreate` checks the key, runs the factory on a miss and stores the result with the TTL. `Find` caches only found tasks; a missing id is cheap to re-check and caching a negative result would need its own invalidation.

The cache sits outside the counter:

```csharp
builder.Services.AddScoped<CountingTaskReader>(services =>
    new CountingTaskReader(
        services.GetRequiredService<DapperTaskReader>(),
        services.GetRequiredService<ReadCounter>()));

builder.Services.AddScoped<ITaskReader>(services =>
    new CachingTaskReader(
        services.GetRequiredService<CountingTaskReader>(),
        services.GetRequiredService<IMemoryCache>(),
        services.GetRequiredService<IOptions<CacheOptions>>()));
```

A cache hit returns before the counter sees a call, which is why the test expects one increment for two reads.

The row cache uses `Find` with the same configured TTL; the list and item keys can now be invalidated by the writer.

## Refactor

Caching is a correctness decision as much as a performance one:

- Every write must invalidate every key it can affect. A missing key here means a stale response.
- The TTL is the backstop for writes that bypass the API, such as the raw SQL in the v5 test. Those writes do not call `InvalidatingTaskWriter`; only the TTL or a manual eviction refreshes the cache.
- `IMemoryCache` is per process. Two instances of the API do not share it, and a restart starts empty. A distributed cache (Redis, for example) needs a different adapter and its own invalidation story.
- Do not cache scoped objects such as the `DbContext`. Cache plain values or DTOs with a known lifetime.
- Keep keys in one place. `tasks:all` and `tasks:{id}` are strings today; a small key helper prevents typos as the number of keys grows.

The next chapters reuse the decorator shape: a queue consumer, a scheduled job and a resilience policy can all wrap an interface without editing the core implementation.

## Repeat for new requirements

### A write clears the list

```csharp
[TestMethod]
public async Task Write_InvalidatesTheListCache()
{
    using var api = new Api();
    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("First"));

    var first = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");
    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Second"));
    var second = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    Assert.AreEqual(1, first!.Count);
    Assert.AreEqual(2, second!.Count);
}
```

Without invalidation, the second read returns the cached one-item list, so the test fails with `Expected:<2>. Actual:<1>`.

Add a writer decorator that clears the list after creation:

```csharp
public sealed class InvalidatingTaskWriter : ITaskWriter
{
    private const string AllKey = "tasks:all";

    private readonly ITaskWriter _inner;
    private readonly IMemoryCache _cache;

    public InvalidatingTaskWriter(ITaskWriter inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public TaskItem Add(string title)
    {
        var task = _inner.Add(title);
        _cache.Remove(AllKey);

        return task;
    }

    public TaskItem? MarkDone(int id) => _inner.MarkDone(id);
}
```

Register it around `EfTaskWriter`:

```csharp
builder.Services.AddScoped<EfTaskWriter>();
builder.Services.AddScoped<ITaskWriter>(services =>
    new InvalidatingTaskWriter(
        services.GetRequiredService<EfTaskWriter>(),
        services.GetRequiredService<IMemoryCache>()));
```

Green. The second list read now sees the new task.

### Marking done clears the list and the task

```csharp
[TestMethod]
public async Task MarkDone_InvalidatesBothCachedEntries()
{
    using var api = new Api();
    var created = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
    var task = await created.Content.ReadFromJsonAsync<TaskItem>();

    await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{task!.Id}");
    await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    await api.Client.PatchAsync($"/tasks/{task.Id}/done", null);

    var fetched = await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{task.Id}");
    var list = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    Assert.IsTrue(fetched!.IsDone);
    Assert.IsTrue(list!.Single(item => item.Id == task.Id).IsDone);
}
```

At this point `MarkDone` leaves the per-task cache intact, so the test returns the old `IsDone` value.

Invalidate both affected keys after the write:

```csharp
public TaskItem? MarkDone(int id)
{
    var task = _inner.MarkDone(id);
    _cache.Remove(AllKey);
    _cache.Remove($"tasks:{id}");

    return task;
}
```

Green. The writer knows the id, so it can remove the per-task key and the list key in one place.

## Wrapping up

What we have covered:

- `IMemoryCache` and a TTL from configuration.
- A read decorator for the list and per-id entries.
- A write decorator that invalidates the affected keys.
- Decorator order: the cache sits outside the read counter.
- Stale data risks, external writers, per-process scope and key discipline.

Next: [v11: messaging and scheduling](../v11-messaging-scheduling/v11-messaging-scheduling.md).

