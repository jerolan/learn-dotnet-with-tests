# v14: CQRS-ish con MediatR

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v14-cqrs)

Los endpoints llaman a los lectores y escritores directamente. La versión 14 mueve cada caso de uso a un handler de comando o query y deja que MediatR lo despache. La capa HTTP pide un resultado; el handler es dueño del trabajo.

La solución terminada está en `build-an-app/v14-cqrs`.

## Escribe primero el test

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

Contra v13 el tipo del comando no existe. El test lo manda por el mediador y después lee la tarea por la API, así prueba el cableado del handler y el almacenamiento compartido.

## Agrega los paquetes

```sh
dotnet add src/TaskTracker.Api package MediatR --version 10.0.1
dotnet add src/TaskTracker.Api package MediatR.Extensions.Microsoft.DependencyInjection --version 10.0.1
```

## Comandos y queries

```csharp
public sealed record GetTasksQuery : IRequest<IReadOnlyList<TaskItem>>;

public sealed record GetTaskQuery(int Id) : IRequest<TaskItem?>;

public sealed record CreateTaskCommand(string Title) : IRequest<TaskItem>;

public sealed record MarkTaskDoneCommand(int Id) : IRequest<bool>;
```

Los records son los mensajes. Una query devuelve datos y no cambia nada; un comando cambia estado.

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

Los otros handlers siguen la misma forma: `GetTasksHandler` usa `ITaskReader.All`, `GetTaskHandler` usa `Find` y `MarkTaskDoneHandler` mapea el resultado del writer a un `bool`.

Registra el ensamblado:

```csharp
builder.Services.AddMediatR(typeof(Program).Assembly);
```

MediatR busca las implementaciones de `IRequestHandler<,>` y las registra. Los handlers son scoped, como el lector y el escritor de los que dependen.

## Los endpoints

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
    // validación ...

    var task = await mediator.Send(new CreateTaskCommand(request.Title.Trim()));

    logger.LogInformation(
        "Task {TaskId} created with title {Title}",
        task.Id,
        task.Title);

    return Results.Created($"/tasks/{task.Id}", task);
}).RequireAuthorization();
```

La capa HTTP sigue siendo dueña de los códigos de estado y la validación. El handler es dueño de la operación de aplicación.

## Cómo funciona

`IMediator.Send` busca el handler registrado para el tipo de mensaje, lo resuelve de DI, llama a `Handle` y devuelve el resultado. El endpoint nunca referencia `CreateTaskHandler` directamente.

Esto es "CQRS-ish", no CQRS completo. Los lados de lectura y escritura comparten una base SQLite, una entidad y una cadena de escritura. No hay un almacén de lectura separado, ni event sourcing, ni consistencia eventual. La separación es a nivel de caso de uso: un handler de comando y uno de query son más fáciles de nombrar y probar que una lambda de endpoint larga.

La forma del handler también da un lugar para comportamiento transversal. Un `IPipelineBehavior<TRequest, TResponse>` de MediatR puede correr validación, logging o transacciones alrededor de cada comando sin editar cada handler.

## Refactor

Algunos bordes evitan que el mediador se vuelva un impuesto:

- Un mensaje por caso de uso, no por tabla. `CreateTaskCommand` describe intención; `UpdateTaskRow` describe implementación.
- Mantén la validación en el borde HTTP para la forma del request, y agrega un pipeline behavior cuando las mismas reglas deban correr para todos los llamadores.
- No escondas lecturas simples detrás del mediador solo por simetría. Una query sin comportamiento transversal puede ser una llamada al lector.
- Prueba los handlers directamente con fakes cuando la lógica sea compleja, y conserva unos pocos tests de aceptación para el contrato HTTP. Esta suite hace las dos cosas.
- Una vertical slice agrupa mensaje, handler y endpoint por característica. `TaskMessages.cs` es una versión pequeña de esa idea.

El mediador es una herramienta de despacho y decoración, no un requisito para cada método. La aplicación funciona porque los handlers y los endpoints coinciden en los mensajes, no porque el mediador esté presente.

## Para cerrar

Lo que cubrimos:

- Records de comando y query con `IRequest<T>`.
- Handlers con `IRequestHandler<TRequest, TResponse>`.
- `AddMediatR` escaneando el ensamblado y resolviendo handlers scoped.
- Endpoints que mandan mensajes y mantienen las preocupaciones HTTP en el borde.
- La diferencia entre esta separación por caso de uso y CQRS completo.
- Pipeline behaviors como siguiente punto de extensión.

Con esto termina la Parte 3. La Parte 4 cubre preguntas de testing y los apéndices good-to-know.

