# Actualizaciones en tiempo real con SignalR

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/signalr)

Un cliente que necesita saber cuándo aparece una tarea puede hacer polling a `GET /tasks` cada pocos segundos. La actualización llega tarde, casi todas las peticiones devuelven la misma lista y el servidor paga cada sondeo. SignalR mantiene una conexión abierta y deja que el servidor empuje el cambio.

Este apéndice agrega un hub a la API mínima, difunde una tarea creada a todos los clientes conectados y prueba el push con un `HubConnection` real contra `WebApplicationFactory`.

## El hub

`src/TaskApi/TaskHub.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;

namespace TaskApi;

public sealed class TaskHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }
}
```

El hub es el lado servidor de la conexión. `Clients.Caller` se dirige a la conexión que acaba de abrir; `Context.ConnectionId` es su identificador. El override es opcional, pero le da algo que observar al test durante el handshake.

## La API

`src/TaskApi/Program.cs`:

```csharp
using Microsoft.AspNetCore.SignalR;
using TaskApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/ping", () => Results.Ok(new { status = "ok" }));
app.MapHub<TaskHub>("/hubs/tasks");

app.MapPost("/tasks", async (TaskItem task, IHubContext<TaskHub> hub) =>
{
    await hub.Clients.All.SendAsync("TaskCreated", task);
    return Results.Created($"/tasks/{task.Id}", task);
});

app.Run();

public partial class Program
{
}
```

`AddSignalR` registra los servicios. `MapHub` expone el endpoint al que se conectan los clientes. El POST no necesita un llamador; `IHubContext<TaskHub>` permite que un endpoint común envíe a todas las conexiones, que es lo mismo que haría un job en segundo plano.

## Escribe primero el test

`tests/TaskApi.Tests/TaskHubTests.cs`, sin el setup por un momento:

```csharp
[TestMethod]
public async Task ConnectedClientReceivesCreatedTask()
{
    var received = new TaskCompletionSource<JsonElement>(
        TaskCreationOptions.RunContinuationsAsynchronously);

    await using var connection = await ConnectAsync(
        hub => hub.On<JsonElement>("TaskCreated", task => received.TrySetResult(task)));

    var task = new { id = Guid.NewGuid(), title = "Write the chapter" };
    var response = await _factory.CreateClient().PostAsJsonAsync("/tasks", task);

    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

    var payload = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

    Assert.AreEqual(task.id, payload.GetProperty("id").GetGuid());
    Assert.AreEqual("Write the chapter", payload.GetProperty("title").GetString());
}
```

El test registra el handler antes de que arranque la conexión. El mensaje `Connected` se envía durante el handshake; un handler agregado después de `StartAsync` puede perdérselo.

`WaitAsync` convierte un mensaje perdido en un test fallido a los cinco segundos, en vez de una corrida que nunca termina.

## Intenta correrlo

Sin `AddSignalR`, el pipeline no puede resolver `IHubContext<TaskHub>`:

```text
System.InvalidOperationException: Failure to infer one or more parameters.
Below is the list of parameters that we found:

Parameter           | Source
-----------------------------------
task                | Body (Inferred)
hub                 | UNKNOWN

Did you mean to register the "UNKNOWN" parameters as a Service?
```

El mensaje nombra el servicio que falta sin nombrar a SignalR. Agrega `builder.Services.AddSignalR()` y mapea el hub con `app.MapHub<TaskHub>("/hubs/tasks")`. Sin el mapeo el endpoint se resuelve, pero un cliente que se conecta a `/hubs/tasks` recibe 404.

## Conecta el cliente de test

`WebApplicationFactory` corre la app en memoria. El cliente de SignalR necesita un `HttpMessageHandler` que hable con ese servidor en memoria:

```csharp
private async Task<HubConnection> ConnectAsync(Action<HubConnection> beforeStart)
{
    var connection = new HubConnectionBuilder()
        .WithUrl(new Uri(_factory.Server.BaseAddress, "hubs/tasks"), options =>
        {
            options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            options.Transports = HttpTransportType.LongPolling;
        })
        .Build();

    beforeStart(connection);
    await connection.StartAsync();

    return connection;
}
```

`CreateHandler` es el puente hacia `TestServer`. Long polling es el transporte que funciona a través de él; WebSockets necesita un socket real, así que el servidor en memoria no los ofrece.

Corre los tests:

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 328 ms
```

El segundo test abre una conexión y comprueba que el mensaje `Connected` trae el mismo id que `connection.ConnectionId`.

## Lo que el push no garantiza

`Clients.All.SendAsync` es fire and forget desde el punto de vista del llamador. Si un cliente está desconectado, el mensaje se pierde. SignalR no guarda una cola por conexión.

Eso está bien para una notificación que el cliente recupera refrescando la lista. No está bien para un comando que debe ocurrir exactamente una vez. Guarda el registro durable en la base de datos y trata el push como una pista para leerlo. El endpoint de tareas ya devuelve la tarea creada; un cliente que se perdió el evento puede pedir `GET /tasks`.

Más de un servidor necesita un backplane, como Redis, para que un mensaje publicado en una instancia llegue a conexiones que sostiene otra. Las sesiones pegajosas mantienen una conexión en un servidor; no reparten una difusión a los demás.

## Refactor para producción

- Autentica el hub. `[Authorize]` en el hub y un token en la URL de conexión; después `Context.User` está disponible.
- Dirige grupos en vez de a todos. `Groups.AddToGroupAsync` y `Clients.Group(...)` evitan que un inquilino vea las tareas de otro.
- Configura el cliente para reconectar y recarga el estado cuando lo haga. Una reconexión es una conexión nueva con un id nuevo.
- Pon un límite de tamaño a los mensajes y envía identificadores, no entidades completas, cuando el payload puede crecer.
- Prueba el hub a través de una conexión, como aquí. Mockear `IHubContext` demuestra que se llamó a un método, no que un cliente recibe el mensaje.

## Para cerrar

Lo que cubrimos:

- Un hub que envía al llamador durante el handshake.
- `AddSignalR`, `MapHub` y un endpoint que difunde a través de `IHubContext`.
- Un cliente de test conectado a `TestServer` con long polling.
- La espera de cinco segundos que convierte un mensaje perdido en un fallo.
- Lo que un push no garantiza, y el backplane, la autenticación y los grupos que necesita un despliegue real.

Sigue: gRPC.
