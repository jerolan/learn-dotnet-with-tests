# v15: Real-time updates with SignalR

[The code for this version lives here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v15-signalr).

The TaskTracker API can save a task and return it to the caller. A dashboard that needs to show new tasks still has to poll `GET /tasks`; most requests return the same list, and a change can sit unseen until the next poll. The v14 API already puts task events on an in-process queue. We can send those events to connected clients instead.

This follows the small steps in [Learn Go with Tests' WebSockets chapter](https://quii.gitbook.io/learn-go-with-tests/build-an-application/websockets): first establish the web connection, then test a message reaching the server, then make server output observable by a connected client. Here, ASP.NET Core SignalR manages the connection and message protocol.

## How it works

A hub is the endpoint for SignalR connections. A hub does not need methods when the server only pushes messages. Code outside a hub can send through `IHubContext`; the v14 `TaskEventProcessor` is already the place where queued events are handled. Microsoft documents `IHubContext` for sending to clients outside a hub in the [SignalR hub context guide](https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext?view=aspnetcore-6.0).

The event path will be:

```text
POST /tasks → save task → queue TaskEvent → process event → send TaskChanged
```

The endpoint returns after saving the task. The background consumer later broadcasts the event. The hub route will require the same JWT authentication as the task routes.

## How to test

Use a real .NET `HubConnection` against the `WebApplicationFactory` test server. Subscribe before calling `StartAsync`, so the handler is ready before the connection can receive a message. The test server accepts HTTP requests through its own handler, so configure the SignalR client to use long polling for this test. SignalR supports WebSockets, Server-Sent Events and long polling; in production the client can select the best transport supported by both sides. See Microsoft's [SignalR transport overview](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-6.0) and [.NET client guide](https://learn.microsoft.com/en-us/aspnet/core/signalr/dotnet-client?view=aspnetcore-6.0).

The test client also needs the same bearer token as an API client. The SignalR .NET client accepts it through `AccessTokenProvider`, as described in Microsoft's [SignalR configuration guide](https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration?view=aspnetcore-6.0).

## Write the test first

Create a completion source before opening the connection. Then create a task over HTTP and wait for the matching event:

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

The completion source does not create the event; it only lets the test wait for the real client callback. The five-second bound turns a missing message into a failure instead of a hung test.

## Try to run the test

Run just this behavior:

```sh
dotnet test build-an-app/v15-signalr/TaskTracker.sln \
  --filter FullyQualifiedName~AuthenticatedClientReceivesCreatedTaskEvent
```

Against v14, the client cannot negotiate a connection because `/hubs/tasks` does not exist:

```text
Failed AuthenticatedClientReceivesCreatedTaskEvent [1 s]
System.Net.Http.HttpRequestException: Response status code does not indicate success: 404 (Not Found).
```

That is the missing behavior. The test reaches the real HTTP boundary and fails before it can send or receive a task event.

## Write the minimal amount of code to make the test run and check the failing test output

Register SignalR, map the route, and add an empty hub:

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

The client can now connect, but no code sends its event. The same test gets past negotiation and fails while waiting:

```text
System.TimeoutException: The operation has timed out.
```

The timeout tells us the route works and the missing piece is the event delivery.

## Write enough code to make it pass

The event processor runs outside the hub, so give it an `IHubContext`. Record the existing audit entry, then send the event to all connected clients:

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

The hosted consumer must await the asynchronous send before it moves on to the next event:

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

Run the focused test again:

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1
```

The task was saved through the API, processed from the existing queue, and received by a connected SignalR client.

## Refactor

The event queue remains the one source of task events. Sending from `TaskEventProcessor` avoids publishing a second notification directly from the HTTP endpoint. `IHubContext` is the supported way to send to connected clients from a service outside a hub.

There is no reason to add a mock for the hub. The integration test proves that a connected client receives the event. The test also avoids `Thread.Sleep`: it waits for the callback and fails after five seconds if the callback never arrives.

## Repeat for new requirements

The API already protects task routes. The new hub carries task data too, so it must require authentication. First add a test for an anonymous negotiation request:

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

Before the authorization rule, it fails because negotiation succeeds:

```text
Assert.AreEqual failed. Expected:<Unauthorized>. Actual:<OK>.
```

Require authorization when mapping the endpoint:

```csharp
app.MapHub<TaskUpdatesHub>("/hubs/tasks").RequireAuthorization();
```

The test connection obtains a token from the existing `/auth/token` endpoint and supplies it to the client:

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

Both new tests pass. The complete v15 solution passes all 34 tests:

```text
Passed!  - Failed:     0, Passed:    34, Skipped:     0, Total:    34
```

## A note on source control

Once the tests are green, save the working version before starting the next refactor. Each Part 3 version is a standalone solution, so v15 keeps its own copy of the TaskTracker code rather than referencing v14.

## Wrapping up

The TaskTracker now:

- Maps an authenticated SignalR hub at `/hubs/tasks`.
- Sends created and completed task events from the existing event consumer.
- Proves delivery with a connected .NET client and proves that anonymous negotiation is rejected.

SignalR is a notification channel, not durable storage. A disconnected client can miss an event, so it should reload task state from the API after reconnecting. The [Part 4 SignalR appendix](../../qa/signalr/signalr.md) covers connection lifecycle and delivery limits.
