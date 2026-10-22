# Integration Testing Tools

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/integration-testing-tools)

An integration test needs the real database engine, not an in-memory imitation. This appendix starts a disposable PostgreSQL container with Testcontainers and resets its rows with Respawn.

## How it works

Testcontainers starts a pinned PostgreSQL image, waits until it accepts connections and supplies the connection string. Respawn reads the table graph and deletes rows in dependency order. The test class starts one container and resets the database before each test. `ClassCleanup` awaits `DisposeAsync` to remove the container when the class finishes.

## How to test

Run these integration tests on a machine with Docker. The tests use PostgreSQL and Npgsql directly; SQLite would not exercise PostgreSQL's dialect, constraints or transaction behavior.

## Write the test first

```csharp
[TestMethod]
public async Task Reset_ClearsTheTableBetweenTests()
{
    Assert.AreEqual(0, await _store.CountAsync());
}
```

This test checks that the test fixture starts each test with an empty table.

## Try to run the test

The method cannot compile yet because `_store` and its database fixture have not been added. It also needs a running PostgreSQL engine, which the test will provision through Docker.

## Write the minimal amount of code for the test to run and check the failing test output

Start one PostgreSQL container for the class, create the schema, then create the Respawner after the table exists:

```csharp
[ClassInitialize]
public static async Task InitializeAsync(TestContext context)
{
    _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    await _container.StartAsync();

    _store = new WidgetStore(_container.GetConnectionString());
    await _store.EnsureSchemaAsync();

    await using var connection = await OpenAsync();
    _respawner = await Respawner.CreateAsync(
        connection,
        new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        });
}
```

Reset before each test:

```csharp
[TestInitialize]
public async Task ResetAsync()
{
    await _store.EnsureSchemaAsync();

    await using var connection = await OpenAsync();
    await _respawner.ResetAsync(connection);
}
```

The database fixture is in place, but `_store` still needs the schema and count operations used by the test.

## Write enough code to make it pass

```csharp
public sealed class WidgetStore
{
    private readonly string _connectionString;

    public WidgetStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task EnsureSchemaAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS widgets (
                id serial primary key,
                name text not null
            );";

        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> CountAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM widgets;";

        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }
}
```

Run `dotnet test`. The reset test passes with an empty table.

## Refactor

The schema is created before `Respawner` is built because Respawn needs to see at least one table. In an EF Core project, a migration applied during startup can create it instead.

A few choices keep the setup healthy:

- Pin the image tag (`postgres:16-alpine`). `latest` makes a test change without a code change.
- Start the container once per test class or collection, not once per test.
- Keep schema creation in one place and let reset handle rows.
- Add `TablesToIgnore` for migration history and any table owned by another service.
- Expect the first run to pull the image. A CI runner needs Docker and the right permissions.
- Do not skip the test when Docker is missing. Fail with a clear message or run the suite where Docker is available.

For several services, Docker Compose or a Testcontainers network can start the API, database and broker together. Reset remains per test, while the container can live for the class.

## Repeat for new requirements

The next requirement is to store widgets. Write the test first:

```csharp
[TestMethod]
public async Task Add_StoresTheWidget()
{
    await _store.AddAsync("first");
    await _store.AddAsync("second");

    Assert.AreEqual(2, await _store.CountAsync());
}
```

The test fails to compile until `AddAsync` exists. Add it to `WidgetStore`:

```csharp
public async Task AddAsync(string name)
{
    await using var connection = new NpgsqlConnection(_connectionString);
    await connection.OpenAsync();

    await using var command = connection.CreateCommand();
    command.CommandText = "INSERT INTO widgets (name) VALUES (@name);";
    command.Parameters.AddWithValue("name", name);

    await command.ExecuteNonQueryAsync();
}
```

Both tests pass. Respawn clears the database before each one, so their rows stay isolated.

## Wrapping up

What we have covered:

- A PostgreSQL container started with Testcontainers.
- A real schema and a small Npgsql store.
- Respawn resetting rows between tests.
- Container lifecycle, image pinning and migration history.
- The trade-off between SQLite and a server database.

Next: performance testing.
