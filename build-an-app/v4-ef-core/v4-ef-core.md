# v4: EF Core, Code First and a Migration

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v4-ef-core)

Version 3 kept tasks in a singleton list. Restarting the app lost them. Version 4 moves the store to SQLite with EF Core: a code-first model, a migration and a repository behind the same HTTP contract.

The finished solution is in `build-an-app/v4-ef-core`.

## How it works

EF Core maps a persistence entity through `TaskDbContext`; `ITaskRepository` keeps that storage model behind the same API contract. `Database.Migrate()` applies the checked-in schema history before the app serves requests.

## How to test

The restart test uses a temporary SQLite file so a second app instance can read the first one's data. Other tests use an open in-memory SQLite connection. The test host replaces the app's context options:

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

SQLite keeps an in-memory database only while a connection remains open, so the test driver holds that connection for the factory's lifetime. Each test gets a fresh database; startup migrations create its schema.

## Write the test first

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

The test starts an app, creates a task, disposes the app, starts a second app against the same file and reads the task. The in-memory store cannot pass it.

## Try to run the test

Run it against v3's singleton store. The second app starts with an empty list, so the GET returns no task and `Assert.IsNotNull(fetched)` fails.

## Write the minimal amount of code for the test to run and check the failing test output

### Add the packages

```sh
dotnet add src/TaskTracker.Api package Microsoft.EntityFrameworkCore.Sqlite --version 6.0.36
dotnet add src/TaskTracker.Api package Microsoft.EntityFrameworkCore.Design --version 6.0.36
dotnet tool install --global dotnet-ef --version 6.0.36
```

The design package is only needed to generate migrations; it is marked `PrivateAssets="all"` so it does not flow to consumers.

### The model and the context

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

`TaskEntity` is the persistence shape. `TaskItem` stays the API shape. The repository maps between them, so a change to the table does not force a change to the JSON contract.

The app still uses the in-memory store, so the restart test remains red. Next, route reads and writes through the EF repository.

## Write enough code to make it pass

### The repository

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

Reads use `AsNoTracking()` because the API does not modify the entities it returns. `Add` saves and then uses the generated `Id`. `MarkDone` loads the tracked entity, changes it and saves.

### Wire it up

Change the endpoint parameters from `InMemoryTaskStore` to `ITaskRepository`, remove the singleton store registration, then register the EF context and repository:

```csharp
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TaskTracker")));
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>();
```

and apply migrations at startup:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
    db.Database.Migrate();
}
```

The connection string lives in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "TaskTracker": "Data Source=tasktracker.db"
  }
}
```

Create and apply the migration:

```sh
dotnet ef migrations add InitialCreate --project src/TaskTracker.Api
dotnet ef database update --project src/TaskTracker.Api
```

The migration files belong in source control. They are the schema history, and `Database.Migrate()` applies the pending ones before the app serves a request.

The restart test now reads the task from the second app. Green: persistence changed, while the HTTP contract stayed the same.

## Refactor

A few details keep the persistence boundary clean:

- `TaskEntity` is not the domain record. The mapping is explicit and lives in the repository.
- Reads use `AsNoTracking()` and sort by `Id`, so the list order stays deterministic.
- The context is scoped. A singleton context would keep stale tracked entities and is not thread-safe.
- `TaskDbContextFactory` implements `IDesignTimeDbContextFactory<TaskDbContext>` so the `dotnet ef` commands do not need to start the web host or touch a development database.
- SQLite is a file, not a server. The next chapter adds Dapper queries against the same file; a production system would point the same code at PostgreSQL or SQL Server.

## Repeat for new requirements

This version's new requirement is persistence across app restarts. The existing create, list, get and update tests continue to pin the HTTP behavior while the repository changes underneath; no second persistence feature is added here.

## Wrapping up

What we have covered:

- A code-first EF Core model, a context and a SQLite connection.
- A repository that maps entities to the API's domain record.
- A migration generated with `dotnet ef` and applied at startup.
- A test driver that uses in-memory SQLite per test.
- A restart test that proves persistence across app instances.
- Scoped contexts, `AsNoTracking()` and a design-time factory.

Next: [v5: Dapper read queries](../v5-dapper/v5-dapper.md).

