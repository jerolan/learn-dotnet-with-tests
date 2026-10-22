# v8: Tiempos de vida y un decorador

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v8-di)

La API tiene más dependencias ahora: un contexto, un lector, un escritor, un validador y los servicios de JWT. La versión 8 hace visibles los tiempos de vida y agrega un decorador que cuenta lecturas sin cambiar el lector Dapper.

La solución terminada está en `build-an-app/v8-di`.

## Escribe primero el test

```csharp
[TestMethod]
public async Task Diagnostics_UsesAScopedRequestIdAndASingletonAppId()
{
    using var api = new Api();

    var first = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");
    var second = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

    Assert.IsNotNull(first);
    Assert.IsNotNull(second);
    Assert.AreEqual(first.AppId, second.AppId);
    Assert.AreNotEqual(first.RequestId, second.RequestId);
}
```

Contra v7, la ruta devuelve 404. El test describe el contrato de tiempos de vida antes de que exista el endpoint.

## Agrega los servicios de tiempo de vida

```csharp
public sealed class RequestContext
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class AppInstance
{
    public Guid Id { get; } = Guid.NewGuid();
}

public sealed class ReadCounter
{
    private int _count;

    public int Count => Volatile.Read(ref _count);

    public void Increment()
    {
        Interlocked.Increment(ref _count);
    }
}
```

Regístralos:

```csharp
builder.Services.AddScoped<RequestContext>();
builder.Services.AddSingleton<AppInstance>();
builder.Services.AddSingleton<ReadCounter>();
```

y expónlos:

```csharp
app.MapGet("/diagnostics", (RequestContext request, AppInstance app, ReadCounter counter) =>
    new DiagnosticsResponse(app.Id, request.Id, counter.Count)).RequireAuthorization();
```

Verde. Dos peticiones comparten el id singleton y reciben ids scoped distintos.

## Cómo funciona

Un servicio scoped se crea una vez por petición HTTP. La misma instancia se reutiliza en toda la petición y se libera al final. `TaskDbContext` y los repositorios son scoped por eso: una unidad de trabajo, un contexto.

Un singleton se crea una vez por aplicación. `AppInstance` y `ReadCounter` son singletons; cada petición ve la misma instancia. Un singleton debe ser thread-safe, porque peticiones concurrentes lo usan a la vez. `ReadCounter` usa `Interlocked.Increment` y `Volatile.Read` por eso.

Un transient se crea cada vez que se resuelve. Sirve para servicios baratos y sin estado, pero resolverlo muchas veces dentro de una petición puede crear más objetos de los esperados.

La combinación peligrosa es un singleton que captura un servicio scoped. El singleton mantiene viva la instancia scoped después de que termina la petición, y una petición posterior usa un contexto liberado. Eso es una dependencia cautiva. El contenedor puede validarlo si activas la validación de scopes; ASP.NET Core lo hace en Development por defecto.

## Repite con requisitos nuevos

### Contar lecturas con un decorador

Escribe el test:

```csharp
[TestMethod]
public async Task ReadCounter_IsIncrementedByTheDecorator()
{
    using var api = new Api();

    var before = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");
    await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");
    var after = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

    Assert.IsNotNull(before);
    Assert.IsNotNull(after);
    Assert.AreEqual(before.ReadCount + 1, after.ReadCount);
}
```

Agrega el decorador:

```csharp
public sealed class CountingTaskReader : ITaskReader
{
    private readonly ITaskReader _inner;
    private readonly ReadCounter _counter;

    public CountingTaskReader(ITaskReader inner, ReadCounter counter)
    {
        _inner = inner;
        _counter = counter;
    }

    public IReadOnlyList<TaskItem> All()
    {
        _counter.Increment();
        return _inner.All();
    }

    public TaskItem? Find(int id)
    {
        _counter.Increment();
        return _inner.Find(id);
    }
}
```

Regístralo con una factory para que el lector interno sea explícito:

```csharp
builder.Services.AddScoped<DapperTaskReader>();
builder.Services.AddScoped<ITaskReader>(services =>
    new CountingTaskReader(
        services.GetRequiredService<DapperTaskReader>(),
        services.GetRequiredService<ReadCounter>()));
```

Verde. Los endpoints siguen pidiendo `ITaskReader`; no saben que un contador lo envuelve.

Un `AddScoped<ITaskReader, CountingTaskReader>()` simple no funcionaría: el decorador necesita otro `ITaskReader` que envolver, y el contenedor resolvería la misma registración recursivamente. La factory rompe el ciclo nombrando el tipo concreto interno.

## Refactor

Las registraciones son ahora la raíz de composición de la aplicación. Mantenlas juntas y haz explícita la elección de tiempo de vida:

- Scoped para lo que posee una unidad de trabajo: `TaskDbContext`, los repositorios, el validador.
- Singleton para configuración sin estado y contadores de proceso, con estado thread-safe.
- Transient para ayudantes baratos y sin estado que no deben compartirse.

El decorador es un ejemplo pequeño de una preocupación transversal. Un decorador de logging, caché o métricas puede envolver la misma interfaz sin editar `DapperTaskReader`. Los capítulos siguientes usan la misma forma para logging y caché.

Evita el service locator en los handlers. Pedir `IServiceProvider` y resolver servicios a mano esconde las dependencias y vuelve los tests menos directos. Los parámetros del endpoint son la lista de dependencias.

## Para cerrar

Lo que cubrimos:

- Tiempos de vida scoped, singleton y transient en una API.
- Un id de petición scoped y un id de aplicación singleton expuestos por diagnóstico.
- Un contador singleton thread-safe.
- Un decorador que envuelve `ITaskReader` y cuenta lecturas.
- Registro por factory para evitar una resolución recursiva del decorador.
- Dependencias cautivas y la raíz de composición.

Siguiente: [v9: logging estructurado](../v9-logging/v9-logging.es.md).

