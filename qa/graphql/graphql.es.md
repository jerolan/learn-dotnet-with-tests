# GraphQL

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/graphql)

Un endpoint REST devuelve una forma. Una pantalla que necesita el id y el título de la tarea recibe la tarea completa, y una pantalla que necesita otra combinación recibe un segundo endpoint o un parámetro de query que crece cada sprint.

GraphQL publica un esquema. El cliente manda una query que nombra los campos que necesita y el servidor devuelve esa forma. Este apéndice pone una query y una mutation sobre el store de tareas, y las prueba por HTTP con `WebApplicationFactory`.

## Las clases del esquema

`src/TaskApi/TaskItem.cs` y `TaskStore.cs` son el mismo store en memoria que se usa en otros apéndices:

```csharp
public sealed record TaskItem(Guid Id, string Title);

public sealed class TaskStore
{
    private readonly ConcurrentDictionary<Guid, TaskItem> _tasks = new();

    public TaskItem Add(string title)
    {
        var task = new TaskItem(Guid.NewGuid(), title);
        _tasks[task.Id] = task;
        return task;
    }

    public TaskItem? Find(Guid id)
    {
        return _tasks.TryGetValue(id, out var task) ? task : null;
    }

    public IReadOnlyList<TaskItem> All()
    {
        return _tasks.Values.OrderBy(task => task.Title).ToList();
    }
}
```

El tipo de query expone las lecturas:

```csharp
using HotChocolate;

namespace TaskApi;

public sealed class Query
{
    public IReadOnlyList<TaskItem> GetTasks([Service] TaskStore store)
    {
        return store.All();
    }

    public TaskItem? GetTask(Guid id, [Service] TaskStore store)
    {
        return store.Find(id);
    }
}
```

`[Service]` le dice a Hot Chocolate que resuelva el parámetro desde DI, igual que la inyección de una API mínima en un endpoint. Los nombres de método se vuelven nombres de campo con la primera letra en minúscula: `tasks` y `task`.

La mutation escribe:

```csharp
public sealed class Mutation
{
    public TaskItem CreateTask(string title, [Service] TaskStore store)
    {
        return store.Add(title);
    }
}
```

## El servidor

`src/TaskApi/Program.cs`:

```csharp
using TaskApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TaskStore>();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

var app = builder.Build();

app.MapGraphQL("/graphql");

app.Run();

public partial class Program
{
}
```

Un endpoint sirve queries, mutations e introspección. El esquema se construye desde las dos clases al arrancar; un resolver que no se puede conectar falla antes de la primera petición.

## Escribe primero el test

El test manda documentos GraphQL como JSON, igual que un cliente:

```csharp
[TestMethod]
public async Task MutationCreatesTaskAndQueryReadsIt()
{
    var created = await PostAsync(
        "mutation { createTask(title: \"Write the chapter\") { id title } }");

    var task = created.GetProperty("createTask");
    var id = task.GetProperty("id").GetString();

    Assert.AreEqual("Write the chapter", task.GetProperty("title").GetString());

    var read = await PostAsync($"{{ task(id: \"{id}\") {{ id title }} }}");

    Assert.AreEqual(id, read.GetProperty("task").GetProperty("id").GetString());
    Assert.AreEqual("Write the chapter", read.GetProperty("task").GetProperty("title").GetString());
}
```

El helper desenvuelve `data` y falla cuando la respuesta trae un arreglo `errors`:

```csharp
private async Task<JsonElement> PostAsync(string query)
{
    var response = await _client.PostAsJsonAsync("/graphql", new { query });
    response.EnsureSuccessStatusCode();

    var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.IsFalse(
        payload.TryGetProperty("errors", out var errors),
        errors.ToString());

    return payload.GetProperty("data");
}
```

El segundo test pide un id aleatorio y espera que `task` sea `null` en JSON. Esa es la forma del esquema de decir que el campo se resolvió y no hay tarea; un error significaría que la query está mal.

## Intenta correrlo

Con el servidor construido pero sin `MapGraphQL`:

```text
System.Net.Http.HttpRequestException: Response status code does not indicate success: 404 (Not Found).
   at System.Net.Http.HttpResponseMessage.EnsureSuccessStatusCode()
```

El endpoint no existe. Agrega `app.MapGraphQL("/graphql")` y los mismos documentos llegan al esquema.

Corre los tests:

```text
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 893 ms
```

## En qué se diferencia la respuesta de REST

Una respuesta GraphQL trae `data` y, cuando algo falla durante la ejecución, un arreglo `errors`. El estado HTTP suele ser `200` aunque haya `errors`, porque la petición estaba bien formada y parte de los datos pudo resolverse. Un cliente que solo revisa el status code se pierde esos errores.

El cliente elige los campos, así que `{ task(id: "...") { id } }` no devuelve título. Esa es la ventaja: menos datos por la red y ningún endpoint nuevo por pantalla. También es un riesgo: un cliente puede pedir campos anidados muy profundos y convertir una petición en miles de consultas a la base.

## Preocupaciones de producción

- Agrupa las búsquedas repetidas. Una lista de tareas cuyo resolver carga un cliente por tarea es el problema N+1; el DataLoader de Hot Chocolate agrupa esas claves.
- Limita profundidad y complejidad. Sin un límite, una query anidada puede agotar el servidor.
- Autoriza campos, no solo endpoints. Un endpoint sirve todos los campos, así que un chequeo a nivel de ruta no distingue un título público de una nota privada.
- Apaga la introspección si el esquema no debe ser público. Sirve en desarrollo y es exposición innecesaria en producción.
- Prefiere queries persistidas para clientes conocidos. El servidor acepta un identificador en vez de un documento arbitrario, lo que además permite cachear.
- Mantén las mutations explícitas. Una mutation que devuelve un objeto completo invita a los clientes a pedir campos que el camino de escritura no quería exponer.

## Lo que queda diferido

No hay paginación, ni DataLoader, ni suscripciones, ni autorización por campo. Un laboratorio práctico agregaría una conexión `tasks(first: 10, after: ...)`, una carga agrupada de clientes con un contador que demuestre una sola query, y una suscripción sobre el transporte de SignalR o WebSocket.

## Para cerrar

Lo que cubrimos:

- Clases de query y mutation con resolvers sin constructor e inyección con `[Service]`.
- Un solo endpoint `/graphql` construido con `AddGraphQLServer`.
- Tests que mandan documentos como JSON e inspeccionan `data` y `errors`.
- El 404 cuando el endpoint no está mapeado.
- N+1, límites de profundidad, autorización por campo y queries persistidas como preocupaciones de producción.

Sigue: AsyncAPI.
