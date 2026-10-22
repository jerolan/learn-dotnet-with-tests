# v11: Messaging and Scheduling

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v11-messaging-scheduling)

Writing a task can trigger work that does not belong in the request: an audit entry, a notification, a cleanup. Version 11 publishes those events to an in-process queue, consumes them in a background service and adds a scheduled cleanup job.

The finished solution is in `build-an-app/v11-messaging-scheduling`.

## Write the test first

```csharp
[TestMethod]
public async Task CreateTask_PublishesAndConsumesAnEvent()
{
    using var api = new Api();
    var response = await api.Client.PostAsJsonAsync(
        "/tasks",
        new CreateTaskRequest("Write tests"));
    var task = await response.Content.ReadFromJsonAsync<TaskItem>();

    var audit = (InMemoryAuditLog)api.Services.GetRequiredService<IAuditLog>();
    await WaitForAsync(() => audit.Entries.Contains($"Created:{task!.Id}"));

    CollectionAssert.Contains(audit.Entries.ToList(), $"Created:{task.Id}");
}
```

The helper waits up to five seconds for the background consumer:

```csharp
private static async Task WaitForAsync(Func<bool> condition)
{
    var deadline = DateTime.UtcNow.AddSeconds(5);

    while (!condition())
    {
        if (DateTime.UtcNow > deadline)
        {
            Assert.Fail("Timed out waiting for the background consumer.");
        }

        await Task.Delay(10);
    }
}
```

Against v10 there is no queue, so the test times out.

## The queue

```csharp
public sealed record TaskEvent(int TaskId, string Title, string Type);

public sealed class TaskEventQueue
{
    private readonly Channel<TaskEvent> _channel = Channel.CreateUnbounded<TaskEvent>();

    public bool TryPublish(TaskEvent taskEvent)
    {
        return _channel.Writer.TryWrite(taskEvent);
    }

    public IAsyncEnumerable<TaskEvent> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
```

`Channel<T>` is the in-process queue. An unbounded channel never rejects a write; `TryWrite` returns `true`. A bounded channel would apply backpressure and return `false` when full.

## The publisher

```csharp
public sealed class PublishingTaskWriter : ITaskWriter
{
    private readonly ITaskWriter _inner;
    private readonly TaskEventQueue _queue;

    public TaskItem Add(string title)
    {
        var task = _inner.Add(title);
        _queue.TryPublish(new TaskEvent(task.Id, task.Title, "Created"));

        return task;
    }

    public TaskItem? MarkDone(int id)
    {
        var task = _inner.MarkDone(id);

        if (task is not null)
        {
            _queue.TryPublish(new TaskEvent(task.Id, task.Title, "Done"));
        }

        return task;
    }

    public int DeleteDone()
    {
        return _inner.DeleteDone();
    }
}
```

The publisher is the outermost writer decorator: the cache invalidator and EF run first, then the event is published. The endpoint does not know the queue exists.

## The consumer

```csharp
public sealed class TaskEventConsumer : BackgroundService
{
    private readonly TaskEventQueue _queue;
    private readonly TaskEventProcessor _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var taskEvent in _queue.ReadAllAsync(stoppingToken))
            {
                _processor.Process(taskEvent);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
```

Register the queue, the audit log, the processor and the hosted service:

```csharp
builder.Services.AddSingleton<TaskEventQueue>();
builder.Services.AddSingleton<IAuditLog, InMemoryAuditLog>();
builder.Services.AddSingleton<TaskEventProcessor>();
builder.Services.AddHostedService<TaskEventConsumer>();
```


`BackgroundService` starts when the host starts. `ReadAllAsync` waits for events and ends when the stopping token is canceled. The consumer is a singleton; the queue and processor must be safe for concurrent use.

## Repeat for new requirements

### A done event

```csharp
[TestMethod]
public async Task MarkDone_PublishesADoneEvent()
{
    using var api = new Api();
    var created = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
    var task = await created.Content.ReadFromJsonAsync<TaskItem>();

    await api.Client.PatchAsync($"/tasks/{task!.Id}/done", null);

    var audit = (InMemoryAuditLog)api.Services.GetRequiredService<IAuditLog>();
    await WaitForAsync(() => audit.Entries.Contains($"Done:{task.Id}"));
}
```

It passes with the same publisher; `MarkDone` adds a second event type.

### A scheduled cleanup

Add a delete operation to the writer:

```csharp
public int DeleteDone()
{
    var done = _db.Tasks.Where(task => task.IsDone).ToList();

    _db.Tasks.RemoveRange(done);
    _db.SaveChanges();

    return done.Count;
}
```

The job:

```csharp
public sealed class TaskCleanupJob
{
    public int Run()
    {
        var deleted = _writer.DeleteDone();
        _audit.Record($"Cleanup:{deleted}");

        return deleted;
    }
}
```

and the hosted service:

```csharp
public sealed class TaskCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<CleanupOptions> _options;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var job = scope.ServiceProvider.GetRequiredService<TaskCleanupJob>();
                job.Run();
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.Value.IntervalSeconds),
                stoppingToken);
        }
    }
}
```

The hosted service is a singleton; the job is scoped because it uses the scoped writer. `CreateScope()` gives the job a fresh context per run.

Test the job directly:

```csharp
[TestMethod]
public async Task CleanupJob_DeletesCompletedTasks()
{
    using var api = new Api();
    var first = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Done task"));
    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Keep task"));
    var doneTask = await first.Content.ReadFromJsonAsync<TaskItem>();

    await api.Client.PatchAsync($"/tasks/{doneTask!.Id}/done", null);

    using var scope = api.Services.CreateScope();
    var job = scope.ServiceProvider.GetRequiredService<TaskCleanupJob>();
    var deleted = job.Run();

    var tasks = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    Assert.AreEqual(1, deleted);
    Assert.AreEqual(1, tasks!.Count);
    Assert.AreEqual("Keep task", tasks[0].Title);
}
```

Green. The hosted service calls the same job once at startup and then every configured interval.

## Refactor

The in-process queue is a teaching stand-in for a broker:

- Messages live in memory and disappear on restart. A durable broker (SQS, RabbitMQ, Kafka) persists them and adds retries and dead-letter queues.
- Delivery is at least once in most brokers. Consumers must be idempotent; the audit example tolerates duplicates, a payment would not.
- A transactional outbox solves the "write committed, message lost" gap. The database write and the outbox row commit together, and a publisher drains the outbox.
- A background service can overlap with the next scheduled run if a run takes longer than the interval. Use a lock or a distributed lease when the job must not run twice.

Tests of background work should wait for an observable condition, not a fixed sleep. The audit log is the observable state here; the five-second deadline is only a failure bound.

## Wrapping up

What we have covered:

- `Channel<T>` as an in-process queue.
- A publisher decorator around the writer.
- A `BackgroundService` consumer with a cancellation token.
- A scoped cleanup job invoked from a singleton hosted service.
- A test that waits for an observable audit entry.
- The gaps between this design and a durable broker.

Next: v12: OpenAPI.

