using MySqlConnector;

namespace MyWebApp.Data;

public static class DbMigrator
{
    private const string CreateItemsTable = """
        CREATE TABLE IF NOT EXISTS items (
            id         INT          NOT NULL AUTO_INCREMENT,
            name       VARCHAR(255) NOT NULL,
            quantity   INT          NOT NULL DEFAULT 0,
            created_at DATETIME     NOT NULL DEFAULT UTC_TIMESTAMP(),
            PRIMARY KEY (id)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
        """;

    private const string CreateNameIndex = """
        CREATE INDEX IF NOT EXISTS idx_items_name ON items(name);
        """;

    public static async Task RunAsync(string connectionString)
    {
        await using var conn = new MySqlConnection(connectionString);
        await conn.OpenAsync();

        await ExecAsync(conn, CreateItemsTable);
        await ExecAsync(conn, CreateNameIndex);

        Console.WriteLine("[migrate] Schema is up to date.");
    }

    private static async Task ExecAsync(MySqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}