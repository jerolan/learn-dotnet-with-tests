# v5: Consultas de lectura con Dapper

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v5-dapper)

EF Core es bueno trackeando cambios y guardando entidades. Las consultas de reporte muchas veces quieren el SQL a la vista. La versión 5 deja EF para las escrituras y lee con Dapper contra la misma base SQLite.

La solución terminada está en `build-an-app/v5-dapper`.

## Escribe primero el test

```csharp
[TestMethod]
public async Task Get_SeesRowsWrittenOutsideEf()
{
    using var api = new Api();

    await api.ExecuteAsync("INSERT INTO Tasks (Title, IsDone) VALUES ('Direct', 0);");

    var tasks = await api.Client.GetFromJsonAsync<List<TaskItem>>("/tasks");

    Assert.IsNotNull(tasks);
    Assert.AreEqual(1, tasks.Count);
    Assert.AreEqual("Direct", tasks[0].Title);
}
```

El test escribe una fila con SQL crudo, salteando EF por completo. Si GET la ve, el camino de lectura consulta la base, no una caché de EF.

## Agrega Dapper

```sh
dotnet add src/TaskTracker.Api package Dapper --version 2.1.35
```

## Separa lectura y escritura

```csharp
public interface ITaskReader
{
    IReadOnlyList<TaskItem> All();

    TaskItem? Find(int id);
}

public interface ITaskWriter
{
    TaskItem Add(string title);

    TaskItem? MarkDone(int id);
}
```

Los endpoints cambian su dependencia: GET recibe `ITaskReader`, POST y PATCH reciben `ITaskWriter`. El contrato HTTP queda igual.

## El lector Dapper

```csharp
public sealed class DapperTaskReader : ITaskReader
{
    private readonly TaskDbContext _db;

    public DapperTaskReader(TaskDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<TaskItem> All()
    {
        var rows = Connection()
            .Query<TaskRow>("SELECT Id, Title, IsDone FROM Tasks ORDER BY Id")
            .AsList();

        return rows.Select(row => row.ToTaskItem()).ToList();
    }

    public TaskItem? Find(int id)
    {
        var row = Connection().QuerySingleOrDefault<TaskRow>(
            "SELECT Id, Title, IsDone FROM Tasks WHERE Id = @Id",
            new { Id = id });

        return row?.ToTaskItem();
    }

    private IDbConnection Connection()
    {
        var connection = _db.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        return connection;
    }

    private sealed class TaskRow
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public long IsDone { get; set; }

        public TaskItem ToTaskItem()
        {
            return new TaskItem((int)Id, Title, IsDone != 0);
        }
    }
}
```

El lector toma prestada la conexión de EF con `GetDbConnection()`. Eso importa en los tests: la base SQLite en memoria vive en esa conexión exacta, así que Dapper y EF tienen que usar la misma. El lector la abre si hace falta y nunca la libera; el contexto es el dueño.

`TaskRow` existe porque SQLite devuelve las columnas enteras como `long`, mientras `TaskItem` usa `int` y `bool`. Mapear explícitamente mantiene independientes el esquema SQL y el modelo de la API.

## El escritor EF

```csharp
public interface ITaskWriter
{
    TaskItem Add(string title);

    TaskItem? MarkDone(int id);
}

public sealed class EfTaskWriter : ITaskWriter
{
    private readonly TaskDbContext _db;

    public EfTaskWriter(TaskDbContext db)
    {
        _db = db;
    }

    public TaskItem Add(string title)
    {
        var task = new TaskEntity { Title = title };
        _db.Tasks.Add(task);
        _db.SaveChanges();

        return new TaskItem(task.Id, task.Title, task.IsDone);
    }

    public TaskItem? MarkDone(int id)
    {
        var task = _db.Tasks.SingleOrDefault(item => item.Id == id);

        if (task is null)
        {
            return null;
        }

        task.IsDone = true;
        _db.SaveChanges();

        return new TaskItem(task.Id, task.Title, task.IsDone);
    }
}
```

Registra los dos lados:

```csharp
builder.Services.AddScoped<ITaskReader, DapperTaskReader>();
builder.Services.AddScoped<ITaskWriter, EfTaskWriter>();
```

## Cómo funciona

`Query<T>` ejecuta una sentencia SQL y mapea cada fila. `QuerySingleOrDefault<T>` espera cero o una fila y devuelve `null` cuando no hay ninguna. `AsList()` materializa el resultado; sin él, la secuencia perezosa de Dapper mantendría el data reader abierto mientras el llamador itera.

El parámetro del id se pasa como objeto anónimo, no concatenado en la cadena. Dapper convierte `@Id` en un parámetro, que es la defensa contra inyección SQL y además deja que el proveedor reutilice el plan de la consulta.

La separación lectura/escritura es un paso pequeño de CQRS. No es una base separada ni un bus de mensajes; son dos interfaces con trabajos distintos. El lado de lectura puede crecer SQL que devuelva exactamente las columnas que necesita una pantalla, mientras el de escritura conserva el tracking y las migraciones de EF.

## Refactor

Algunas reglas para el enfoque mixto:

- Nunca construyas SQL concatenando valores. Usa parámetros, como `new { Id = id }`.
- No liberes una conexión que pertenece al contexto. Si el lector crea su propia conexión más adelante, él es dueño de liberarla.
- Usa Dapper para lecturas que son incómodas en LINQ: joins de reporte, agregados, proyecciones a mano. Usa EF para escrituras y para consultas que se benefician del tracking o de las propiedades de navegación.
- Mantén el SQL cerca del lector. Un repositorio que esconde SQL y EF detrás de un método hace difícil ver qué camino corre.

## Para cerrar

Lo que cubrimos:

- Una interfaz de lectura implementada con Dapper y una de escritura con EF Core.
- `Query<T>`, `QuerySingleOrDefault<T>`, `AsList()` y SQL parametrizado.
- Compartir la conexión de EF para que los tests en memoria y las transacciones vean los mismos datos.
- Un DTO de fila que mapea el `long` de SQLite al `int` y `bool` de la API.
- Cuándo sirve un diseño mixto EF/Dapper.

Siguiente: v6: validación con FluentValidation.

