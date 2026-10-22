# GraphQL

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/graphql)

A REST endpoint returns one shape. A screen that needs the task id and title gets the whole task, and a screen that needs a different combination gets a second endpoint or a query parameter that grows every sprint.

GraphQL publishes one schema. The client sends a query that names the fields it needs, and the server returns that shape. This appendix puts a query and a mutation over the task store, then tests them through HTTP with `WebApplicationFactory`.

## The schema classes

`src/TaskApi/TaskItem.cs` and `TaskStore.cs` are the same in-memory store used elsewhere:

```csharp
public sealed record TaskItem(Guid Id, string Title);

public sealed class TaskStore
{
    private readonly ConcurrentDictionary<Guid, TaskItem> _tasks = new();

    public TaskItem Add(string title)
    {
        var task = new TaskItem(Guid.NewGuid(), title);
        _tasks[task.Id] = task;
        return task;
    }

    public TaskItem? Find(Guid id)
    {
        return _tasks.TryGetValue(id, out var task) ? task : null;
    }

    public IReadOnlyList<TaskItem> All()
    {
        return _tasks.Values.OrderBy(task => task.Title).ToList();
    }
}
```

The query type exposes reads:

```csharp
using HotChocolate;

namespace TaskApi;

public sealed class Query
{
    public IReadOnlyList<TaskItem> GetTasks([Service] TaskStore store)
    {
        return store.All();
    }

    public TaskItem? GetTask(Guid id, [Service] TaskStore store)
    {
        return store.Find(id);
    }
}
```

`[Service]` tells Hot Chocolate to resolve the parameter from DI, the same way minimal API injection works for an endpoint. The method names become field names with the first letter lowercased: `tasks` and `task`.

The mutation writes:

```csharp
public sealed class Mutation
{
    public TaskItem CreateTask(string title, [Service] TaskStore store)
    {
        return store.Add(title);
    }
}
```

## The server

`src/TaskApi/Program.cs`:

```csharp
using TaskApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TaskStore>();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

var app = builder.Build();

app.MapGraphQL("/graphql");

app.Run();

public partial class Program
{
}
```

One endpoint serves queries, mutations and introspection. The schema is built from the two classes at startup; a resolver that cannot be wired fails before the first request.

## Write the test first

The test posts GraphQL documents as JSON, the same way a client would:

```csharp
[TestMethod]
public async Task MutationCreatesTaskAndQueryReadsIt()
{
    var created = await PostAsync(
        "mutation { createTask(title: \"Write the chapter\") { id title } }");

    var task = created.GetProperty("createTask");
    var id = task.GetProperty("id").GetString();

    Assert.AreEqual("Write the chapter", task.GetProperty("title").GetString());

    var read = await PostAsync($"{{ task(id: \"{id}\") {{ id title }} }}");

    Assert.AreEqual(id, read.GetProperty("task").GetProperty("id").GetString());
    Assert.AreEqual("Write the chapter", read.GetProperty("task").GetProperty("title").GetString());
}
```

The helper unwraps `data` and fails when the response contains an `errors` array:

```csharp
private async Task<JsonElement> PostAsync(string query)
{
    var response = await _client.PostAsJsonAsync("/graphql", new { query });
    response.EnsureSuccessStatusCode();

    var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.IsFalse(
        payload.TryGetProperty("errors", out var errors),
        errors.ToString());

    return payload.GetProperty("data");
}
```

The second test asks for a random id and expects `task` to be JSON `null`. That is the schema's way of saying the field was resolved and there is no task; an error would mean the query itself was wrong.

## Try to run it

With the server built but `MapGraphQL` missing:

```text
System.Net.Http.HttpRequestException: Response status code does not indicate success: 404 (Not Found).
   at System.Net.Http.HttpResponseMessage.EnsureSuccessStatusCode()
```

The endpoint does not exist. Add `app.MapGraphQL("/graphql")` and the same documents reach the schema.

Run the tests:

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 893 ms
```

## How the response differs from REST

A GraphQL response carries `data` and, when something goes wrong during execution, an `errors` array. The HTTP status is often `200` even when `errors` is present, because the request was well formed and part of the data may have resolved. A client that only checks the status code will miss those errors.

The client chooses fields, so `{ task(id: "...") { id } }` returns no title. That is the feature: less data over the wire and no new endpoint for each screen. It is also a risk: a client can ask for deeply nested fields and turn one request into thousands of database queries.

## Production concerns

- Batch the repeated lookups. A list of tasks whose resolver loads a customer per task is the N+1 problem; Hot Chocolate's DataLoader groups those keys.
- Limit depth and complexity. Without a limit, a nested query can exhaust the server.
- Authorize fields, not only endpoints. One endpoint serves every field, so a check at the route level cannot tell a public title from a private note.
- Disable introspection if the schema should not be public. It is useful in development and unnecessary exposure in production.
- Prefer persisted queries for known clients. The server accepts an identifier instead of an arbitrary document, which also makes caching possible.
- Keep mutations explicit. A mutation that returns a full object invites clients to select fields the write path did not intend to expose.

## What is deferred

There is no pagination, no DataLoader, no subscription and no field-level authorization here. A hands-on lab would add a `tasks(first: 10, after: ...)` connection, a batched customer lookup with a counter proving one query, and a subscription over the SignalR or WebSocket transport.

## Wrapping up

What we have covered:

- Query and mutation classes with constructor-free resolvers and `[Service]` injection.
- A single `/graphql` endpoint built with `AddGraphQLServer`.
- Tests that post documents as JSON and inspect `data` and `errors`.
- The 404 when the endpoint is not mapped.
- N+1, depth limits, field authorization and persisted queries as production concerns.

Next: AsyncAPI.
