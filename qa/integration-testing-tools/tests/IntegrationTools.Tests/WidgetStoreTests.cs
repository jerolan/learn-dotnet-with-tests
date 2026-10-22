using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace IntegrationTools.Tests;

[TestClass]
public sealed class WidgetStoreTests
{
    private static PostgreSqlContainer _container = null!;
    private static WidgetStore _store = null!;
    private static Respawner _respawner = null!;

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

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        await _container.DisposeAsync();
    }

    [TestInitialize]
    public async Task ResetAsync()
    {
        await _store.EnsureSchemaAsync();
        await using var connection = await OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    private static async Task<NpgsqlConnection> OpenAsync()
    {
        var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();

        return connection;
    }

    [TestMethod]
    public async Task Add_StoresTheWidget()
    {
        await _store.AddAsync("first");
        await _store.AddAsync("second");

        Assert.AreEqual(2, await _store.CountAsync());
    }

    [TestMethod]
    public async Task Reset_ClearsTheTableBetweenTests()
    {
        Assert.AreEqual(0, await _store.CountAsync());
    }
}

