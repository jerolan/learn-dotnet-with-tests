# v2: JSON, Routing and a Consistent Error Shape

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v2-json-routing)

Version 1 returned an anonymous `{ error }` object and used only GET and POST. Version 2 fixes the wire contract: a named error shape, a JSON configuration the tests can see, and a PATCH route that changes a task.

The finished solution is in `build-an-app/v2-json-routing`.

## Write the test first

```csharp
[TestMethod]
public async Task MarkDone_ChangesTheTask()
{
    using var api = new Api();
    var created = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
    var task = await created.Content.ReadFromJsonAsync<TaskItem>();

    var response = await api.Client.PatchAsync($"/tasks/{task!.Id}/done", null);

    Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

    var fetched = await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{task.Id}");
    Assert.IsNotNull(fetched);
    Assert.IsTrue(fetched.IsDone);
}
```

## Try to run the test

The route does not exist, so the PATCH returns `404 NotFound`. The assertion expects `204 NoContent` and fails.

## Write the minimal amount of code for the test to run and check the failing test output

Add the store operation:

```csharp
public TaskItem? MarkDone(int id)
{
    var index = _tasks.FindIndex(task => task.Id == id);

    if (index < 0)
    {
        return null;
    }

    var task = _tasks[index] with { IsDone = true };
    _tasks[index] = task;

    return task;
}
```

Wire the route:

```csharp
app.MapMethods("/tasks/{id:int}/done", new[] { HttpMethods.Patch }, (int id, InMemoryTaskStore store) =>
{
    var task = store.MarkDone(id);

    return task is null
        ? Results.NotFound(new ErrorResponse($"Task {id} was not found."))
        : Results.NoContent();
});
```

Green. The store replaces the item with a copy whose `IsDone` is true; the records are immutable, so `with` creates the updated value.

## How it works

`MapMethods` lets you bind a route to an HTTP method that has no dedicated `Map...` helper. In .NET 6, `MapPatch` is not available, so `MapMethods` with `HttpMethods.Patch` is the explicit form.

`204 NoContent` says the operation succeeded and there is no body to read. A `200` with the updated task would also be valid; the choice belongs in the API contract and the test pins it.

The `{id:int}` constraint keeps the route from matching `/tasks/abc`. The PATCH route adds a second segment, `/done`, so the path is `/tasks/{id}/done`.

## Repeat for new requirements

### A named error shape

Replace the anonymous error object with a record:

```csharp
public sealed record ErrorResponse(string Error);
```

Use it in the POST guard and the GET/PATCH 404 branches. Add the tests:

```csharp
[TestMethod]
public async Task UnknownTask_ReturnsAnErrorBody()
{
    using var api = new Api();

    var response = await api.Client.GetAsync("/tasks/99");

    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.IsNotNull(error);
    Assert.AreEqual("Task 99 was not found.", error.Error);
}

[TestMethod]
public async Task BlankTitle_ReturnsAnErrorBody()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest(" "));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.IsNotNull(error);
    Assert.AreEqual("Title is required.", error.Error);
}
```

Green. Every error the API produces now has the same shape. A client can parse `error` without guessing whether the body is an object, a string or empty.

### JSON naming

Write the test:

```csharp
[TestMethod]
public async Task Response_UsesCamelCaseJson()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
    var json = await response.Content.ReadAsStringAsync();

    StringAssert.Contains(json, "\"isDone\"");
    StringAssert.Contains(json, "\"title\"");
}
```

It passes with the web defaults, but make the policy explicit:

```csharp
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
```

Green. The API's JSON contract no longer depends on a framework default that could change. `DefaultIgnoreCondition` keeps null properties out of responses, which matters once the task model grows optional fields.

## Refactor

There are now four wire types with different jobs:

- `CreateTaskRequest` is the input.
- `TaskItem` is the task as the API exposes it.
- `ErrorResponse` is the failure shape.
- The store is the storage boundary.

Keeping them separate means a change to storage does not leak into the JSON, and a change to the error shape does not require a new domain type. Version 4 adds a database behind the store; the endpoint signatures stay the same.

`ErrorResponse` is deliberately small. When validation arrives in version 6, it grows into a field-error shape or a Problem Details document. The point of this version is that the shape is named and tested.

## Wrapping up

What we have covered:

- A PATCH route with `MapMethods` and `HttpMethods.Patch`.
- `204 NoContent` and the immutable `with` update in the store.
- A named `ErrorResponse` used by every failure path.
- Explicit camelCase JSON and null handling.
- Request, response, error and storage types as separate boundaries.

Next: v3: configuration and the options pattern.

