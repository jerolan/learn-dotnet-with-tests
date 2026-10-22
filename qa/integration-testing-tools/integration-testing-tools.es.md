# Herramientas de testing de integración

[Encuentras todo el código de este apéndice aquí](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/integration-testing-tools)

Un test de integración necesita el motor de base real, no una imitación en memoria. Dos herramientas lo hacen práctico: Testcontainers levanta una base desechable en Docker y Respawn la resetea entre tests.

Este apéndice corre un contenedor real de PostgreSQL, crea una tabla y usa Respawn para aislar los tests.

## Escribe primero el test

```csharp
[TestMethod]
public async Task Reset_ClearsTheTableBetweenTests()
{
    Assert.AreEqual(0, await _store.CountAsync());
}
```

El test solo tiene sentido si otro test insertó una fila antes. El estado compartido de la clase y el reset en `TestInitialize` son lo que hace determinista la aserción.

## Inicia el contenedor una vez por clase

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

El contenedor arranca una vez por clase de test. Arrancar uno por test sería más simple pero mucho más lento; el reset de abajo da el mismo aislamiento a menor costo.

## Resetea entre tests

```csharp
[TestInitialize]
public async Task ResetAsync()
{
    await _store.EnsureSchemaAsync();

    await using var connection = await OpenAsync();
    await _respawner.ResetAsync(connection);
}
```

Respawn descubre las tablas y borra sus filas. No elimina el esquema, así el siguiente test empieza con una base vacía y las mismas tablas.

El esquema se crea antes de construir el `Respawner` porque Respawn necesita ver al menos una tabla. En un proyecto con EF Core, la migración que corre al arrancar hace ese trabajo.

## El acceso a datos

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

El apéndice usa Npgsql directamente para mantener el foco en el contenedor y el reset. La misma forma de test funciona con EF Core o Dapper; solo cambia el store.

## Cómo funciona

Testcontainers habla con el daemon de Docker, descarga la imagen si hace falta, arranca un contenedor, espera el puerto y te da una cadena de conexión. `DisposeAsync` en `ClassCleanup` detiene y elimina el contenedor.

Respawn se conecta a la base, lee el grafo de tablas del catálogo y genera sentencias de borrado en orden de dependencias. Puede ignorar tablas de historial de migraciones o esquemas concretos cuando los valores por defecto no alcanzan.

El par es la respuesta habitual para una base de servidor en tests:

- Testcontainers provee el motor y su versión.
- Respawn provee aislamiento rápido entre tests.

SQLite sigue siendo un buen valor por defecto para un capítulo o un servicio pequeño. Una base de servidor se gana su lugar cuando el test depende de su dialecto SQL, sus restricciones o su comportamiento transaccional.

## Refactor

Algunas decisiones mantienen sano el setup:

- Fija el tag de la imagen (`postgres:16-alpine`). `latest` hace que un test cambie sin un cambio de código.
- Arranca el contenedor una vez por clase o colección, no una vez por test.
- Mantén la creación del esquema en un solo lugar y deja que el reset maneje las filas.
- Agrega `TablesToIgnore` para el historial de migraciones y cualquier tabla de otro servicio.
- La primera corrida descarga la imagen. En CI, Docker está disponible en el runner Ubuntu estándar; un runner propio necesita el daemon y permisos.
- No escondas un daemon de Docker ausente con un test skipeado. Falla con un mensaje claro o corre la suite en un entorno que tenga Docker.

Para un entorno completo con varios servicios, Docker Compose o una red de Testcontainers pueden levantar la API, la base y un broker juntos. La estrategia de reset sigue siendo por clase, no por test.

## Para cerrar

Lo que cubrimos:

- Un contenedor PostgreSQL arrancado con Testcontainers.
- Un esquema real y un store pequeño con Npgsql.
- Respawn reseteando filas entre tests.
- Ciclo de vida del contenedor, fijado de imagen e historial de migraciones.
- El intercambio entre SQLite y una base de servidor.

Siguiente: tests de rendimiento.

