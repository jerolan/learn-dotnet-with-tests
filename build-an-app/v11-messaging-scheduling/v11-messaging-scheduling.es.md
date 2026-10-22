# v11: Mensajería y scheduling

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v11-messaging-scheduling)

Escribir una tarea puede disparar trabajo que no pertenece a la petición: una entrada de auditoría, una notificación, una limpieza. La versión 11 publica esos eventos en una cola en proceso, los consume un servicio en segundo plano y agrega un job de limpieza programado.

La solución terminada está en `build-an-app/v11-messaging-scheduling`.

## Cómo funciona

Un decorador del escritor publica un evento en un `Channel<TaskEvent>` en proceso. Un `BackgroundService` singleton lee el canal y envía cada evento a un procesador de auditoría. El servicio de limpieza también es un singleton hospedado, pero crea un scope en cada corrida porque el job usa el escritor scoped.

El canal vive intencionalmente en memoria: un reinicio pierde los mensajes. El job de limpieza se puede ejecutar directamente en un test; el servicio hospedado lo ejecuta al arrancar y después en el intervalo configurado.

## Cómo probarlo

Prueba el recorrido del evento por HTTP: crea una tarea y espera a que el log de auditoría contenga el evento. La espera tiene un plazo para que la ausencia del evento falle con claridad, sin depender de un sleep fijo.

Prueba la limpieza por separado: resuelve `TaskCleanupJob` desde un scope y llama a `Run()`. Así verificas enseguida el resultado del borrado, sin esperar al planificador.

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

## Intenta ejecutar el test

El test todavía no compila porque aún no existen `TaskEventQueue`, `IAuditLog` ni `InMemoryAuditLog`.

## Escribe el código mínimo para que el test compile y mira la salida

Agrega la cola de eventos y un log de auditoría pequeño y seguro para concurrencia. El test ya compila, pero todavía se agota el tiempo porque ningún escritor publica un evento.

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

public interface IAuditLog
{
    void Record(string entry);
}

public sealed class InMemoryAuditLog : IAuditLog
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyCollection<string> Entries => _entries.ToArray();

    public void Record(string entry)
    {
        _entries.Enqueue(entry);
    }
}
```

Register those services:

```csharp
builder.Services.AddSingleton<TaskEventQueue>();
builder.Services.AddSingleton<IAuditLog, InMemoryAuditLog>();
```

Con la cola y el log registrados, el test se ejecuta, pero sigue agotando el tiempo porque todavía no hay un publicador ni un consumidor. El fallo es `Timed out waiting for the background consumer.`

`Channel<T>` es la cola en proceso. Un canal sin límite nunca rechaza una escritura; `TryWrite` devuelve `true`. Un canal con límite aplicaría contrapresión y devolvería `false` cuando esté lleno.

## Escribe el código suficiente para que pase

Implementa el decorador del escritor para publicar un evento `Created` después de que el escritor interno guarde la tarea:

```csharp
public sealed class PublishingTaskWriter : ITaskWriter
{
    private readonly ITaskWriter _inner;
    private readonly TaskEventQueue _queue;

    public PublishingTaskWriter(ITaskWriter inner, TaskEventQueue queue)
    {
        _inner = inner;
        _queue = queue;
    }

    public TaskItem Add(string title)
    {
        var task = _inner.Add(title);
        _queue.TryPublish(new TaskEvent(task.Id, task.Title, "Created"));

        return task;
    }

    public TaskItem? MarkDone(int id) => _inner.MarkDone(id);
}
```

El publicador es el decorador de escritura más externo: primero corren el invalidado de caché y el escritor EF, después publica el evento. El endpoint no conoce la cola.

Un procesador registra el evento en el log de auditoría y un consumidor hospedado vacía la cola:

```csharp
public sealed class TaskEventProcessor
{
    private readonly IAuditLog _audit;

    public TaskEventProcessor(IAuditLog audit)
    {
        _audit = audit;
    }

    public void Process(TaskEvent taskEvent)
    {
        _audit.Record($"{taskEvent.Type}:{taskEvent.TaskId}");
    }
}

public sealed class TaskEventConsumer : BackgroundService
{
    private readonly TaskEventQueue _queue;
    private readonly TaskEventProcessor _processor;

    public TaskEventConsumer(TaskEventQueue queue, TaskEventProcessor processor)
    {
        _queue = queue;
        _processor = processor;
    }

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

Registra el procesador y el consumidor hospedado; la cola y el log de auditoría ya están registrados:

```csharp
builder.Services.AddSingleton<TaskEventProcessor>();
builder.Services.AddHostedService<TaskEventConsumer>();
```

`BackgroundService` arranca cuando arranca el host. `ReadAllAsync` espera eventos y termina cuando se cancela el token de parada. El consumidor es singleton; la cola y el procesador deben ser seguros para uso concurrente.

## Refactor

La cola en proceso es un sustituto didáctico de un broker:

- Los mensajes viven en memoria y desaparecen al reiniciar. Un broker durable (SQS, RabbitMQ, Kafka) los persiste y agrega reintentos y dead-letter queues.
- La entrega suele ser al menos una vez. Los consumidores deben ser idempotentes; la auditoría tolera duplicados, un pago no.
- Un outbox transaccional resuelve el hueco entre "la escritura se confirmó" y "el mensaje se perdió". La escritura y la fila del outbox se confirman juntas, y un publicador drena el outbox.
- Un servicio en segundo plano puede solaparse con la siguiente corrida programada si una tarda más que el intervalo. Usa un lock o un lease distribuido cuando el job no deba correr dos veces.

Aquí no hace falta cambiar código: la cola, el publicador y el consumidor ya tienen responsabilidades separadas. Los tests de la solución pasan después del refactor. Los tests del trabajo en segundo plano deben esperar una condición observable, no un sleep fijo. El log de auditoría es el estado observable; el plazo de cinco segundos es solo el límite de fallo.

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

    CollectionAssert.Contains(audit.Entries.ToList(), $"Done:{task.Id}");
}
```

Se agota el tiempo porque el publicador solo emite eventos `Created`. Agrega la publicación después de una llamada exitosa a `MarkDone`:

```csharp
public TaskItem? MarkDone(int id)
{
    var task = _inner.MarkDone(id);

    if (task is not null)
    {
        _queue.TryPublish(new TaskEvent(task.Id, task.Title, "Done"));
    }

    return task;
}
```

### Una limpieza programada

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

Antes de agregar el job, este test no compila porque aún no existe `TaskCleanupJob`. La interfaz del escritor también necesita una operación `DeleteDone()`.

Agrega la operación a `ITaskWriter` y delégala desde el publicador:

```csharp
int DeleteDone();

public int DeleteDone() => _inner.DeleteDone();
```

Agrega la operación de borrado al escritor EF:

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
    private readonly ITaskWriter _writer;
    private readonly IAuditLog _audit;

    public TaskCleanupJob(ITaskWriter writer, IAuditLog audit)
    {
        _writer = writer;
        _audit = audit;
    }

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

    public TaskCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<CleanupOptions> options)
    {
        _scopeFactory = scopeFactory;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var job = scope.ServiceProvider.GetRequiredService<TaskCleanupJob>();
                job.Run();
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(_options.Value.IntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
```

El servicio es singleton; el job es scoped porque usa el escritor scoped. `CreateScope()` le da al job un contexto nuevo en cada corrida.

Registra el job como scoped y el planificador como servicio hospedado:

```csharp
builder.Services.AddScoped<TaskCleanupJob>();
builder.Services.AddHostedService<TaskCleanupService>();
```

El test pasa. El servicio hospedado ejecuta el mismo job al arrancar y después en cada intervalo configurado.

## Para cerrar

Lo que cubrimos:

- `Channel<T>` como cola en proceso.
- Un decorador publicador alrededor del escritor.
- Un consumidor `BackgroundService` con token de cancelación.
- Un job de limpieza scoped invocado desde un servicio singleton.
- Un test que espera una entrada observable de auditoría.
- Las diferencias entre este diseño y un broker durable.

Siguiente: [v12: OpenAPI](../v12-openapi/v12-openapi.es.md).
