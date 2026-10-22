# gRPC

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/grpc)

A JSON endpoint is easy to call from a browser and easy to read in a log. For a call between two services it also repeats the same field names on every request, and nothing in the HTTP contract tells a client which fields are required.

gRPC starts from a `.proto` file. The tooling generates a server base class and a client from that file, and the messages travel as binary over HTTP/2. This appendix defines a small task service, calls it through `WebApplicationFactory`, and checks both the success path and the not-found status.

## How it works

The `.proto` file is the shared wire contract. Field numbers identify values on the wire; the generated C# names make those values convenient to use from a typed client.

`src/TaskApi/Protos/tasks.proto`:

```proto
syntax = "proto3";

option csharp_namespace = "TaskApi.Grpc";

package tasks;

service TaskService {
  rpc Create (CreateTaskRequest) returns (TaskReply);
  rpc Get (GetTaskRequest) returns (TaskReply);
}

message CreateTaskRequest {
  string title = 1;
}

message GetTaskRequest {
  string id = 1;
}

message TaskReply {
  string id = 1;
  string title = 2;
}
```

The numbers are the wire format. A client and a server agree on field `1` meaning `id`; the names are for the generated code and the logs. Never renumber a field that has shipped, and never reuse a number for a different meaning. Remove a field by reserving its number.

### Generate the code

The server project references `Grpc.AspNetCore` and includes the proto:

```xml
<ItemGroup>
  <PackageReference Include="Grpc.AspNetCore" Version="2.52.0" />
</ItemGroup>

<ItemGroup>
  <Protobuf Include="Protos\tasks.proto" GrpcServices="Both" />
</ItemGroup>
```

`GrpcServices="Both"` generates the server base class and the client in the same assembly, which keeps this appendix to one project. A real system puts the messages and the client in a contracts package, so the server and its callers version that package together.

### The service

`src/TaskApi/TaskGrpcService.cs`:

```csharp
using System.Collections.Concurrent;
using Grpc.Core;
using TaskApi.Grpc;

namespace TaskApi;

public sealed class TaskGrpcService : TaskService.TaskServiceBase
{
    private readonly ConcurrentDictionary<Guid, string> _tasks = new();

    public override Task<TaskReply> Create(CreateTaskRequest request, ServerCallContext context)
    {
        var id = Guid.NewGuid();
        _tasks[id] = request.Title;

        return Task.FromResult(new TaskReply { Id = id.ToString(), Title = request.Title });
    }

    public override Task<TaskReply> Get(GetTaskRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id) || !_tasks.TryGetValue(id, out var title))
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Task not found."));
        }

        return Task.FromResult(new TaskReply { Id = id.ToString(), Title = title });
    }
}
```

`RpcException` with a `StatusCode` becomes the gRPC status the client sees. It is not an HTTP 404 in the response body; the status travels in the HTTP/2 trailers.

### The server

`src/TaskApi/Program.cs`:

```csharp
using TaskApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TaskGrpcService>();

var app = builder.Build();

app.MapGet("/", () => Results.Text("gRPC endpoint: use a gRPC client."));

app.Run();

public partial class Program
{
}
```

The singleton registration is what makes the dictionary survive between calls. Without it, the service is created per call and each request starts with an empty dictionary. A real service would keep its data in a database; the lifetime still matters for any dependency it holds.

At the first test checkpoint, the service is registered but gRPC and its route are not configured yet.

## How to test

`WebApplicationFactory` supplies an in-memory server, and the gRPC channel routes through its `TestServer` handler. No port or external service is needed. Run `dotnet test Grpc.sln` from this appendix folder.

## Write the test first

The contract and service are already in place; this first test proves that a generated client can create a task and read it back.

`tests/TaskApi.Tests/TaskGrpcTests.cs`:

```csharp
[TestMethod]
public async Task CreatedTaskCanBeReadBack()
{
    var created = await _client.CreateAsync(new CreateTaskRequest { Title = "Write the chapter" });

    var fetched = await _client.GetAsync(new GetTaskRequest { Id = created.Id });

    Assert.AreEqual(created.Id, fetched.Id);
    Assert.AreEqual("Write the chapter", fetched.Title);
}
```

The client needs a channel pointed at the in-memory server:

```csharp
_factory = new WebApplicationFactory<Program>();
_channel = GrpcChannel.ForAddress(_factory.Server.BaseAddress, new GrpcChannelOptions
{
    HttpHandler = _factory.Server.CreateHandler()
});
_client = new TaskService.TaskServiceClient(_channel);
```

`CreateHandler` routes the channel through `TestServer`; no port is opened. gRPC needs HTTP/2, and the in-memory handler supports it.

## Try to run the test

With the proto generated but the service not mapped, both calls fail:

```text
Grpc.Core.RpcException: Status(StatusCode="Unimplemented", Detail="Bad gRPC response. HTTP status code: 404")
```

The client speaks gRPC; the server has no gRPC endpoint, so the request falls through to a 404 and the client reports `Unimplemented`.

## Write the minimal amount of code for the test to run and check the failing test output

Register gRPC and map the existing service:

```csharp
builder.Services.AddGrpc();
app.MapGrpcService<TaskGrpcService>();
```

Without `AddGrpc()`, the service cannot be constructed. With both registrations, the test reaches the implementation.

## Write enough code to make it pass

Run the tests:

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 387 ms
```

## Refactor

### What the contract buys

- The generated client has typed methods and messages. A missing field is a compile error, not a `null` discovered in production.
- The payload is binary and smaller than the equivalent JSON, and HTTP/2 multiplexes calls over one connection.
- The status is part of the protocol. `NotFound`, `InvalidArgument`, `Unavailable` and `DeadlineExceeded` are distinguishable without inventing a JSON error shape.
- A deadline travels with the call. Set one on the client; the server gets a cancellation token in `ServerCallContext`.

The same proto also describes streaming: server streaming, client streaming and bidirectional streams. This appendix sticks to unary calls because they map cleanly onto the request and response the reader already knows.

### Production concerns

- Move the proto and the client into a versioned contracts package. Do not let every consumer generate its own copy from a file in the server repository.
- Add a deadline to every client call, and pass `context.CancellationToken` into database work.
- Add interceptors for authentication, logging and metrics instead of repeating that code in every method.
- Keep server reflection off in production unless you have a reason; it exposes the contract to anyone who can reach the port.
- Run over TLS. HTTP/2 without TLS is not supported by browsers and is not a safe default between hosts.
- Set maximum message sizes deliberately; the default is a limit, not a suggestion.
- Health checks and load balancing belong to the hosting platform; a gRPC service behind a plain L7 proxy may need HTTP/2 end to end.

## Repeat for new requirements

The next requirement is to return the protocol's not-found status for an unknown id:

```csharp
[TestMethod]
public async Task MissingTaskReturnsNotFound()
{
    var error = await Assert.ThrowsExceptionAsync<RpcException>(async () =>
        await _client.GetAsync(new GetTaskRequest { Id = Guid.NewGuid().ToString() }));

    Assert.AreEqual(StatusCode.NotFound, error.StatusCode);
}
```

The service already maps a missing id to `StatusCode.NotFound`, so the new test passes without changing the transport contract.

## What is deferred

There is no streaming sample, no interceptor, no TLS and no separate contracts package here. A hands-on lab would add a server-streaming method, a client that cancels halfway, and a deadline that expires against a deliberately slow handler.

## Wrapping up

What we have covered:

- A proto contract with messages and two unary methods.
- Generated server and client code, and why field numbers are permanent.
- A singleton gRPC service that maps not-found to `StatusCode.NotFound`.
- A channel wired to `TestServer`, with no open port.
- The `Unimplemented` failure that appears when the service is not mapped.
- Versioning, deadlines, interceptors, TLS and reflection as production concerns.

Next: GraphQL.
