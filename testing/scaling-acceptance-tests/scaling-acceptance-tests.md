# Scaling Acceptance Tests

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/testing/scaling-acceptance-tests)

One acceptance test is easy to read. Twenty of them, each with its own HTTP setup and JSON plumbing, become slow and repetitive. Scaling acceptance tests is mostly about keeping the scenarios focused and moving the mechanical parts into a small driver.

We will grow the Todo API with validation and duplicate detection, and introduce a test driver so each test states one behavior. The finished solution is in `testing/scaling-acceptance-tests`.

## Write the test first

```csharp
[TestMethod]
public async Task RejectsATodoWithoutATitle()
{
    using var api = new Api();

    var response = await api.Client.CreateAsync(new Todo(1, " "));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
}
```

## Try to run the test

The app currently accepts any todo:

```csharp
app.MapPost("/todos", (Todo todo) =>
{
    todos.Add(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
});
```

The test fails with `Expected:<BadRequest>. Actual:<Created>`.

## Write enough code to make it pass

```csharp
app.MapPost("/todos", (Todo todo) =>
{
    if (string.IsNullOrWhiteSpace(todo.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }

    todos.Add(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
});
```

Green. The endpoint now rejects a blank title with a 400 and a small JSON error.

## How it works

The test uses a driver:

```csharp
private sealed class Api : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();

    public Api()
    {
        Client = new TodoClient(_factory.CreateClient());
    }

    public TodoClient Client { get; }

    public void Dispose()
    {
        _factory.Dispose();
    }
}

private sealed class TodoClient
{
    private readonly HttpClient _client;

    public TodoClient(HttpClient client)
    {
        _client = client;
    }

    public Task<HttpResponseMessage> CreateAsync(Todo todo)
    {
        return _client.PostAsJsonAsync("/todos", todo);
    }

    public async Task<List<Todo>> ListAsync()
    {
        var todos = await _client.GetFromJsonAsync<List<Todo>>("/todos");
        return todos!;
    }
}
```

The driver is test code, not production code. It hides the factory, the client and the JSON calls, so a test reads as "create this todo and expect this status". When the API surface changes, the driver is the one place to update.

Each `Api` creates its own factory, so each test gets a fresh list. That isolation is worth the extra startup cost; a shared factory would let one test's todos leak into the next.

## Repeat for new requirements

### Creation order

Write the test:

```csharp
[TestMethod]
public async Task ListsTodosInCreationOrder()
{
    using var api = new Api();

    await api.Client.CreateAsync(new Todo(1, "First"));
    await api.Client.CreateAsync(new Todo(2, "Second"));

    var todos = await api.Client.ListAsync();

    Assert.AreEqual(2, todos.Count);
    Assert.AreEqual("First", todos[0].Title);
    Assert.AreEqual("Second", todos[1].Title);
}
```

It passes because the endpoint appends to a list. The test pins the order as part of the feature, so a later switch to a dictionary or an unordered store cannot change it silently.

### Duplicate ids

Write the test:

```csharp
[TestMethod]
public async Task RejectsADuplicateId()
{
    using var api = new Api();

    await api.Client.CreateAsync(new Todo(1, "First"));
    var response = await api.Client.CreateAsync(new Todo(1, "Duplicate"));

    Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
}
```

It fails with `Expected:<Conflict>. Actual:<Created>`. Add the guard:

```csharp
if (todos.Any(existing => existing.Id == todo.Id))
{
    return Results.Conflict(new { error = "A todo with that id already exists." });
}
```

Green. The test uses the driver twice for setup and the third call for the behavior under test.

## Refactor

The driver already keeps the tests short. These habits keep a suite of acceptance tests manageable:

- One behavior per test. The name should say what the user gets, not which endpoint was called.
- Build data with small helpers. `new Todo(1, "First")` is fine here; a larger object gets a builder so the test only names the fields that matter.
- Isolate state. A factory per test is the simplest rule. If startup becomes too slow, share a fixture and reset the data between tests; do not let tests depend on order.
- Keep the critical paths at this level. Validation, ordering and conflicts are user-visible. The exact formatting of an error message or the branch inside a helper belongs in unit tests.
- Do not assert implementation details. The test should survive replacing the list with a repository.

MSTest has `[ClassInitialize]` and `[AssemblyInitialize]` for shared setup. Use them when the cost of starting the app dominates, and add a reset step so each test still starts from a known state. The Todo API starts fast enough that per-test isolation is the better default.

## Wrapping up

What we have covered:

- A test driver that wraps the HTTP calls.
- Per-test factories for isolated application state.
- Acceptance tests for validation, ordering and duplicate ids.
- The trade-off between a shared fixture and per-test isolation.
- Keeping acceptance tests at the user-visible level and unit tests for details.

The chapter follows [Scaling acceptance tests in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/scaling-acceptance-tests): the test suite grows without turning every scenario into a copy of the setup. The driver and the factory are the .NET version of that idea.

Next: [Working without mocks](../working-without-mocks/working-without-mocks.md).

