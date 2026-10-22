# Escalar tests de aceptación

[Encuentras todo el código de este capítulo aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/testing/scaling-acceptance-tests)

Un test de aceptación es fácil de leer. Veinte, cada uno con su propio setup HTTP y su plumbing de JSON, se vuelven lentos y repetitivos. Escalar tests de aceptación es sobre todo mantener los escenarios enfocados y mover la parte mecánica a un driver pequeño.

Vamos a hacer crecer la API de tareas con validación y detección de duplicados, e introducir un driver de test para que cada test diga un solo comportamiento. La solución terminada está en `testing/scaling-acceptance-tests`.

## Cómo funciona

`WebApplicationFactory<Program>` arranca la API en memoria y envía peticiones por el pipeline HTTP real. Cada factory arranca una app nueva con su propia lista de tareas, lo que mantiene aislados los tests.

## Cómo probarlo

`Api` crea y libera la factory, y `TodoClient` envuelve las llamadas HTTP y JSON. Los tests usan esta superficie pequeña en vez de repetir el setup de la factory y el plumbing de las peticiones.

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

El driver es código de test, no de producción. Esconde la factory, el cliente y las llamadas JSON, así el test se lee como "crea esta tarea y espera este estado". Si cambia la superficie de la API, actualiza el driver en un solo lugar.

## Escribe primero el test

```csharp
[TestMethod]
public async Task RejectsATodoWithoutATitle()
{
    using var api = new Api();

    var response = await api.Client.CreateAsync(new Todo(1, " "));

    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
}
```

## Intenta ejecutar el test

Ejecuta el test desde la carpeta de la solución. La API actual arranca y atiende la petición, así que el test llega al comportamiento que falta.

## Escribe el código mínimo para que el test compile y mira la salida

La API del capítulo anterior ya compila y responde. No hace falta un cambio solo para compilar; este es el handler mínimo que ya funciona y mantiene el test en rojo:

```csharp
app.MapPost("/todos", (Todo todo) =>
{
    todos.Add(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
});
```

El test se ejecuta y falla con `Expected:<BadRequest>. Actual:<Created>`.

## Escribe el código suficiente para que pase

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

Verde. El endpoint ahora rechaza un título vacío con 400 y un pequeño error JSON.

## Refactor

El driver mantiene cortos los tests, y cada test sigue usando su propia factory. Todavía no hay código de producción que extraer; conserva el test en verde y el aislamiento por test.

### Mantener manejable la suite

- Un comportamiento por test. El nombre debe decir qué obtiene la persona, no qué endpoint se llamó.
- Construye datos con ayudantes pequeños. `new Todo(1, "First")` alcanza aquí; un objeto más grande pide un builder para que el test nombre solo los campos que importan.
- Aísla el estado. Una factory por test es la regla más simple; no dejes que los tests dependan del orden.
- Mantén los caminos críticos en este nivel. Validación, orden y conflictos son visibles para la persona. El formato exacto de un mensaje o la rama dentro de un ayudante pertenecen a tests unitarios.
- No afirmes detalles de implementación. El test debe sobrevivir a reemplazar la lista por un repositorio.

MSTest tiene `[ClassInitialize]` y `[AssemblyInitialize]` para setup compartido. Úsalos si arrancar la app se vuelve costoso, y resetea los datos para que cada test empiece desde un estado conocido. Esta API arranca lo bastante rápido como para que una factory por test sea la mejor opción.

## Repite con requisitos nuevos

### Orden de creación

Escribe el test:

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

Pasa porque el endpoint agrega al final de una lista. El test fija el orden como parte de la característica, así un cambio posterior a un diccionario o a un almacenamiento sin orden no puede cambiarlo en silencio.

### Ids duplicados

Escribe el test:

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

Falla con `Expected:<Conflict>. Actual:<Created>`. Agrega el guard:

```csharp
if (todos.Any(existing => existing.Id == todo.Id))
{
    return Results.Conflict(new { error = "A todo with that id already exists." });
}
```

Verde. El test usa el driver dos veces para preparar y la tercera llamada para el comportamiento bajo prueba.

## Para cerrar

Lo que cubrimos:

- Un driver de test que envuelve las llamadas HTTP.
- Factories por test para aislar el estado de la aplicación.
- Tests de aceptación para validación, orden e ids duplicados.
- El intercambio entre un fixture compartido y el aislamiento por test.
- Mantener los tests de aceptación en el nivel visible y los unitarios para el detalle.

El capítulo sigue [Scaling acceptance tests de Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/scaling-acceptance-tests): la suite crece sin convertir cada escenario en una copia del setup. El driver y la factory son la versión .NET de esa idea.

Siguiente: [Trabajar sin mocks](../working-without-mocks/working-without-mocks.es.md).

