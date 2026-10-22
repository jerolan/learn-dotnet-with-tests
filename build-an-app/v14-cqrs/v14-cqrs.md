# v14: CQRS-ish with MediatR

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v14-cqrs)

The endpoints call readers and writers directly. Version 14 moves each use case into a command or query handler and lets MediatR dispatch it. The HTTP layer asks for an outcome; the handler owns the work.

The finished solution is in `build-an-app/v14-cqrs`.

## How it works

`IMediator.Send` finds the handler registered for a message, resolves it from DI, calls `Handle` and returns its result. The endpoint or test names a command or query; neither needs to reference the handler class directly.

The separation is at the use-case level. A command handler changes state; a query handler returns data without changing it.

## How to test

Send a command through `IMediator`, then check the result and read through the HTTP API. That crosses the mediator boundary and confirms the handler shares the application's store. Existing HTTP acceptance tests continue to protect status codes, validation and routes as the other endpoints move to handlers.

## Write the test first

```csharp
[TestMethod]
public async Task CreateTaskCommand_IsHandledThroughMediatR()
{
    using var api = new Api();
    using var scope = api.Services.CreateScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    var task = await mediator.Send(new CreateTaskCommand("From mediator"));

    Assert.AreEqual("From mediator", task.Title);

    var tasks = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    Assert.IsNotNull(tasks);
    Assert.IsTrue(tasks.Any(item => item.Id == task.Id));
}
```

The command goes through MediatR, then the test reads the task through HTTP. It checks that dispatch reaches the handler and that the handler writes to the same store used by the API.

## Try to run the test

The test does not compile yet because MediatR and `CreateTaskCommand` have not been added.

## Write the minimal amount of code for the test to run and check the failing test output

Install MediatR and add the message used by the test:

```sh
dotnet add src/TaskTracker.Api package MediatR --version 10.0.1
dotnet add src/TaskTracker.Api package MediatR.Extensions.Microsoft.DependencyInjection --version 10.0.1
```

```csharp
public sealed record CreateTaskCommand(string Title) : IRequest<TaskItem>;
```

The test now compiles, but it cannot resolve `IMediator` because no handlers have been registered.

## Write enough code to make it pass

Add the handler and scan the API assembly so MediatR registers it:

```csharp
public sealed class CreateTaskHandler : IRequestHandler<CreateTaskCommand, TaskItem>
{
    private readonly ITaskWriter _writer;

    public CreateTaskHandler(ITaskWriter writer)
    {
        _writer = writer;
    }

    public Task<TaskItem> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_writer.Add(request.Title));
    }
}
```

```csharp
builder.Services.AddMediatR(typeof(Program).Assembly);
```

The test passes. The handler writes through `ITaskWriter`, and `/tasks` reads the same task from the shared store.

## Refactor

This is “CQRS-ish,” not full CQRS. Reads and writes still share one SQLite database, one entity and one writer chain. There is no separate read store, event sourcing or eventual consistency.

A few boundaries keep the mediator from becoming a tax:

- Use a message per use case, not per table. `CreateTaskCommand` describes intent; `UpdateTaskRow` describes implementation.
- Keep validation at the HTTP boundary for request shape, and add a pipeline behavior when the same rules must run for every caller.
- Do not hide simple reads behind the mediator just for symmetry. A query with no cross-cutting behavior can remain a reader call.
- Test handlers directly with fakes when their behavior is complex. This suite verifies the create path through MediatR and uses HTTP acceptance tests for the API behavior.
- A vertical slice groups the message, handler and endpoint by feature. `TaskMessages.cs` is a small version of that idea.

The mediator dispatches and decorates work; it is not required for every method. Pipeline behaviors can add validation, logging or transactions around handlers without editing each one.

## Repeat for new requirements

The v13 acceptance tests already cover listing, fetching, creating and marking tasks done. Keep those behaviors green while moving the remaining operations behind messages.

Add query and command records:

```csharp
public sealed record GetTasksQuery : IRequest<IReadOnlyList<TaskItem>>;

public sealed record GetTaskQuery(int Id) : IRequest<TaskItem?>;

public sealed record MarkTaskDoneCommand(int Id) : IRequest<bool>;
```

The handlers follow the same shape. `GetTasksHandler` calls `ITaskReader.All`, `GetTaskHandler` calls `Find`, and `MarkTaskDoneHandler` maps the writer result to a `bool`.

Change the endpoints to send those messages:

```csharp
app.MapGet("/tasks", async (IMediator mediator) =>
    await mediator.Send(new GetTasksQuery())).RequireAuthorization();

app.MapGet("/tasks/{id:int}", async (int id, IMediator mediator) =>
{
    var task = await mediator.Send(new GetTaskQuery(id));

    return task is null
        ? Results.NotFound(new ErrorResponse($"Task {id} was not found."))
        : Results.Ok(task);
}).RequireAuthorization();

app.MapPost("/tasks", async (
    CreateTaskRequest request,
    IValidator<CreateTaskRequest> validator,
    IMediator mediator,
    ILogger<Program> logger) =>
{
    // validation ...

    var task = await mediator.Send(new CreateTaskCommand(request.Title.Trim()));

    logger.LogInformation(
        "Task {TaskId} created with title {Title}",
        task.Id,
        task.Title);

    return Results.Created($"/tasks/{task.Id}", task);
}).RequireAuthorization();

app.MapMethods("/tasks/{id:int}/done", new[] { HttpMethods.Patch }, async (int id, IMediator mediator) =>
{
    var done = await mediator.Send(new MarkTaskDoneCommand(id));

    return done
        ? Results.NoContent()
        : Results.NotFound(new ErrorResponse($"Task {id} was not found."));
}).RequireAuthorization();
```

The HTTP layer keeps status codes and validation at the edge; handlers own the application operations. The existing acceptance tests stay green after the move.

## Wrapping up

What we have covered:

- Command and query records with `IRequest<T>`.
- Handlers with `IRequestHandler<TRequest, TResponse>`.
- `AddMediatR` assembly scanning and scoped handler resolution.
- Endpoints that send messages and keep HTTP concerns at the edge.
- The difference between this use-case separation and full CQRS.
- Pipeline behaviors as the next extension point.

That completes Part 3. Part 4 covers testing Q&A and the good-to-know appendices.
