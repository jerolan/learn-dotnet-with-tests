# v1: La API y sus primeros tests

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v1-intro)

Esta parte construye una aplicación: `TaskTracker`, una API de tareas. Cada versión agrega una preocupación y sigue siendo ejecutable por sí sola. La versión 1 es la API útil más pequeña: crear una tarea, listar tareas y buscar una por id.

La solución terminada está en `build-an-app/v1-intro`. Ejecútala con `dotnet run --project src/TaskTracker.Api` y pruébala con `dotnet test`.

## Escribe primero el test

Crea el proyecto web y el proyecto de tests, conecta el paquete del host de tests y la referencia, y agrega el primer test de aceptación:

```csharp
[TestClass]
public sealed class TaskApiTests
{
    [TestMethod]
    public async Task CanCreateAndGetATask()
    {
        using var api = new Api();

        var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<TaskItem>();
        Assert.IsNotNull(created);
        Assert.AreEqual(1, created.Id);
        Assert.AreEqual("Write tests", created.Title);
        Assert.IsFalse(created.IsDone);

        var fetched = await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{created.Id}");

        Assert.IsNotNull(fetched);
        Assert.AreEqual(created, fetched);
    }
}
```

El test maneja la API pública. No sabe cómo se guardan las tareas.

## Intenta ejecutar el test

La app arranca sin endpoints:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Run();

public sealed record CreateTaskRequest(string Title);

public sealed record TaskItem(int Id, string Title, bool IsDone);

public partial class Program
{
}
```

El test falla con `Expected:<Created>. Actual:<NotFound>`. La ruta no existe.

## Escribe el código mínimo para que el test compile y mira la salida

Agrega un store y regístralo:

```csharp
public sealed class InMemoryTaskStore
{
    private readonly List<TaskItem> _tasks = new();
    private int _nextId = 1;

    public IReadOnlyList<TaskItem> All()
    {
        return _tasks;
    }

    public TaskItem? Find(int id)
    {
        return _tasks.FirstOrDefault(task => task.Id == id);
    }

    public TaskItem Add(string title)
    {
        var task = new TaskItem(_nextId++, title, false);
        _tasks.Add(task);
        return task;
    }
}
```

y:

```csharp
builder.Services.AddSingleton<InMemoryTaskStore>();
```

Conecta el endpoint POST:

```csharp
app.MapPost("/tasks", (CreateTaskRequest request, InMemoryTaskStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }

    var task = store.Add(request.Title.Trim());

    return Results.Created($"/tasks/{task.Id}", task);
});
```

El POST ahora funciona, pero el GET del mismo test falla con `NotFound`.

## Escribe el código suficiente para que pase

```csharp
app.MapGet("/tasks/{id:int}", (int id, InMemoryTaskStore store) =>
{
    var task = store.Find(id);

    return task is null ? Results.NotFound() : Results.Ok(task);
});
```

Verde. El test crea una tarea, lee el id de la respuesta y la busca de vuelta.

## Cómo funciona

`MapPost` enlaza el cuerpo JSON con `CreateTaskRequest` y el último parámetro desde DI, que es el store singleton. `Results.Created` pone un estado 201 y la cabecera `Location`. `MapGet("/tasks/{id:int}")` agrega una restricción de ruta: el segmento del id debe ser entero, así `/tasks/abc` no matchea este endpoint.

El test usa `WebApplicationFactory<Program>`. La factory arranca la app en memoria y `CreateClient()` devuelve un `HttpClient` que habla con ella. Cada `Api` crea una factory nueva, así cada test empieza con un store vacío.

## Repite con requisitos nuevos

### Listar las tareas

Escribe el test:

```csharp
[TestMethod]
public async Task List_ReturnsCreatedTasks()
{
    using var api = new Api();

    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("First"));
    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Second"));

    var tasks = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    Assert.IsNotNull(tasks);
    Assert.AreEqual(2, tasks.Count);
    Assert.AreEqual("First", tasks[0].Title);
    Assert.AreEqual("Second", tasks[1].Title);
}
```

Agrega el endpoint:

```csharp
app.MapGet("/tasks", (InMemoryTaskStore store) => store.All());
```

Verde. El store agrega al final y el test fija ese orden como parte de la característica.

### Un id que no existe

Escribe el test:

```csharp
[TestMethod]
public async Task UnknownTask_ReturnsNotFound()
{
    using var api = new Api();

    var response = await api.Client.GetAsync("/tasks/99");

    Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
}
```

Pasa con la comprobación de null del GET. El 404 es parte del contrato, no un accidente.

### Un título vacío

Escribe el test:

```csharp
[TestMethod]
public async Task BlankTitle_ReturnsBadRequest()
{
    using var api = new Api();

    var response = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest(" "));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
}
```

Pasa con el guard del POST. El título se recorta antes de llegar al store, así un título con espacios alrededor se guarda limpio.

## Refactor

La API es pequeña, pero ya hay dos bordes:

- `CreateTaskRequest` es la forma de entrada; `TaskItem` es la forma de salida y el valor de dominio. Pueden separarse más en versiones posteriores sin cambiar a todos los llamadores.
- `InMemoryTaskStore` es el borde de almacenamiento. La versión 4 lo reemplaza por EF Core y los endpoints conservan su forma.

El store en memoria no es thread-safe. Es aceptable para una versión tutorial de una petición a la vez; un store real necesita un lock, una colección concurrente o una transacción de base de datos. La versión 4 mueve el estado a SQLite.

## Para cerrar

Lo que cubrimos:

- Una API mínima con POST, GET por id y GET de lista.
- Un store en memoria singleton detrás de una interfaz pequeña.
- Records de entrada y salida.
- Restricciones de ruta, 201 Created con cabecera Location, 400 y 404.
- Tests de aceptación con `WebApplicationFactory` y una factory por test.

Siguiente: [v2: JSON, routing y una forma de error consistente](../v2-json-routing/v2-json-routing.es.md).

