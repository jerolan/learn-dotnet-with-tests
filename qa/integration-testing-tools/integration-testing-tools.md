# Integration Testing Tools

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/integration-testing-tools)

An integration test needs the real database engine, not an in-memory imitation. Two tools make that practical: Testcontainers starts a disposable database in Docker, and Respawn resets it between tests.

This appendix runs a real PostgreSQL container, creates a table and uses Respawn to keep the tests isolated.

## Write the test first

```csharp
[TestMethod]
public async Task Reset_ClearsTheTableBetweenTests()
{
    Assert.AreEqual(0, await _store.CountAsync());
}
```

The test only makes sense if another test inserted a row first. The shared class state and the reset in `TestInitialize` are what make the assertion deterministic.

## Start the container once per class

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

The container starts once for the test class. Starting one per test would be simpler but much slower; the reset below gives the same isolation at a lower cost.

## Reset between tests

```csharp
[TestInitialize]
public async Task ResetAsync()
{
    await _store.EnsureSchemaAsync();

    await using var connection = await OpenAsync();
    await _respawner.ResetAsync(connection);
}
```

Respawn discovers the tables and deletes their rows. It does not drop the schema, so the next test starts from an empty database with the same tables.

The schema is created before the `Respawner` is built because Respawn needs to see at least one table. In an EF Core project, the migration applied at startup does that work.

## The data access

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

The appendix uses Npgsql directly to keep the focus on the container and the reset. The same test shape works with EF Core or Dapper; only the store changes.

## How it works

Testcontainers talks to the Docker daemon, pulls the image if needed, starts a container, waits for the port and gives you a connection string. `DisposeAsync` in `ClassCleanup` stops and removes the container.

Respawn connects to the database, reads the table graph from the catalog and generates delete statements in dependency order. It can ignore migration history tables or specific schemas when the defaults are not enough.

The pair is the usual answer for a server database in tests:

- Testcontainers provides the engine and its version.
- Respawn provides fast isolation between tests.

SQLite remains a good default for a chapter or a small service. A server database earns its place when the test depends on its SQL dialect, its constraints or its transaction behavior.

## Refactor

A few choices keep the setup healthy:

- Pin the image tag (`postgres:16-alpine`). `latest` makes a test change without a code change.
- Start the container once per test class or per collection, not once per test.
- Keep the schema creation in one place and let the reset handle rows.
- Add `TablesToIgnore` for migration history and any table owned by another service.
- Expect the first run to pull the image. In CI, Docker is available on the standard Ubuntu runner; a self-hosted runner needs the daemon and permissions.
- Do not hide a missing Docker daemon with a skipped test. Fail with a clear message or run the suite in an environment that has Docker.

For a full environment with several services, Docker Compose or a Testcontainers network can start the API, the database and a broker together. The reset strategy is still per test class, not per test.

## Wrapping up

What we have covered:

- A PostgreSQL container started with Testcontainers.
- A real schema and a small Npgsql store.
- Respawn resetting rows between tests.
- Container lifecycle, image pinning and migration history.
- The trade-off between SQLite and a server database.

Next: performance testing.

