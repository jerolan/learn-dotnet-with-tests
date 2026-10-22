# v9: Structured Logging

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v9-logging)

A log line that says "task created" is hard to query. A log event with a `TaskId` and a `Title` field is data. Version 9 adds structured events, a correlation scope and a test logger that captures the fields instead of matching a formatted string.

The finished solution is in `build-an-app/v9-logging`.

## How it works

The logging pipeline has three pieces: the call site, an `ILogger<T>` category and a provider. The category is usually the type that owns the log, so `ILogger<Program>` for the endpoint and `ILogger<CorrelationIdMiddleware>` for the middleware.

A message template uses braces for named fields. The provider receives both the template and the values. Console and JSON providers render them; a test provider can inspect them; an OpenTelemetry exporter can attach them to a span.

Log levels narrow the output. `Information` is for normal business events, `Warning` for something unexpected but handled, `Error` for a failure. Avoid logging every method entry; log the decisions and the boundaries.

## How to test

Install a recording provider through `WebApplicationFactory` and inspect its key/value fields instead of comparing the rendered message:

```csharp
builder.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddProvider(_logs);
    logging.SetMinimumLevel(LogLevel.Information);
});
```

## Write the test first

```csharp
[TestMethod]
public async Task CreateTask_LogsTaskIdAndTitle()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync(
        "/tasks",
        new CreateTaskRequest("Write tests"));

    var task = await response.Content.ReadFromJsonAsync<TaskItem>();
    var record = api.Logs.Single(entry => entry.Message.Contains("created"));

    Assert.AreEqual(task!.Id, record.Fields["TaskId"]);
    Assert.AreEqual("Write tests", record.Fields["Title"]);
}
```

The assertions read named fields from the record rather than comparing a formatted message.

## Try to run the test

Run it against v8. The create request succeeds, but no `created` record exists, so the test cannot find a log event.

## Write the minimal amount of code for the test to run and check the failing test output

Add a structured event with the task id:

```csharp
logger.LogInformation("Task {TaskId} created", task.Id);
```

The event now has `TaskId`, but the test still fails because it also expects `Title`.

## Write enough code to make it pass

Add the title as a named field:

```csharp
logger.LogInformation(
    "Task {TaskId} created with title {Title}",
    task.Id,
    task.Title);
```

Green. The message template names the fields; the arguments fill them. A provider can store `TaskId=1` and `Title=Write tests` without parsing the rendered text.

Do not build the message with interpolation:

```csharp
logger.LogInformation($"Task {task.Id} created"); // loses the fields
```

The interpolated string is a single opaque message. The template version keeps the names and values separate.

## Refactor

A few log habits keep the output useful:

- Log at the boundary: request received, task created, dependency failed. Skip the internal loop.
- Use stable templates. Changing `Task {TaskId} created` breaks dashboards that query the event; add a new event instead of renaming an old one.
- Never log secrets, tokens, passwords or full request bodies. Log identifiers and outcomes.
- Put shared context in scopes instead of repeating it in every message.
- Keep the provider configuration in the composition root; the domain code asks for `ILogger<T>` and does not know where the logs go.

The recording provider in the test project is a small example of a provider. A production provider writes JSON, ships to a collector or both; the call site does not change.

## Repeat for new requirements

### A correlation id

Write the test:

```csharp
[TestMethod]
public async Task CorrelationId_FlowsIntoTheResponseAndTheScope()
{
    using var api = new Api();
    var request = new HttpRequestMessage(HttpMethod.Get, "/tasks");
    request.Headers.Add("X-Correlation-Id", "test-123");

    var response = await api.Client.SendAsync(request);

    Assert.AreEqual("test-123", response.Headers.GetValues("X-Correlation-Id").Single());

    var record = api.Logs.Last(entry => entry.Message.Contains("Handled"));
    Assert.AreEqual("test-123", record.Fields["CorrelationId"]);
}
```

Add the middleware:

```csharp
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var value)
            && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            logger.LogInformation(
                "Handled {Method} {Path}",
                context.Request.Method,
                context.Request.Path.Value);

            await _next(context);
        }
    }
}
```

Register it:

```csharp
app.UseMiddleware<CorrelationIdMiddleware>();
```

Green. The response carries the id, and the log event inside the scope carries `CorrelationId` as a field.

The scope applies to every log written through that logger for the duration of the request. In a larger app, the correlation id also goes into outbound HTTP headers and message metadata, so one id follows a request across services.

## Wrapping up

What we have covered:

- Structured message templates with named fields.
- `ILogger<T>` categories and the logging pipeline.
- A recording provider that captures fields in tests.
- A correlation id header and a logging scope.
- Log levels and the boundary logging habit.
- Avoiding interpolation, secrets and noisy per-line logs.

Next: [v10: caching with IMemoryCache](../v10-caching/v10-caching.md).

