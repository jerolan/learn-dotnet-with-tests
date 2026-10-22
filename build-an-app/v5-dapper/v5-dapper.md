# v5: Dapper Read Queries

[You can find all the code for this version here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/build-an-app/v5-dapper)

EF Core is good at tracking changes and saving entities. Reporting queries often want the SQL in front of you. Version 5 keeps EF for writes and reads through Dapper against the same SQLite database.

The finished solution is in `build-an-app/v5-dapper`.

## How it works

EF Core keeps the write path and its change tracking. Dapper reads from the same SQLite database through the connection owned by `TaskDbContext`; separate `ITaskReader` and `ITaskWriter` interfaces keep those jobs visible.

`Query<T>` maps rows, `QuerySingleOrDefault<T>` returns `null` when no row matches, and `AsList()` materializes the query before the connection is reused. The row type maps SQLite's integer columns to the API's `int` and `bool` values.

The read/write split is a small CQRS step: two interfaces with different jobs, not a separate database or message bus. Reads can select exactly the columns a screen needs; writes keep EF's tracking and migrations.

## How to test

The test inserts a row directly into the open SQLite connection, bypassing EF, then reads it through the HTTP API. The test driver uses the same connection as the app so both libraries see the same in-memory database.

## Write the test first

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

The test writes a row with raw SQL, bypassing EF entirely. If GET sees it, the read path is querying the database, not an EF cache.

## Try to run the test

Run it against the v4 implementation. It passes because EF also reads the shared SQLite database. This chapter changes the read implementation while preserving that behavior, so the green baseline is intentional.

## Write the minimal amount of code for the test to run and check the failing test output

The test already compiles and runs, so no compile-only change is needed. Add Dapper and split the read and write contracts before replacing the implementation.

### Add Dapper

```sh
dotnet add src/TaskTracker.Api package Dapper --version 2.1.35
```

### Split the read and write sides

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

The endpoints change their dependency: GET takes `ITaskReader`, POST and PATCH take `ITaskWriter`. The HTTP contract stays the same.

## Write enough code to make it pass

### The Dapper reader

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

The reader borrows EF's connection with `GetDbConnection()`. That matters for tests: the in-memory SQLite database lives on that exact connection, so Dapper and EF must use the same one. The reader opens it if needed and never disposes it; the context owns it.

`TaskRow` exists because SQLite returns integer columns as `long`, while `TaskItem` uses `int` and `bool`. Mapping explicitly keeps the SQL schema and the API model independent.

### The EF writer

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

Register both sides:

```csharp
builder.Services.AddScoped<ITaskReader, DapperTaskReader>();
builder.Services.AddScoped<ITaskWriter, EfTaskWriter>();
```

The row inserted by the test now appears through the Dapper reader. The rest of the API still writes through EF, and the existing acceptance tests stay green.

## Refactor

A few rules for the mixed approach:

- Never build SQL with string concatenation for values. Use parameters, as `new { Id = id }` does.
- Do not dispose a connection owned by the context. If the reader creates its own connection later, it owns the disposal.
- Use Dapper for reads that are awkward in LINQ: reporting joins, aggregates, hand-tuned projections. Use EF for writes and for queries that benefit from tracking or navigation properties.
- Keep the SQL close to the reader. A repository that hides both SQL and EF behind one method makes it hard to see which path runs.

## Repeat for new requirements

There is no new HTTP behavior in this version. The list and get routes now use Dapper; the existing create and update tests ensure writes still use EF and preserve the same contract.

## Wrapping up

What we have covered:

- A read interface implemented with Dapper and a write interface implemented with EF Core.
- `Query<T>`, `QuerySingleOrDefault<T>`, `AsList()` and parameterized SQL.
- Sharing EF's connection so in-memory tests and transactions see the same data.
- A row DTO that maps SQLite's `long` to the API's `int` and `bool`.
- When a mixed EF/Dapper design is useful.

Next: [v6: validation with FluentValidation](../v6-validation/v6-validation.md).

