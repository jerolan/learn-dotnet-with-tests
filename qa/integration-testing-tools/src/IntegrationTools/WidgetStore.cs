using Npgsql;

namespace IntegrationTools;

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

    public async Task AddAsync(string name)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO widgets (name) VALUES (@name);";
        command.Parameters.AddWithValue("name", name);

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

