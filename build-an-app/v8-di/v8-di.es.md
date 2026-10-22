# v8: Tiempos de vida y un decorador

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v8-di)

La API tiene más dependencias ahora: un contexto, un lector, un escritor, un validador y los servicios de JWT. La versión 8 hace visibles los tiempos de vida y agrega un decorador que cuenta lecturas sin cambiar el lector Dapper.

La solución terminada está en `build-an-app/v8-di`.

## Cómo funciona

Un servicio scoped se crea una vez por petición HTTP y se libera al final. `TaskDbContext` y los repositorios son scoped para una unidad de trabajo. Un singleton se crea una vez por app y se comparte entre peticiones concurrentes, así que su estado mutable debe ser thread-safe. Un transient se crea cada vez que se resuelve.

La combinación peligrosa es un singleton que captura un servicio scoped. Mantiene viva la instancia scoped después de que termina la petición y una petición posterior podría usar un contexto liberado. Eso es una dependencia cautiva; la validación de scopes puede detectarla y ASP.NET Core la activa en Development por defecto.

## Cómo probarlo

Llama dos veces a `/diagnostics`. El id de la app debe mantenerse y el id de petición debe cambiar. El driver se autentica por defecto, así estas peticiones prueban la ruta protegida por medio de `WebApplicationFactory`.

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

## Intenta ejecutar el test

Contra v7, la ruta devuelve `404 Not Found`. El test describe el contrato de tiempos de vida antes de que exista el endpoint.

## Escribe el código mínimo para que el test compile y mira la salida

### Agrega los servicios de tiempo de vida

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

Los servicios ya tienen los tiempos de vida esperados, pero el test sigue recibiendo 404 porque falta la ruta de diagnóstico.

## Escribe el código suficiente para que pase

Expón los servicios en la ruta:

```csharp
app.MapGet("/diagnostics", (RequestContext request, AppInstance app, ReadCounter counter) =>
    new DiagnosticsResponse(app.Id, request.Id, counter.Count)).RequireAuthorization();
```

Verde. Dos peticiones comparten el id singleton y reciben ids scoped distintos.

`ReadCounter` usa `Interlocked.Increment` y `Volatile.Read` para actualizar y leer su conteo compartido de forma segura entre peticiones concurrentes.

## Refactor

Las registraciones son ahora la raíz de composición de la aplicación. Mantenlas juntas y haz explícita la elección del tiempo de vida:

- Scoped para lo que posee una unidad de trabajo: `TaskDbContext`, los repositorios y el validador.
- Singleton para configuración sin estado y contadores de proceso, con estado thread-safe.
- Transient para ayudantes baratos y sin estado que no deben compartirse.

El decorador es un ejemplo pequeño de una preocupación transversal. Un decorador de logging, caché o métricas puede envolver la misma interfaz sin editar `DapperTaskReader`. Los capítulos siguientes usan la misma forma para logging y caché.

Evita el service locator en los handlers. Pedir `IServiceProvider` y resolver servicios a mano esconde las dependencias y vuelve los tests menos directos. Los parámetros del endpoint son la lista de dependencias.

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

Antes del decorador, el conteo sigue en cero, así que este test falla después de la petición a `/tasks`.

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

## Para cerrar

Lo que cubrimos:

- Tiempos de vida scoped, singleton y transient en una API.
- Un id de petición scoped y un id de aplicación singleton expuestos por diagnóstico.
- Un contador singleton thread-safe.
- Un decorador que envuelve `ITaskReader` y cuenta lecturas.
- Registro por factory para evitar una resolución recursiva del decorador.
- Dependencias cautivas y la raíz de composición.

Siguiente: [v9: logging estructurado](../v9-logging/v9-logging.es.md).

