# NoSQL con MongoDB

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/nosql)

Una tabla relacional obliga a que cada fila tenga las mismas columnas. Un document store deja que una tarea lleve un checklist, otra una fecha de vencimiento y una tercera ninguna de las dos. Esa flexibilidad tiene un precio: la base impone menos reglas, y el orden, los índices y las actualizaciones se comportan distinto que en SQL.

Este apéndice guarda tareas en MongoDB, levanta un servidor real en Docker y prueba el repositorio contra él. El fallo que guía el último cambio es un orden que pone `Beta` antes que `alpha`.

## Cómo funciona

MongoDB guarda cada tarea como un documento BSON. El repositorio traduce expresiones de C# a filtros y actualizaciones; MongoDB define el orden y la representación almacenada.

### El documento

`src/TaskTracker.Mongo/TaskDocument.cs`:

```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TaskTracker.Mongo;

public sealed class TaskDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

`[BsonId]` mapea la propiedad a `_id`. La representación string guarda el GUID como texto legible en vez del subtipo binario del driver. Elige una representación y mantenla: una base escrita con GUIDs binarios no coincide con un filtro construido desde strings.

Mongo guarda `DateTime` como milisegundos UTC. Una hora local se convierte al escribir y vuelve como UTC.

### El repositorio

`src/TaskTracker.Mongo/TaskRepository.cs`:

```csharp
public sealed class TaskRepository
{
    private readonly IMongoCollection<TaskDocument> _tasks;

    public TaskRepository(IMongoDatabase database)
    {
        _tasks = database.GetCollection<TaskDocument>("tasks");
    }

    public Task InsertAsync(TaskDocument task, CancellationToken cancellationToken = default)
    {
        return _tasks.InsertOneAsync(task, cancellationToken: cancellationToken);
    }

    public async Task<TaskDocument?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _tasks
            .Find(task => task.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> MarkDoneAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _tasks.UpdateOneAsync(
            task => task.Id == id,
            Builders<TaskDocument>.Update.Set(task => task.IsDone, true),
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }
}
```

`MatchedCount` responde "coincidió algún documento con el filtro". `ModifiedCount` responde "cambió algún valor". Marcar como hecha una tarea que ya lo estaba coincide pero no modifica; tratarlo como tarea ausente sería incorrecto.

### Levanta Mongo una vez

`tests/TaskTracker.Mongo.Tests/TaskRepositoryTests.cs`:

```csharp
[ClassInitialize]
public static async Task StartMongo(TestContext context)
{
    _mongo = new MongoDbBuilder()
        .WithImage("mongo:6.0")
        .Build();

    await _mongo.StartAsync();
}

[ClassCleanup]
public static async Task StopMongo()
{
    await _mongo.DisposeAsync();
}
```

El contenedor arranca una vez por clase de test. `TestInitialize` borra la base y crea un repositorio nuevo, así cada test empieza con una colección vacía:

```csharp
var client = new MongoClient(_mongo.GetConnectionString());
await client.DropDatabaseAsync("tasktracker");

_repository = new TaskRepository(client.GetDatabase("tasktracker"));
```

La imagen pesa 1.1 GB, así que la primera corrida paga el pull. Las siguientes la reutilizan.

## Cómo probarlo

Estos tests usan un contenedor MongoDB real mediante Testcontainers, así que Docker debe estar disponible. La base se borra antes de cada test; ejecuta `dotnet test Nosql.sln` desde esta carpeta.

## Escribe primero el test

El documento y las operaciones básicas del repositorio dan contexto; el nuevo comportamiento de lista empieza con su propio test. Inserta valores a ambos lados del límite entre mayúsculas y minúsculas y afirma el orden visible para el usuario:

```csharp
[TestMethod]
public async Task ListByTitleIgnoresCase()
{
    await _repository.InsertAsync(new TaskDocument
    {
        Id = Guid.NewGuid(),
        Title = "Beta",
        CreatedAt = DateTime.UtcNow
    });
    await _repository.InsertAsync(new TaskDocument
    {
        Id = Guid.NewGuid(),
        Title = "alpha",
        CreatedAt = DateTime.UtcNow
    });

    var tasks = await _repository.ListByTitleAsync();

    CollectionAssert.AreEqual(
        new[] { "alpha", "Beta" },
        tasks.Select(task => task.Title).ToArray());
}
```

## Intenta ejecutar el test

Antes de agregar la consulta de lista, el test no compila porque falta `ListByTitleAsync`.

## Escribe el código mínimo para que el test compile y mira la salida

La consulta básica delega el orden a MongoDB, pero usa la collation por defecto:

```csharp
return await _tasks
    .Find(FilterDefinition<TaskDocument>.Empty)
    .SortBy(task => task.Title)
    .ToListAsync(cancellationToken);
```

Ejecuta el test. Ahora llega a la aserción y falla:

```text
CollectionAssert.AreEqual failed. (Element at index 0 do not match.)
```

La collation binaria por defecto de Mongo ordena por code point. La `B` mayúscula (66) va antes que la `a` minúscula (97), así que el orden es `Beta`, `alpha`. Quien lee una lista de tareas espera un orden que ignore mayúsculas.

No arregles esto trayendo todo y ordenando en memoria. El orden pertenece a la base; una colección con un millón de tareas no debería cruzar la red para ordenarse.

## Escribe el código suficiente para que pase

Dale una collation a la query:

```csharp
using var cursor = await _tasks.FindAsync(
    FilterDefinition<TaskDocument>.Empty,
    new FindOptions<TaskDocument>
    {
        Sort = Builders<TaskDocument>.Sort.Ascending(task => task.Title),
        Collation = new Collation("en", strength: CollationStrength.Secondary)
    },
    cancellationToken);

return await cursor.ToListAsync(cancellationToken);
```

`Secondary` compara letras e ignora mayúsculas. Corre los tests:

```text
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 6 s
```

## Refactor

### Qué cambia Mongo

- No hay migración de esquema para un campo opcional nuevo. Sí hay migración de datos cuando un campo cambia de significado o de representación.
- Un documento puede embeber una lista que SQL pondría en una segunda tabla. Embebe lo que se lee junto; referencia lo que crece sin límite.
- Los índices no son automáticos. Una query que ordena por `Title` sin índice hace un scan de la colección; agrega el índice en una migración de arranque o en un script de infraestructura.
- Las transacciones multi-documento necesitan un replica set. Un contenedor de un solo nodo sirve para este test de repositorio, no para código que depende de transacciones.
- El driver serializa por nombre de propiedad. Renombrar una propiedad de C# es una migración de datos salvo que mapees el nombre viejo explícitamente.

### Preocupaciones de producción

- Crea índices para los filtros y órdenes que realmente usas, y revisa el plan con `explain`.
- Mantén la cadena de conexión en configuración o en un gestor de secretos; nunca en el repositorio.
- Habilita escrituras reintentables y elige el read concern a propósito. Una lectura de secundario puede estar desactualizada.
- Fija un timeout de selección de servidor y de socket. Los defaults pueden hacer que un nodo caído parezca un cuelgue.
- Haz backup antes de cambiar la representación de un campo existente. Una colección mixta es más difícil de arreglar que una vacía.
- Agrega health checks que hagan ping a la base, no solo al proceso.

## Repite con requisitos nuevos

El siguiente requisito es indicar que una tarea ausente no cambió:

```csharp
[TestMethod]
public async Task MarkDoneReturnsFalseForMissingTask()
{
    var changed = await _repository.MarkDoneAsync(Guid.NewGuid());

    Assert.IsFalse(changed);
}
```

`MatchedCount` sigue en cero para un id desconocido, así que el repositorio devuelve `false`.

## Lo que queda diferido

No hay autenticación, TLS, replica set, transacción, change stream ni migración de índices. Un laboratorio práctico habilitaría auth en el contenedor, correría un replica set de tres nodos, insertaría un documento en una transacción y observaría un change stream entregar el insert a un servicio en segundo plano.

## Para cerrar

Lo que cubrimos:

- Un modelo de documento con una representación explícita del id BSON.
- Un repositorio para insertar, leer, actualizar y listar ordenado.
- Un contenedor MongoDB real levantado una vez por clase de test.
- El fallo de orden sensible a mayúsculas y la collation que lo arregla.
- Matched frente a modified, índices, transacciones y cambios de representación como preocupaciones de producción.

Sigue: streaming.
