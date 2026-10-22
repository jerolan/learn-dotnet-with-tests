# Real-time updates with SignalR

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/signalr)

A client that needs to know when a task appears can poll `GET /tasks` every few seconds. The update arrives late, most requests return the same list, and the server pays for every poll. SignalR keeps a connection open and lets the server push the change.

This appendix adds a hub to the minimal API, broadcasts a created task to every connected client, and tests the push with a real `HubConnection` against `WebApplicationFactory`.

## The hub

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

The hub is the server side of the connection. `Clients.Caller` addresses the connection that just opened; `Context.ConnectionId` is its identifier. The override is optional, but it gives the test something to observe during the handshake.

## The API

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

`AddSignalR` registers the services. `MapHub` exposes the endpoint clients connect to. The POST does not need a caller; `IHubContext<TaskHub>` lets an ordinary endpoint send to every connection, which is how a background job would broadcast too.

## Write the test first

`tests/TaskApi.Tests/TaskHubTests.cs`, with the setup omitted for a moment:

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

The test registers the handler before the connection starts. The `Connected` message is sent during the handshake; a handler added after `StartAsync` can miss it.

`WaitAsync` turns a lost message into a failed test after five seconds instead of a test run that never ends.

## Try to run it

Without `AddSignalR`, the request pipeline cannot resolve `IHubContext<TaskHub>`:

```text
System.InvalidOperationException: Failure to infer one or more parameters.
Below is the list of parameters that we found:

Parameter           | Source
-----------------------------------
task                | Body (Inferred)
hub                 | UNKNOWN

Did you mean to register the "UNKNOWN" parameters as a Service?
```

The message names the missing service without naming SignalR. Add `builder.Services.AddSignalR()` and map the hub with `app.MapHub<TaskHub>("/hubs/tasks")`. Without the mapping the endpoint resolves, but a client that connects to `/hubs/tasks` gets a 404.

## Connect the test client

`WebApplicationFactory` runs the app in memory. The SignalR client needs an `HttpMessageHandler` that talks to that in-memory server:

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

`CreateHandler` is the bridge to `TestServer`. Long polling is the transport that works through it; WebSockets need a real socket, so the in-memory server does not offer them.

Run the tests:

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 328 ms
```

The second test opens a connection and asserts that the `Connected` message carries the same id as `connection.ConnectionId`.

## What the push does not guarantee

`Clients.All.SendAsync` is fire and forget from the caller's point of view. If a client is disconnected, the message is gone. SignalR does not keep a queue per connection.

That is fine for a notification that the client can recover by refreshing the list. It is not fine for a command that must happen exactly once. Keep the durable record in the database and treat the push as a hint to read it. The task endpoint already returns the created task; a client that missed the event can fetch `GET /tasks`.

More than one server needs a backplane, such as Redis, so a message published on one instance reaches connections held by another. Sticky sessions alone keep a connection on one server; they do not fan a broadcast out to the rest.

## Refactor for production

- Authenticate the hub. `[Authorize]` on the hub and a token in the connection URL; then `Context.User` is available.
- Address groups instead of everyone. `Groups.AddToGroupAsync` and `Clients.Group(...)` keep one tenant from seeing another's tasks.
- Configure the client to reconnect, and reload state when it does. A reconnect is a new connection with a new id.
- Put a size limit on messages and send identifiers, not whole entities, when the payload can grow.
- Test the hub through a connection, as here. Mocking `IHubContext` proves that a method was called, not that a client receives the message.

## Wrapping up

What we have covered:

- A hub that sends to the caller during the handshake.
- `AddSignalR`, `MapHub` and an endpoint that broadcasts through `IHubContext`.
- A test client wired to `TestServer` with long polling.
- The five-second wait that turns a missing message into a failure.
- What a push does not guarantee, and the backplane, auth and groups a real deployment needs.

Next: gRPC.
