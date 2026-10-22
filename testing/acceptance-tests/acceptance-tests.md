# Acceptance Tests

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/testing/acceptance-tests)

An acceptance test checks a feature through the same boundary a user uses. For an HTTP API, that means sending requests and reading responses, not calling a service method directly. The test answers "what does winning look like?" before you decide how the code is organized inside.

We will build a small Todo API and test it with `WebApplicationFactory`. The finished solution is in `testing/acceptance-tests`; if you are building it from scratch, create a web project in `src/TodoApi`, an MSTest project in `tests/TodoApi.Tests`, a solution, a project reference from the tests to the app, and the test host package:

```sh
dotnet new web -n TodoApi -o src/TodoApi
dotnet new mstest -n TodoApi.Tests -o tests/TodoApi.Tests
dotnet add tests/TodoApi.Tests/TodoApi.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 6.0.36
dotnet add tests/TodoApi.Tests/TodoApi.Tests.csproj reference src/TodoApi/TodoApi.csproj
dotnet new sln -n AcceptanceTests
dotnet sln add src/TodoApi/TodoApi.csproj tests/TodoApi.Tests/TodoApi.Tests.csproj
```

## Write the test first

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TodoApi.Tests;

[TestClass]
public sealed class TodoApiTests
{
    [TestMethod]
    public async Task CanCreateAndListTodos()
    {
        using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/todos", new Todo(1, "Write tests"));
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");

        Assert.IsNotNull(todos);
        Assert.AreEqual(1, todos.Count);
        Assert.AreEqual("Write tests", todos[0].Title);
    }
}
```

The test speaks HTTP. It does not know whether the endpoint uses a list, a repository or a database.

## Try to run the test

The app starts with no endpoints:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Run();

public sealed record Todo(int Id, string Title);

public partial class Program
{
}
```

The test compiles and fails with `Assert.AreEqual failed. Expected:<Created>. Actual:<NotFound>`. The feature does not exist yet.

The `public partial class Program` declaration is what lets the test project use `WebApplicationFactory<Program>`. Top-level statements generate an internal `Program`; the partial declaration makes it visible without adding a `Main`.

## Write enough code to make it pass

Add the two endpoints:

```csharp
var todos = new List<Todo>();

app.MapGet("/todos", () => todos);

app.MapPost("/todos", (Todo todo) =>
{
    todos.Add(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.Run();
```

Green. The POST returns `201 Created` with the todo, and the GET returns the list as JSON. The test passed through routing, model binding, JSON serialization and the endpoint logic.

## How it works

`WebApplicationFactory<Program>` starts the application in memory. `CreateClient()` gives you an `HttpClient` whose requests go to the in-memory server, not to a port on your machine.

Under the factory is `TestServer`, the in-memory server that actually handles the requests. You rarely construct `TestServer` directly; the factory configures it with the app's startup.

The factory uses the app's real startup path: the same middleware, routing and JSON settings as production. That is what makes this an acceptance test rather than a unit test. You can refactor the endpoint into a service, a repository or a database and the test keeps passing as long as the HTTP behavior stays the same.

The test host package must match the target framework. A `net6.0` app uses `Microsoft.AspNetCore.Mvc.Testing` `6.0.x`. Mixing a newer test host with an older runtime is a common source of startup errors.

## Repeat for new requirements

### An empty list

Write the test:

```csharp
[TestMethod]
public async Task EmptyList_ReturnsNoTodos()
{
    using var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();

    var todos = await client.GetFromJsonAsync<List<Todo>>("/todos");

    Assert.IsNotNull(todos);
    Assert.AreEqual(0, todos.Count);
}
```

It passes. Each test creates its own factory, which creates its own app and its own `todos` list. If the tests shared one factory, the todo from the first test would leak into the second.

## Refactor

There is no production code to clean up yet. The important boundary is the test itself.

Acceptance tests are slower than unit tests because they start the application and go through the HTTP stack. Keep them for the critical paths a user cares about: create a todo, list todos, reject invalid input. Test the details of a calculation or a branch at the unit level.

An acceptance test should describe behavior, not implementation. `CanCreateAndListTodos` survives a change from a list to a database. A test that asserts the endpoint called a repository method would break on that refactor, and it would be testing the inside rather than the feature.

The factory can also replace services for integration scenarios: a test database, a fake email sender, a clock. That is the seam to use when the feature needs infrastructure. Part 3 uses it for database-backed tests.

## Wrapping up

What we have covered:

- An acceptance test through HTTP with `WebApplicationFactory<Program>`.
- Starting the real app in memory and creating an `HttpClient`.
- `PostAsJsonAsync`, `GetFromJsonAsync` and status codes.
- The `public partial class Program` declaration the test host needs.
- Per-test factories for isolated application state.
- The difference between an acceptance test and a unit test.

The chapter follows the [Introduction to acceptance tests in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/intro-to-acceptance-tests): write a test for the feature a user can see, then build inward until it passes. .NET provides the in-memory host through `WebApplicationFactory`.

Next: [Scaling acceptance tests](../scaling-acceptance-tests/scaling-acceptance-tests.md).

