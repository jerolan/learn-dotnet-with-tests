# v14: CQRS-ish con MediatR

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v14-cqrs)

Los endpoints llaman a los lectores y escritores directamente. La versión 14 mueve cada caso de uso a un handler de comando o query y deja que MediatR lo despache. La capa HTTP pide un resultado; el handler es dueño del trabajo.

La solución terminada está en `build-an-app/v14-cqrs`.

## Cómo funciona

`IMediator.Send` busca el handler registrado para un mensaje, lo resuelve de DI, llama a `Handle` y devuelve el resultado. El endpoint o el test nombra un comando o una query; ninguno necesita referenciar directamente la clase handler.

La separación ocurre a nivel de caso de uso. Un handler de comando cambia el estado; uno de query devuelve datos sin cambiarlos.

## Cómo probarlo

Envía un comando por `IMediator`, comprueba el resultado y después lee por la API HTTP. Así cruzas el límite del mediador y confirmas que el handler comparte el almacén de la aplicación. Los tests de aceptación HTTP existentes siguen protegiendo códigos de estado, validación y rutas mientras se migran los demás endpoints.

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

The command goes through MediatR, then the test reads the task through HTTP. It checks that dispatch reaches the handler and that the handler writes to the same store used by the API.

## Intenta ejecutar el test

El test todavía no compila porque aún no se agregaron MediatR ni `CreateTaskCommand`.

## Escribe el código mínimo para que el test compile y mira la salida

Instala MediatR y agrega el mensaje que usa el test:

```sh
dotnet add src/TaskTracker.Api package MediatR --version 10.0.1
dotnet add src/TaskTracker.Api package MediatR.Extensions.Microsoft.DependencyInjection --version 10.0.1
```

```csharp
public sealed record CreateTaskCommand(string Title) : IRequest<TaskItem>;
```

El test ya compila, pero no puede resolver `IMediator` porque aún no se registraron handlers.

## Escribe el código suficiente para que pase

Agrega el handler y escanea el ensamblado de la API para que MediatR lo registre:

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

El test pasa. El handler escribe mediante `ITaskWriter` y `/tasks` lee la misma tarea desde el almacén compartido.

## Refactor

Esto es “CQRS-ish”, no CQRS completo. Las lecturas y escrituras todavía comparten una base SQLite, una entidad y una cadena de escritura. No hay un almacén de lectura separado, event sourcing ni consistencia eventual.

Algunos límites evitan que el mediador se vuelva un impuesto:

- Un mensaje por caso de uso, no por tabla. `CreateTaskCommand` describe intención; `UpdateTaskRow` describe implementación.
- Mantén la validación en el borde HTTP para la forma del request y agrega un pipeline behavior cuando las mismas reglas deban correr para todos los llamadores.
- No escondas lecturas simples detrás del mediador solo por simetría. Una query sin comportamiento transversal puede seguir siendo una llamada al lector.
- Prueba los handlers directamente con fakes cuando su comportamiento sea complejo. Esta suite verifica la creación mediante MediatR y usa tests de aceptación HTTP para el comportamiento de la API.
- Una vertical slice agrupa mensaje, handler y endpoint por característica. `TaskMessages.cs` es una versión pequeña de esa idea.

El mediador despacha y decora el trabajo; no se requiere para cada método. Los pipeline behaviors pueden agregar validación, logging o transacciones alrededor de los handlers sin editar cada uno.

## Repite con requisitos nuevos

Los tests de aceptación de v13 ya cubren listar, buscar, crear y marcar tareas como hechas. Mantén esos comportamientos en verde mientras mueves las operaciones restantes detrás de mensajes.

Agrega records de query y comando:

```csharp
public sealed record GetTasksQuery : IRequest<IReadOnlyList<TaskItem>>;

public sealed record GetTaskQuery(int Id) : IRequest<TaskItem?>;

public sealed record MarkTaskDoneCommand(int Id) : IRequest<bool>;
```

Los handlers siguen la misma forma. `GetTasksHandler` llama a `ITaskReader.All`, `GetTaskHandler` llama a `Find` y `MarkTaskDoneHandler` convierte el resultado del writer en un `bool`.

Cambia los endpoints para enviar esos mensajes:

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

app.MapMethods("/tasks/{id:int}/done", new[] { HttpMethods.Patch }, async (int id, IMediator mediator) =>
{
    var done = await mediator.Send(new MarkTaskDoneCommand(id));

    return done
        ? Results.NoContent()
        : Results.NotFound(new ErrorResponse($"Task {id} was not found."));
}).RequireAuthorization();
```

La capa HTTP conserva los códigos de estado y la validación en el borde; los handlers son dueños de las operaciones de aplicación. Los tests de aceptación existentes siguen en verde después del cambio.

## Para cerrar

Lo que cubrimos:

- Records de comando y query con `IRequest<T>`.
- Handlers con `IRequestHandler<TRequest, TResponse>`.
- `AddMediatR` escaneando el ensamblado y resolviendo handlers scoped.
- Endpoints que mandan mensajes y mantienen las preocupaciones HTTP en el borde.
- La diferencia entre esta separación por caso de uso y CQRS completo.
- Pipeline behaviors como siguiente punto de extensión.

Con esto termina la Parte 3. La Parte 4 cubre preguntas de testing y los apéndices good-to-know.
