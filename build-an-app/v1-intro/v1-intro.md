# v1: The API and Its First Tests

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v1-intro)

This part builds one application: `TaskTracker`, a task API. Each version adds one concern and stays runnable on its own. Version 1 is the smallest useful API: create a task, list tasks and fetch one by id.

The finished solution is in `build-an-app/v1-intro`. Run it with `dotnet run --project src/TaskTracker.Api` and test it with `dotnet test`.

## How it works

Minimal API handlers bind a JSON request to a record and resolve the store from dependency injection. `Results.Created` returns 201 with a `Location` header; the constrained GET route only matches an integer id.

## How to test

`WebApplicationFactory<Program>` starts the app in memory. Each `Api` owns a factory and client, so each test gets a fresh store and can exercise the public HTTP boundary.

```csharp
private sealed class Api : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public Api()
    {
        Client = _factory.CreateClient();
    }

    public HttpClient Client { get; }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
```

## Write the test first

Create the web project and the test project, wire the test host package and the reference, and add the first acceptance test:

```csharp
[TestClass]
public sealed class TaskApiTests
{
    [TestMethod]
    public async Task CanCreateAndGetATask()
    {
        using var api = new Api();

        var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TaskItem>();
        Assert.IsNotNull(created);
        Assert.AreEqual(1, created.Id);
        Assert.AreEqual("Write tests", created.Title);
        Assert.IsFalse(created.IsDone);

        var fetched = await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{created.Id}");

        Assert.IsNotNull(fetched);
        Assert.AreEqual(created, fetched);
    }
}
```

The test drives the public API. It does not know how tasks are stored.

## Try to run the test

The app starts with no endpoints:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Run();

public sealed record CreateTaskRequest(string Title);

public sealed record TaskItem(int Id, string Title, bool IsDone);

public partial class Program
{
}
```

The test fails with `Expected:<Created>. Actual:<NotFound>`. The route does not exist.

## Write the minimal amount of code for the test to run and check the failing test output

Add a store and register it:

```csharp
public sealed class InMemoryTaskStore
{
    private readonly List<TaskItem> _tasks = new();
    private int _nextId = 1;

    public IReadOnlyList<TaskItem> All()
    {
        return _tasks;
    }

    public TaskItem? Find(int id)
    {
        return _tasks.FirstOrDefault(task => task.Id == id);
    }

    public TaskItem Add(string title)
    {
        var task = new TaskItem(_nextId++, title, false);
        _tasks.Add(task);
        return task;
    }
}
```

and:

```csharp
builder.Services.AddSingleton<InMemoryTaskStore>();
```

Wire the POST endpoint:

```csharp
app.MapPost("/tasks", (CreateTaskRequest request, InMemoryTaskStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }

    var task = store.Add(request.Title.Trim());

    return Results.Created($"/tasks/{task.Id}", task);
});
```

The POST now succeeds, but the GET side of the same test fails with `NotFound`.

## Write enough code to make it pass

```csharp
app.MapGet("/tasks/{id:int}", (int id, InMemoryTaskStore store) =>
{
    var task = store.Find(id);

    return task is null ? Results.NotFound() : Results.Ok(task);
});
```

Green. The test creates a task, reads the id from the response and fetches it back.

## Refactor

The API is small, and the acceptance test does not depend on how tasks are stored. Keep the request, response and storage responsibilities separate as later versions add new concerns.

### Keep the boundaries clear

- `CreateTaskRequest` is the input shape; `TaskItem` is the output shape and the domain value. They can diverge in later versions without changing every caller.
- `InMemoryTaskStore` is the storage boundary. Version 4 replaces it with EF Core and the endpoints keep their shape.

The in-memory store is not thread-safe. That is acceptable for a single-request tutorial version; a real store needs a lock, a concurrent collection or a database transaction. Version 4 moves the state into SQLite.

## Repeat for new requirements

### List the tasks

Write the test:

```csharp
[TestMethod]
public async Task List_ReturnsCreatedTasks()
{
    using var api = new Api();

    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("First"));
    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Second"));

    var tasks = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    Assert.IsNotNull(tasks);
    Assert.AreEqual(2, tasks.Count);
    Assert.AreEqual("First", tasks[0].Title);
    Assert.AreEqual("Second", tasks[1].Title);
}
```

Add the endpoint:

```csharp
app.MapGet("/tasks", (InMemoryTaskStore store) => store.All());
```

Green. The store appends in creation order, and the test pins that order as part of the feature.

### An unknown id

Write the test:

```csharp
[TestMethod]
public async Task UnknownTask_ReturnsNotFound()
{
    using var api = new Api();

    var response = await api.Client.GetAsync("/tasks/99");

    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
}
```

It passes with the null check in the GET endpoint. The 404 is part of the contract, not an accident.

### A blank title

Write the test:

```csharp
[TestMethod]
public async Task BlankTitle_ReturnsBadRequest()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest(" "));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
}
```

It passes with the guard in the POST endpoint. The title is trimmed before it reaches the store, so a title with surrounding spaces is stored cleanly.

## Wrapping up

What we have covered:

- A minimal API with POST, GET by id and GET list.
- A singleton in-memory store.
- Request and response records.
- Route constraints, 201 Created with a Location header, 400 and 404.
- Acceptance tests with `WebApplicationFactory` and a per-test factory.

Next: [v2: JSON, routing and a consistent error shape](../v2-json-routing/v2-json-routing.md).

