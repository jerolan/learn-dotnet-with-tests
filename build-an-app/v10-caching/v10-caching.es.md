# v10: Caché con IMemoryCache

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v10-caching)

El endpoint de lista lee las mismas filas en cada petición. La versión 10 cachea la lista y cada tarea en `IMemoryCache`, y el lado de escritura invalida las entradas que cambia. El contador de lecturas de v8 hace visible la caché en un test.

La solución terminada está en `build-an-app/v10-caching`.

## Cómo funciona

`IMemoryCache` guarda la lista bajo `tasks:all` y cada fila bajo `tasks:{id}` durante un TTL configurado. El lector con caché envuelve al contador de lecturas, así un hit retorna antes de llegar a él. El decorador de escritura quita las claves que puede afectar un cambio.

## Cómo probarlo

Lee `/diagnostics` antes y después de dos peticiones de lista. El contador muestra si la segunda petición llegó a Dapper, y las peticiones HTTP mantienen el test en el borde público de la API.

## Escribe primero el test

```csharp
[TestMethod]
public async Task RepeatedListReads_HitTheCache()
{
    using var api = new Api();
    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));

    var before = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

    await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");
    await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    var after = await api.Client.GetFromJsonAsync<DiagnosticsResponse>("/diagnostics");

    Assert.AreEqual(before!.ReadCount + 1, after!.ReadCount);
}
```

En v9 el contador sube dos veces, una por lectura. El test describe el contrato de la caché.

## Intenta ejecutar el test

Ejecútalo contra v9. El contador sube dos veces, así que falla con `Expected:<1>. Actual:<2>`.

## Escribe el código mínimo para que el test compile y mira la salida

### Agrega la caché

Regístrala y enlaza el TTL:

```csharp
builder.Services.AddMemoryCache();
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));
```

```csharp
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public int Seconds { get; set; } = 30;
}
```

Registrar la caché todavía no cambia el lector; ambas peticiones siguen llegando al contador.

## Escribe el código suficiente para que pase

### El decorador de lectura

```csharp
public sealed class CachingTaskReader : ITaskReader
{
    private const string AllKey = "tasks:all";

    private readonly ITaskReader _inner;
    private readonly IMemoryCache _cache;
    private readonly IOptions<CacheOptions> _options;

    public CachingTaskReader(
        ITaskReader inner,
        IMemoryCache cache,
        IOptions<CacheOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _options = options;
    }

    public IReadOnlyList<TaskItem> All()
    {
        return _cache.GetOrCreate(AllKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow =
                TimeSpan.FromSeconds(_options.Value.Seconds);

            return _inner.All();
        })!;
    }

    public TaskItem? Find(int id)
    {
        var key = $"tasks:{id}";

        if (_cache.TryGetValue(key, out TaskItem? cached))
        {
            return cached;
        }

        var task = _inner.Find(id);

        if (task is not null)
        {
            _cache.Set(key, task, TimeSpan.FromSeconds(_options.Value.Seconds));
        }

        return task;
    }
}
```

`GetOrCreate` comprueba la clave, corre la factory en un miss y guarda el resultado con el TTL. `Find` cachea solo tareas encontradas; un id ausente es barato de revisar y cachear un resultado negativo necesitaría su propia invalidación.

La caché va por fuera del contador:

```csharp
builder.Services.AddScoped<CountingTaskReader>(services =>
    new CountingTaskReader(
        services.GetRequiredService<DapperTaskReader>(),
        services.GetRequiredService<ReadCounter>()));

builder.Services.AddScoped<ITaskReader>(services =>
    new CachingTaskReader(
        services.GetRequiredService<CountingTaskReader>(),
        services.GetRequiredService<IMemoryCache>(),
        services.GetRequiredService<IOptions<CacheOptions>>()));
```

Un hit de caché retorna antes de que el contador vea la llamada, y por eso el test espera un solo incremento para dos lecturas.

El lector de fila usa `Find` con el mismo TTL; las claves de lista y tarea ya pueden invalidarse desde el escritor.

## Refactor

La caché es una decisión de corrección tanto como de rendimiento:

- Cada escritura debe invalidar todas las claves que puede afectar. Una clave olvidada significa una respuesta vieja.
- El TTL es la red de seguridad para escrituras que saltean la API, como el SQL crudo del test de v5. Esas escrituras no llaman a `InvalidatingTaskWriter`; solo el TTL o un desalojo manual refrescan la caché.
- `IMemoryCache` es por proceso. Dos instancias de la API no la comparten, y un reinicio arranca vacía. Una caché distribuida (Redis, por ejemplo) necesita otro adaptador y su propia historia de invalidación.
- No cachees objetos scoped como el `DbContext`. Cachea valores planos o DTOs con una vida conocida.
- Mantén las claves en un solo lugar. `tasks:all` y `tasks:{id}` son cadenas hoy; un pequeño ayudante de claves evita errores cuando crezcan.

Los capítulos siguientes reutilizan la forma del decorador: un consumidor de cola, un job programado y una política de resiliencia pueden envolver una interfaz sin editar la implementación central.

## Repite con requisitos nuevos

### Una escritura limpia la lista

```csharp
[TestMethod]
public async Task Write_InvalidatesTheListCache()
{
    using var api = new Api();
    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("First"));

    var first = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");
    await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Second"));
    var second = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    Assert.AreEqual(1, first!.Count);
    Assert.AreEqual(2, second!.Count);
}
```

Sin invalidación, la segunda lectura devuelve la lista cacheada de un elemento, así que falla con `Expected:<2>. Actual:<1>`.

Agrega un decorador de escritura que limpia la lista después de crear una tarea:

```csharp
public sealed class InvalidatingTaskWriter : ITaskWriter
{
    private const string AllKey = "tasks:all";

    private readonly ITaskWriter _inner;
    private readonly IMemoryCache _cache;

    public InvalidatingTaskWriter(ITaskWriter inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public TaskItem Add(string title)
    {
        var task = _inner.Add(title);
        _cache.Remove(AllKey);

        return task;
    }

    public TaskItem? MarkDone(int id) => _inner.MarkDone(id);
}
```

Regístralo alrededor de `EfTaskWriter`:

```csharp
builder.Services.AddScoped<EfTaskWriter>();
builder.Services.AddScoped<ITaskWriter>(services =>
    new InvalidatingTaskWriter(
        services.GetRequiredService<EfTaskWriter>(),
        services.GetRequiredService<IMemoryCache>()));
```

Verde. La segunda lectura de lista ahora ve la tarea nueva.

### Marcar hecha limpia la lista y la tarea

```csharp
[TestMethod]
public async Task MarkDone_InvalidatesBothCachedEntries()
{
    using var api = new Api();
    var created = await api.Client.PostAsJsonAsync("/tasks", new CreateTaskRequest("Write tests"));
    var task = await created.Content.ReadFromJsonAsync<TaskItem>();

    await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{task!.Id}");
    await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    await api.Client.PatchAsync($"/tasks/{task.Id}/done", null);

    var fetched = await api.Client.GetFromJsonAsync<TaskItem>($"/tasks/{task.Id}");
    var list = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    Assert.IsTrue(fetched!.IsDone);
    Assert.IsTrue(list!.Single(item => item.Id == task.Id).IsDone);
}
```

En este punto `MarkDone` deja intacta la caché por tarea, así el test recibe el `IsDone` anterior.

Invalida las dos claves afectadas después de la escritura:

```csharp
public TaskItem? MarkDone(int id)
{
    var task = _inner.MarkDone(id);
    _cache.Remove(AllKey);
    _cache.Remove($"tasks:{id}");

    return task;
}
```

Verde. El escritor conoce el id, así que puede quitar la clave por tarea y la de la lista en un solo lugar.

## Para cerrar

Lo que cubrimos:

- `IMemoryCache` y un TTL desde configuración.
- Un decorador de lectura para la lista y las entradas por id.
- Un decorador de escritura que invalida las claves afectadas.
- Orden de decoradores: la caché va por fuera del contador.
- Riesgos de datos viejos, escrituras externas, alcance por proceso y disciplina de claves.

Siguiente: [v11: mensajería y scheduling](../v11-messaging-scheduling/v11-messaging-scheduling.es.md).

