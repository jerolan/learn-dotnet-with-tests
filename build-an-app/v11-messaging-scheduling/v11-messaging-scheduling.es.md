# v11: Mensajería y scheduling

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v11-messaging-scheduling)

Escribir una tarea puede disparar trabajo que no pertenece a la petición: una entrada de auditoría, una notificación, una limpieza. La versión 11 publica esos eventos en una cola en proceso, los consume un servicio en segundo plano y agrega un job de limpieza programado.

La solución terminada está en `build-an-app/v11-messaging-scheduling`.

## Escribe primero el test

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

El ayudante espera hasta cinco segundos al consumidor:

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

Contra v10 no hay cola, así que el test se queda esperando.

## La cola

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

`Channel<T>` es la cola en proceso. Un canal sin límite nunca rechaza una escritura; `TryWrite` devuelve `true`. Un canal con límite aplicaría contrapresión y devolvería `false` cuando esté lleno.

## El publicador

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

El publicador es el decorador de escritura más externo: primero corren el invalidado de caché y EF, después se publica el evento. El endpoint no sabe que la cola existe.

## El consumidor

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

Registra la cola, el log de auditoría, el procesador y el servicio:

```csharp
builder.Services.AddSingleton<TaskEventQueue>();
builder.Services.AddSingleton<IAuditLog, InMemoryAuditLog>();
builder.Services.AddSingleton<TaskEventProcessor>();
builder.Services.AddHostedService<TaskEventConsumer>();
```

`BackgroundService` arranca cuando arranca el host. `ReadAllAsync` espera eventos y termina cuando se cancela el token de parada. El consumidor es singleton; la cola y el procesador deben ser seguros para uso concurrente.

## Repite con requisitos nuevos

### Un evento al marcar hecha

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

Pasa con el mismo publicador; `MarkDone` agrega un segundo tipo de evento.

### Una limpieza programada

Agrega la operación de borrado al escritor:

```csharp
public int DeleteDone()
{
    var done = _db.Tasks.Where(task => task.IsDone).ToList();

    _db.Tasks.RemoveRange(done);
    _db.SaveChanges();

    return done.Count;
}
```

El job:

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

y el servicio:

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

El servicio es singleton; el job es scoped porque usa el escritor scoped. `CreateScope()` le da al job un contexto nuevo en cada corrida.

Prueba el job directamente:

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

Verde. El servicio programa el mismo job una vez al arrancar y después en cada intervalo configurado.

## Refactor

La cola en proceso es un sustituto didáctico de un broker:

- Los mensajes viven en memoria y desaparecen al reiniciar. Un broker durable (SQS, RabbitMQ, Kafka) los persiste y agrega reintentos y dead-letter queues.
- La entrega suele ser al menos una vez. Los consumidores deben ser idempotentes; la auditoría tolera duplicados, un pago no.
- Un outbox transaccional resuelve el hueco entre "la escritura se confirmó" y "el mensaje se perdió". La escritura y la fila del outbox se confirman juntas, y un publicador drena el outbox.
- Un servicio en segundo plano puede solaparse con la siguiente corrida programada si una tarda más que el intervalo. Usa un lock o un lease distribuido cuando el job no deba correr dos veces.

Los tests de trabajo en segundo plano deben esperar una condición observable, no un sleep fijo. El log de auditoría es el estado observable aquí; el plazo de cinco segundos es solo el límite de fallo.

## Para cerrar

Lo que cubrimos:

- `Channel<T>` como cola en proceso.
- Un decorador publicador alrededor del escritor.
- Un consumidor `BackgroundService` con token de cancelación.
- Un job de limpieza scoped invocado desde un servicio singleton.
- Un test que espera una entrada observable de auditoría.
- Las diferencias entre este diseño y un broker durable.

Siguiente: [v12: OpenAPI](../v12-openapi/v12-openapi.es.md).

