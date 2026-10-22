# v3: Configuration and the Options Pattern

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v3-configuration)

Version 2 validated that a title was not blank. Version 3 adds a real rule: a maximum title length that comes from configuration. The handler reads a typed options object, and the tests override the value without editing a file.

The finished solution is in `build-an-app/v3-configuration`.

## How it works

`appsettings.json` supplies the default limit. `Configure<TaskOptions>` binds the `Tasks` section and makes the typed options available through `IOptions<TaskOptions>`. A test can add an in-memory configuration source after the app's files to override that value.

```json
{
  "Tasks": {
    "MaxTitleLength": 120
  }
}
```

`builder.Configuration.GetSection("Tasks")` selects that subtree. The endpoint reads `IOptions<TaskOptions>.Value`; it does not parse strings or reach into `IConfiguration`.

`IOptions<T>` is a singleton snapshot read once. If a value must change while the app is running, use `IOptionsMonitor<T>` or `IOptionsSnapshot<T>`; for this startup rule, the singleton is the right default.

## How to test

Send a title through the HTTP API and assert the status and error body. The test driver can override the limit for one host without editing production files:

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

`ConfigureAppConfiguration` adds the in-memory source after the app's files, so the test value wins and production files remain untouched.

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

## Try to run the test

The API has no length rule yet, so the POST returns `201 Created` and the test fails with `Expected:<BadRequest>. Actual:<Created>`.

## Write the minimal amount of code for the test to run and check the failing test output

Add and bind the options type:

```csharp
public sealed class TaskOptions
{
    public const string SectionName = "Tasks";

    public int MaxTitleLength { get; set; } = 120;
}
```

```csharp
builder.Services.Configure<TaskOptions>(
    builder.Configuration.GetSection(TaskOptions.SectionName));
```

The app still accepts the 11-character title. The new setting is registered, but the handler does not use it yet.

## Write enough code to make it pass

Use the typed options in the POST handler:

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

## Refactor

The options type is small, but two habits keep configuration healthy:

- Validate the options at startup. `MaxTitleLength` must be positive; a zero or negative value would reject every title. Data annotations and `ValidateOnStart()` turn that into a startup failure instead of a strange 400 at request time.
- Keep configuration access at the composition root. A handler that reads `configuration["Tasks:MaxTitleLength"]` hides a dependency and is harder to test. The endpoint asks for the typed options it needs.

For environment-specific values, add `appsettings.Development.json` or use environment variables with the same keys. The in-memory test source is another configuration provider, which is why it overrides the file cleanly.

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

## Wrapping up

What we have covered:

- A typed options class bound from a configuration section.
- `IOptions<T>` injected into a minimal API handler.
- A test host that overrides configuration with `WithWebHostBuilder` and `AddInMemoryCollection`.
- Defaults from `appsettings.json` and a boundary test.
- Validation and lifetime choices for options.

Next: [v4: EF Core, a code-first model and a migration](../v4-ef-core/v4-ef-core.md).

