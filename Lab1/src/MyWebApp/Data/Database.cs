using MySqlConnector;
using MyWebApp.Models;

namespace MyWebApp.Data;

public class Database : IAsyncDisposable
{
    private readonly MySqlConnection _conn;

    public Database(AppConfig config)
    {
        _conn = new MySqlConnection(config.ConnectionString);
    }

    public async Task OpenAsync()
    {
        if (_conn.State != System.Data.ConnectionState.Open)
            await _conn.OpenAsync();
    }

    public async Task<bool> IsReadyAsync()
    {
        try
        {
            await OpenAsync();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // GET /items — id, name
    public async Task<List<(int Id, string Name)>> GetItemsAsync()
    {
        await OpenAsync();
        var result = new List<(int, string)>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, name FROM items ORDER BY id";
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add((reader.GetInt32(0), reader.GetString(1)));
        return result;
    }

    // GET /items/{id} — full details
    public async Task<InventoryItem?> GetItemByIdAsync(int id)
    {
        await OpenAsync();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, quantity, created_at FROM items WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        return new InventoryItem
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Quantity = reader.GetInt32(2),
            CreatedAt = reader.GetDateTime(3)
        };
    }

    // POST /items — create new item
    public async Task<InventoryItem> CreateItemAsync(string name, int quantity)
    {
        await OpenAsync();
        long newId;
        using (var insertCmd = _conn.CreateCommand())
        {
            insertCmd.CommandText =
                "INSERT INTO items (name, quantity, created_at) VALUES (@name, @quantity, UTC_TIMESTAMP())";
            insertCmd.Parameters.AddWithValue("@name", name);
            insertCmd.Parameters.AddWithValue("@quantity", quantity);
            await insertCmd.ExecuteNonQueryAsync();
            newId = insertCmd.LastInsertedId;
        }
        using var selectCmd = _conn.CreateCommand();
        selectCmd.CommandText =
            "SELECT id, name, quantity, created_at FROM items WHERE id = @id";
        selectCmd.Parameters.AddWithValue("@id", newId);
        using var reader = await selectCmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        return new InventoryItem
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Quantity = reader.GetInt32(2),
            CreatedAt = reader.GetDateTime(3)
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _conn.DisposeAsync();
    }
}