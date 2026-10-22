# v15: Actualizaciones en tiempo real con SignalR

[El código de esta versión está aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v15-signalr).

La API TaskTracker puede guardar una tarea y devolvérsela a quien la creó. Un tablero que quiera mostrar tareas nuevas aún debe consultar `GET /tasks` periódicamente; casi todas las respuestas contienen la misma lista y un cambio puede tardar en aparecer. La API de v14 ya pone los eventos de tareas en una cola dentro del proceso. Podemos enviar esos eventos a los clientes conectados.

Seguimos los pasos pequeños del capítulo [WebSockets de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/build-an-application/websockets): primero establecemos la conexión web, luego probamos un mensaje que llegue al servidor y después hacemos observable la salida del servidor para un cliente conectado. Aquí, ASP.NET Core SignalR administra la conexión y el protocolo de mensajes.

## Cómo funciona

Un hub es el endpoint de las conexiones SignalR. No necesita métodos cuando el servidor solo envía mensajes. El código fuera de un hub puede enviar mensajes mediante `IHubContext`; `TaskEventProcessor` de v14 ya es el lugar donde se procesan los eventos de la cola. Microsoft documenta `IHubContext` para enviar mensajes a clientes desde fuera del hub en la [guía de contexto de SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext?view=aspnetcore-6.0).

El recorrido del evento será:

```text
POST /tasks → guardar tarea → encolar TaskEvent → procesar evento → enviar TaskChanged
```

El endpoint responde después de guardar la tarea. Más tarde, el consumidor en segundo plano difunde el evento. La ruta del hub requerirá el mismo JWT que las rutas de tareas.

## Cómo probarlo

Usa un `HubConnection` real de .NET contra el servidor de pruebas de `WebApplicationFactory`. Registra el callback antes de llamar a `StartAsync`, para que el handler esté listo antes de que la conexión pueda recibir un mensaje. El servidor de pruebas recibe peticiones HTTP mediante su propio handler, así que configura el cliente SignalR para usar long polling en este test. SignalR admite WebSockets, Server-Sent Events y long polling; en producción, el cliente puede elegir el transporte más adecuado que ambos lados soporten. Consulta la [descripción de transportes de SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-6.0) y la [guía del cliente .NET](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client?view=aspnetcore-6.0) de Microsoft.

El cliente de pruebas también necesita el mismo bearer token que un cliente de la API. El cliente SignalR de .NET lo recibe mediante `AccessTokenProvider`, como explica la [guía de configuración de SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration?view=aspnetcore-6.0) de Microsoft.

## Escribe primero el test

Crea una fuente de finalización antes de abrir la conexión. Luego crea una tarea por HTTP y espera el evento correspondiente:

```csharp
[TestMethod]
public async Task AuthenticatedClientReceivesCreatedTaskEvent()
{
    using var api = new Api();
    var received = new TaskCompletionSource<TaskEvent>(
        TaskCreationOptions.RunContinuationsAsynchronously);

    await using var connection = await api.ConnectToTaskUpdatesAsync(
        hub => hub.On<TaskEvent>("TaskChanged", taskEvent => received.TrySetResult(taskEvent)));

    var response = await api.Client.PostAsJsonAsync(
        "/tasks",
        new CreateTaskRequest("Write the SignalR chapter"));

    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

    var created = await response.Content.ReadFromJsonAsync<TaskItem>();
    Assert.IsNotNull(created);

    var published = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.AreEqual("Created", published.Type);
    Assert.AreEqual(created.Id, published.TaskId);
    Assert.AreEqual(created.Title, published.Title);
}
```

La fuente de finalización no crea el evento; solo permite que el test espere el callback real del cliente. El límite de cinco segundos convierte un mensaje ausente en un fallo, en vez de dejar el test colgado.

## Intenta correrlo

Ejecuta solo este comportamiento:

```sh
dotnet test build-an-app/v15-signalr/TaskTracker.sln \
  --filter FullyQualifiedName~AuthenticatedClientReceivesCreatedTaskEvent
```

Contra v14, el cliente no puede negociar la conexión porque `/hubs/tasks` no existe:

```text
Failed AuthenticatedClientReceivesCreatedTaskEvent [1 s]
System.Net.Http.HttpRequestException: Response status code does not indicate success: 404 (Not Found).
```

Ese es el comportamiento faltante. El test llega al límite HTTP real y falla antes de enviar o recibir un evento de tarea.

## Escribe el código mínimo para hacer que el test corra y revisa el fallo

Registra SignalR, mapea la ruta y agrega un hub vacío:

```csharp
builder.Services.AddSignalR();

app.MapHub<TaskUpdatesHub>("/hubs/tasks");
```

```csharp
using Microsoft.AspNetCore.SignalR;

namespace TaskTracker.Api;

public sealed class TaskUpdatesHub : Hub
{
}
```

El cliente ya puede conectarse, pero ningún código envía su evento. El mismo test supera la negociación y falla mientras espera:

```text
System.TimeoutException: The operation has timed out.
```

El timeout indica que la ruta funciona y que falta entregar el evento.

## Escribe suficiente código para hacerlo pasar

El procesador de eventos se ejecuta fuera del hub, así que recibe un `IHubContext`. Registra la entrada de auditoría existente y luego envía el evento a todos los clientes conectados:

```csharp
public sealed class TaskEventProcessor
{
    private readonly IAuditLog _audit;
    private readonly IHubContext<TaskUpdatesHub> _hub;

    public TaskEventProcessor(IAuditLog audit, IHubContext<TaskUpdatesHub> hub)
    {
        _audit = audit;
        _hub = hub;
    }

    public async Task ProcessAsync(TaskEvent taskEvent, CancellationToken cancellationToken)
    {
        _audit.Record($"{taskEvent.Type}:{taskEvent.TaskId}");
        await _hub.Clients.All.SendAsync("TaskChanged", taskEvent, cancellationToken);
    }
}
```

El consumidor hospedado debe esperar el envío asíncrono antes de procesar el siguiente evento:

```csharp
await foreach (var taskEvent in _queue.ReadAllAsync(stoppingToken))
{
    await _processor.ProcessAsync(taskEvent, stoppingToken);
    _logger.LogInformation(
        "Processed {Type} for task {TaskId}",
        taskEvent.Type,
        taskEvent.TaskId);
}
```

Ejecuta de nuevo el test enfocado:

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1
```

La tarea se guardó mediante la API, se procesó desde la cola existente y llegó a un cliente SignalR conectado.

## Refactoriza

La cola de eventos sigue siendo la única fuente de eventos de tareas. Enviar desde `TaskEventProcessor` evita publicar una segunda notificación directamente desde el endpoint HTTP. `IHubContext` es la forma admitida de enviar a clientes conectados desde un servicio que está fuera del hub.

No hace falta agregar un mock para el hub. El test de integración demuestra que un cliente conectado recibe el evento. El test tampoco usa `Thread.Sleep`: espera el callback y falla después de cinco segundos si nunca llega.

## Repite con nuevos requisitos

La API ya protege las rutas de tareas. El nuevo hub también transmite datos de tareas, así que debe requerir autenticación. Primero agrega un test para una negociación anónima:

```csharp
[TestMethod]
public async Task TaskHubNegotiation_RequiresAuthentication()
{
    using var api = new Api(authenticate: false);

    using var response = await api.Client.PostAsync(
        "/hubs/tasks/negotiate?negotiateVersion=1",
        content: null);

    Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

Antes de la regla de autorización, falla porque la negociación tiene éxito:

```text
Assert.AreEqual failed. Expected:<Unauthorized>. Actual:<OK>.
```

Exige autorización al mapear el endpoint:

```csharp
app.MapHub<TaskUpdatesHub>("/hubs/tasks").RequireAuthorization();
```

La conexión de pruebas obtiene un token del endpoint existente `/auth/token` y se lo entrega al cliente:

```csharp
var accessToken = await GetTokenAsync();
var connection = new HubConnectionBuilder()
    .WithUrl(new Uri(_factory.Server.BaseAddress, "hubs/tasks"), options =>
    {
        options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
        options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
        options.Transports = HttpTransportType.LongPolling;
    })
    .Build();
```

Los dos tests nuevos pasan. La solución completa de v15 pasa sus 34 tests:

```text
Passed!  - Failed:     0, Passed:    34, Skipped:     0, Total:    34
```

## Una nota sobre control de versiones

Cuando los tests estén verdes, guarda esa versión funcional antes de iniciar el siguiente refactor. Cada versión de la Parte 3 tiene una solución independiente, así que v15 conserva su propia copia del código de TaskTracker en vez de referenciar v14.

## Para cerrar

TaskTracker ahora:

- Mapea un hub SignalR autenticado en `/hubs/tasks`.
- Envía eventos de tareas creadas y completadas desde el consumidor existente.
- Comprueba la entrega con un cliente .NET conectado y que la negociación anónima se rechaza.

SignalR es un canal de notificación, no almacenamiento duradero. Un cliente desconectado puede perder un evento, así que debe volver a cargar el estado de las tareas desde la API al reconectarse. El [apéndice de SignalR de la Parte 4](../../qa/signalr/signalr.es.md) cubre el ciclo de vida de la conexión y los límites de entrega.
