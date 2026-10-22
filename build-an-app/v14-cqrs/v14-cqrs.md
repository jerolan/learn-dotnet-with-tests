# v14: CQRS-ish with MediatR

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v14-cqrs)

The endpoints call readers and writers directly. Version 14 moves each use case into a command or query handler and lets MediatR dispatch it. The HTTP layer asks for an outcome; the handler owns the work.

The finished solution is in `build-an-app/v14-cqrs`.

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

Against v13 the command type does not exist. The test sends it through the mediator and then reads the task through the API, so it proves both the handler wiring and the shared storage.

## Add the packages

```sh
dotnet add src/TaskTracker.Api package MediatR --version 10.0.1
dotnet add src/TaskTracker.Api package MediatR.Extensions.Microsoft.DependencyInjection --version 10.0.1
```

## Commands and queries

```csharp
public sealed record GetTasksQuery : IRequest<IReadOnlyList<TaskItem>>;

public sealed record GetTaskQuery(int Id) : IRequest<TaskItem?>;

public sealed record CreateTaskCommand(string Title) : IRequest<TaskItem>;

public sealed record MarkTaskDoneCommand(int Id) : IRequest<bool>;
```

The records are the messages. A query returns data and changes nothing; a command changes state.

## Handlers

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

The other handlers follow the same shape: `GetTasksHandler` uses `ITaskReader.All`, `GetTaskHandler` uses `Find` and `MarkTaskDoneHandler` maps the writer result to a `bool`.

Register the assembly:

```csharp
builder.Services.AddMediatR(typeof(Program).Assembly);
```

MediatR scans for `IRequestHandler<,>` implementations and registers them. The handlers are scoped, like the reader and writer they depend on.

## The endpoints

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
```

The HTTP layer still owns status codes and validation. The handler owns the application operation.

## How it works

`IMediator.Send` finds the handler registered for the message type, resolves it from DI, calls `Handle` and returns its result. The endpoint never references `CreateTaskHandler` directly.

This is "CQRS-ish", not full CQRS. The read and write sides still share one SQLite database, one entity and one writer chain. There is no separate read store, no event sourcing and no eventual consistency. The separation is at the use-case level: a command handler and a query handler are easier to name and test than a long endpoint lambda.

The handler shape also gives you a place for cross-cutting behavior. A MediatR `IPipelineBehavior<TRequest, TResponse>` can run validation, logging or transactions around every command without editing each handler.

## Refactor

A few boundaries keep the mediator from becoming a tax:

- Use a message per use case, not per table. `CreateTaskCommand` describes intent; `UpdateTaskRow` describes implementation.
- Keep validation at the HTTP boundary for request shape, and add a pipeline behavior when the same rules must run for every caller.
- Do not hide simple reads behind the mediator just for symmetry. A single query with no cross-cutting behavior is fine as a reader call.
- Test handlers directly with fakes when the behavior is complex, and keep a few acceptance tests for the HTTP contract. This suite does both.
- A vertical slice groups the message, handler and endpoint by feature. The `TaskMessages.cs` file is a small version of that idea.

The mediator is a tool for dispatch and decoration, not a requirement for every method. The application works because the handlers and the endpoints agree on the messages, not because the mediator is present.

## Wrapping up

What we have covered:

- Command and query records with `IRequest<T>`.
- Handlers with `IRequestHandler<TRequest, TResponse>`.
- `AddMediatR` assembly scanning and scoped handler resolution.
- Endpoints that send messages and keep HTTP concerns at the edge.
- The difference between this use-case separation and full CQRS.
- Pipeline behaviors as the next extension point.

That completes Part 3. Part 4 covers testing Q&A and the good-to-know appendices.

