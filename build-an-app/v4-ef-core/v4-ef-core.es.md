# v4: EF Core, code first y una migración

[Encuentras todo el código de esta versión aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v4-ef-core)

La versión 3 guardaba las tareas en una lista singleton. Reiniciar la app las perdía. La versión 4 mueve el almacenamiento a SQLite con EF Core: un modelo code-first, una migración y un repositorio detrás del mismo contrato HTTP.

La solución terminada está en `build-an-app/v4-ef-core`.

## Escribe primero el test

```csharp
[TestMethod]
public async Task TasksSurviveAnAppRestart()
{
    var path = Path.Combine(Path.GetTempPath(), $"tasktracker-{Guid.NewGuid():N}.db");

    try
    {
        int id;

        using (var first = new Api(databasePath: path))
        {
            var response = await first.Client.PostAsJsonAsync(
                "/tasks",
                new CreateTaskRequest("Persist me"));

            var task = await response.Content.ReadFromJsonAsync<TaskItem>();
            id = task!.Id;
        }

        using var second = new Api(databasePath: path);

        var fetched = await second.Client.GetFromJsonAsync<TaskItem>($"/tasks/{id}");

        Assert.IsNotNull(fetched);
        Assert.AreEqual("Persist me", fetched.Title);
    }
    finally
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
```

El test arranca una app, crea una tarea, libera la app, arranca una segunda contra el mismo archivo y lee la tarea. El store en memoria no puede pasarlo.

## Agrega los paquetes

```sh
dotnet add src/TaskTracker.Api package Microsoft.EntityFrameworkCore.Sqlite --version 6.0.36
dotnet add src/TaskTracker.Api package Microsoft.EntityFrameworkCore.Design --version 6.0.36
dotnet tool install --global dotnet-ef --version 6.0.36
```

El paquete de diseño solo se necesita para generar migraciones; lleva `PrivateAssets="all"` para no fluir a los consumidores.

## El modelo y el contexto

```csharp
public sealed class TaskEntity
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }
}

public sealed class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskEntity>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).IsRequired().HasMaxLength(200);
        });
    }
}
```

`TaskEntity` es la forma de persistencia. `TaskItem` sigue siendo la forma de la API. El repositorio mapea entre las dos, así un cambio en la tabla no obliga a cambiar el contrato JSON.

## El repositorio

```csharp
public interface ITaskRepository
{
    IReadOnlyList<TaskItem> All();

    TaskItem? Find(int id);

    TaskItem Add(string title);

    TaskItem? MarkDone(int id);
}

public sealed class EfTaskRepository : ITaskRepository
{
    private readonly TaskDbContext _db;

    public EfTaskRepository(TaskDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<TaskItem> All()
    {
        return _db.Tasks
            .AsNoTracking()
            .OrderBy(task => task.Id)
            .Select(task => new TaskItem(task.Id, task.Title, task.IsDone))
            .ToList();
    }

    public TaskItem? Find(int id)
    {
        var task = _db.Tasks.AsNoTracking().SingleOrDefault(item => item.Id == id);

        return task is null ? null : new TaskItem(task.Id, task.Title, task.IsDone);
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

Las lecturas usan `AsNoTracking()` porque la API no modifica las entidades que devuelve. `Add` guarda y después usa el `Id` generado. `MarkDone` carga la entidad trackeada, la cambia y guarda.

## Conéctalo

```csharp
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TaskTracker")));
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
```

y aplica las migraciones al arrancar:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    db.Database.Migrate();
}
```

La cadena de conexión vive en `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "TaskTracker": "Data Source=tasktracker.db"
  }
}
```

Crea y aplica la migración:

```sh
dotnet ef migrations add InitialCreate --project src/TaskTracker.Api
dotnet ef database update --project src/TaskTracker.Api
```

Los archivos de migración van al control de versiones. Son la historia del esquema, y `Database.Migrate()` aplica las pendientes antes de que la app atienda una petición.

## Cómo funciona

El host de test reemplaza las `DbContextOptions<TaskDbContext>` de la app por una conexión SQLite que controla:

```csharp
builder.ConfigureServices(services =>
{
    services.RemoveAll<DbContextOptions<TaskDbContext>>();
    services.AddDbContext<TaskDbContext>(options =>
    {
        if (_connection is not null)
        {
            options.UseSqlite(_connection);
        }
        else
        {
            options.UseSqlite($"Data Source={databasePath}");
        }
    });
});
```

Para la mayoría de los tests, `_connection` es una conexión abierta a `DataSource=:memory:`. SQLite mantiene una base en memoria solo mientras hay una conexión abierta, así que el driver del test conserva la conexión durante la vida de la factory. Para el test de reinicio, el driver usa un archivo temporal y deja que EF abra y cierre conexiones normalmente.

Cada test recibe una base nueva, así los tests no se pisan. La migración de arranque corre contra la conexión del test y crea el esquema.

## Refactor

Algunos detalles mantienen limpio el borde de persistencia:

- `TaskEntity` no es el record de dominio. El mapeo es explícito y vive en el repositorio.
- Las lecturas usan `AsNoTracking()` y ordenan por `Id`, así el orden de la lista es determinista.
- El contexto es scoped. Un contexto singleton guardaría entidades trackeadas viejas y no es thread-safe.
- `TaskDbContextFactory` implementa `IDesignTimeDbContextFactory<TaskDbContext>` para que los comandos `dotnet ef` no necesiten arrancar el host web ni tocar una base de desarrollo.
- SQLite es un archivo, no un servidor. El siguiente capítulo agrega consultas Dapper contra el mismo archivo; un sistema de producción apuntaría el mismo código a PostgreSQL o SQL Server.

## Para cerrar

Lo que cubrimos:

- Un modelo EF Core code-first, un contexto y una conexión SQLite.
- Un repositorio que mapea entidades al record de dominio de la API.
- Una migración generada con `dotnet ef` y aplicada al arrancar.
- Un driver de test que usa SQLite en memoria por test.
- Un test de reinicio que prueba la persistencia entre instancias.
- Contextos scoped, `AsNoTracking()` y una factory de diseño.

Siguiente: v5: consultas de lectura con Dapper.

