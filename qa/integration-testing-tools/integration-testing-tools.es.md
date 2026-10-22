# Herramientas de testing de integración

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/integration-testing-tools)

Un test de integración necesita el motor de base real, no una imitación en memoria. Este apéndice levanta un contenedor PostgreSQL desechable con Testcontainers y resetea sus filas con Respawn.

## Cómo funciona

Testcontainers inicia una imagen de PostgreSQL fijada, espera a que acepte conexiones y entrega la cadena de conexión. Respawn lee el grafo de tablas y borra filas en orden de dependencias. La clase de test inicia un contenedor y resetea la base antes de cada test. `ClassCleanup` espera `DisposeAsync` para eliminar el contenedor al terminar la clase.

## Cómo probarlo

Ejecuta estos tests de integración en una máquina con Docker. Usan PostgreSQL y Npgsql directamente; SQLite no ejercitaría el dialecto, las restricciones ni las transacciones de PostgreSQL.

## Escribe primero el test

```csharp
[TestMethod]
public async Task Reset_ClearsTheTableBetweenTests()
{
    Assert.AreEqual(0, await _store.CountAsync());
}
```

Este test comprueba que el fixture deja la tabla vacía al comienzo de cada test.

## Intenta ejecutar el test

El método todavía no compila porque aún no existen `_store` ni su fixture de base. También necesita un motor PostgreSQL activo, que el test iniciará mediante Docker.

## Escribe el código mínimo para que el test compile y mira la salida

Inicia un contenedor PostgreSQL para la clase, crea el esquema y después crea `Respawner` cuando la tabla ya exista:

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

Resetea antes de cada test:

```csharp
[TestInitialize]
public async Task ResetAsync()
{
    await _store.EnsureSchemaAsync();

    await using var connection = await OpenAsync();
    await _respawner.ResetAsync(connection);
}
```

El fixture de base ya está, pero a `_store` todavía le faltan las operaciones de esquema y conteo que usa el test.

## Escribe el código suficiente para que pase

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

Ejecuta `dotnet test`. El test de reset pasa con la tabla vacía.

## Refactor

El esquema se crea antes de construir `Respawner` porque Respawn necesita ver al menos una tabla. En un proyecto con EF Core, una migración aplicada al arrancar puede crearla.

Algunas decisiones mantienen sano el setup:

- Fija el tag de la imagen (`postgres:16-alpine`). `latest` hace que un test cambie sin un cambio de código.
- Arranca el contenedor una vez por clase o colección, no una vez por test.
- Mantén la creación del esquema en un solo lugar y deja que el reset maneje las filas.
- Agrega `TablesToIgnore` para el historial de migraciones y cualquier tabla de otro servicio.
- La primera corrida descarga la imagen. El runner de CI necesita Docker y los permisos correctos.
- No omitas el test cuando falte Docker. Falla con un mensaje claro o corre la suite donde Docker esté disponible.

Para varios servicios, Docker Compose o una red de Testcontainers pueden levantar la API, la base y un broker juntos. El reset sigue siendo por test, mientras el contenedor puede vivir toda la clase.

## Repite con requisitos nuevos

El siguiente requisito es guardar widgets. Primero escribe el test:

```csharp
[TestMethod]
public async Task Add_StoresTheWidget()
{
    await _store.AddAsync("first");
    await _store.AddAsync("second");

    Assert.AreEqual(2, await _store.CountAsync());
}
```

El test no compila hasta que exista `AddAsync`. Agrégalo a `WidgetStore`:

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

Los dos tests pasan. Respawn limpia la base antes de cada uno, así sus filas quedan aisladas.

## Para cerrar

Lo que cubrimos:

- Un contenedor PostgreSQL arrancado con Testcontainers.
- Un esquema real y un store pequeño con Npgsql.
- Respawn reseteando filas entre tests.
- Ciclo de vida del contenedor, tag de imagen e historial de migraciones.
- El intercambio entre SQLite y una base de servidor.

Siguiente: tests de rendimiento.
