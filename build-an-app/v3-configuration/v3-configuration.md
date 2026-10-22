# v3: Configuration and the Options Pattern

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v3-configuration)

Version 2 validated that a title was not blank. Version 3 adds a real rule: a maximum title length that comes from configuration. The handler reads a typed options object, and the tests override the value without editing a file.

The finished solution is in `build-an-app/v3-configuration`.

## Write the test first

```csharp
[TestMethod]
public async Task TitleLongerThanTheConfiguredLimit_ReturnsBadRequest()
{
    using var api = new Api(maxTitleLength: 10);

    var response = await api.Client.PostAsJsonAsync(
        "/tasks",
        new CreateTaskRequest(new string('a', 11)));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.IsNotNull(error);
    StringAssert.Contains(error.Error, "10");
}
```

The test driver accepts the limit and overrides configuration:

```csharp
public Api(int? maxTitleLength = null)
{
    var factory = new WebApplicationFactory<Program>();

    if (maxTitleLength.HasValue)
    {
        factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tasks:MaxTitleLength"] = maxTitleLength.Value.ToString()
                })));
    }

    _factory = factory;
    Client = _factory.CreateClient();
}
```

## Try to run the test

The API has no length rule yet, so the POST returns `201 Created` and the test fails with `Expected:<BadRequest>. Actual:<Created>`.

## Write enough code to make it pass

Add the options type:

```csharp
public sealed class TaskOptions
{
    public const string SectionName = "Tasks";

    public int MaxTitleLength { get; set; } = 120;
}
```

Bind it in `Program.cs`:

```csharp
builder.Services.Configure<TaskOptions>(
    builder.Configuration.GetSection(TaskOptions.SectionName));
```

and use it in the POST handler:

```csharp
app.MapPost("/tasks", (CreateTaskRequest request, InMemoryTaskStore store, IOptions<TaskOptions> options) =>
{
    var maxTitleLength = options.Value.MaxTitleLength;

    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new ErrorResponse("Title is required."));
    }

    var title = request.Title.Trim();

    if (title.Length > maxTitleLength)
    {
        return Results.BadRequest(new ErrorResponse($"Title must be at most {maxTitleLength} characters."));
    }

    var task = store.Add(title);

    return Results.Created($"/tasks/{task.Id}", task);
});
```

Green. The 11-character title is rejected with the configured limit in the message.

## How it works

`appsettings.json` holds the default:

```json
{
  "Tasks": {
    "MaxTitleLength": 120
  }
}
```

`builder.Configuration.GetSection("Tasks")` selects that subtree. `Configure<TaskOptions>` binds its keys to the properties and registers `IOptions<TaskOptions>` in DI. The endpoint asks for `IOptions<TaskOptions>` and reads `Value`; it does not parse a string or reach into `IConfiguration`.

The test's `WithWebHostBuilder` creates a modified host for that test. `ConfigureAppConfiguration` adds an in-memory source after the app's files, so the test value wins. The production files stay untouched, and each test can choose its own limit.

`IOptions<T>` is a singleton snapshot read once. If the limit must change while the app is running, use `IOptionsMonitor<T>` or `IOptionsSnapshot<T>`. For a startup rule like this one, the singleton is the right default.

## Repeat for new requirements

### The boundary value

Write the test:

```csharp
[TestMethod]
public async Task TitleAtTheConfiguredLimit_Succeeds()
{
    using var api = new Api(maxTitleLength: 10);

    var response = await api.Client.PostAsJsonAsync(
        "/tasks",
        new CreateTaskRequest(new string('a', 10)));

    Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
}
```

It passes. The rule is "more than the limit fails", so exactly the limit is allowed. The test makes that boundary explicit.

### The default from appsettings

Write the test:

```csharp
[TestMethod]
public async Task DefaultLimit_ComesFromAppSettings()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync(
        "/tasks",
        new CreateTaskRequest(new string('a', 121)));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.IsNotNull(error);
    StringAssert.Contains(error.Error, "120");
}
```

It passes because the test host reads the app's `appsettings.json`. This is the test that proves the default is wired, not just the override mechanism.

## Refactor

The options type is small, but two habits keep configuration healthy:

- Validate the options at startup. `MaxTitleLength` must be positive; a zero or negative value would reject every title. Data annotations and `ValidateOnStart()` turn that into a startup failure instead of a strange 400 at request time.
- Keep configuration access at the composition root. A handler that reads `configuration["Tasks:MaxTitleLength"]` hides a dependency and is harder to test. The endpoint asks for the typed options it needs.

For environment-specific values, add `appsettings.Development.json` or use environment variables with the same keys. The in-memory test source is another configuration provider, which is why it overrides the file cleanly.

## Wrapping up

What we have covered:

- A typed options class bound from a configuration section.
- `IOptions<T>` injected into a minimal API handler.
- A test host that overrides configuration with `WithWebHostBuilder` and `AddInMemoryCollection`.
- Defaults from `appsettings.json` and a boundary test.
- Validation and lifetime choices for options.

Next: [v4: EF Core, a code-first model and a migration](../v4-ef-core/v4-ef-core.md).

