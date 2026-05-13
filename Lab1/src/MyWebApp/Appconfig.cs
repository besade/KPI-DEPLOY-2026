namespace MyWebApp;

public class AppConfig
{
    public string Host { get; private set; } = "127.0.0.1";
    public int Port { get; private set; } = 3000;
    public string DbHost { get; private set; } = "127.0.0.1";
    public int DbPort { get; private set; } = 3306;
    public string DbName { get; private set; } = "mywebapp";
    public string DbUser { get; private set; } = "mywebapp";
    public string DbPassword { get; private set; } = "";
    public bool RunMigration { get; private set; } = false;

    public string ConnectionString =>
        $"Server={DbHost};Port={DbPort};Database={DbName};User={DbUser};Password={DbPassword};AllowPublicKeyRetrieval=true;";

    public static AppConfig Parse(string[] args)
    {
        var config = new AppConfig();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--host" when i + 1 < args.Length: config.Host = args[++i]; break;
                case "--port" when i + 1 < args.Length: config.Port = int.Parse(args[++i]); break;
                case "--db-host" when i + 1 < args.Length: config.DbHost = args[++i]; break;
                case "--db-port" when i + 1 < args.Length: config.DbPort = int.Parse(args[++i]); break;
                case "--db-name" when i + 1 < args.Length: config.DbName = args[++i]; break;
                case "--db-user" when i + 1 < args.Length: config.DbUser = args[++i]; break;
                case "--db-password" when i + 1 < args.Length: config.DbPassword = args[++i]; break;
                case "--migrate": config.RunMigration = true; break;
            }
        }
        return config;
    }

    public static void PrintUsage()
    {
        Console.Error.WriteLine("Usage: mywebapp [--migrate] [--host HOST] [--port PORT]");
        Console.Error.WriteLine("--db-host HOST --db-port PORT");
        Console.Error.WriteLine("--db-name NAME --db-user USER --db-password PASS");
        Console.Error.WriteLine();
        Console.Error.WriteLine("--migrate Run DB migration and exit");
        Console.Error.WriteLine("--host Bind address (default: 127.0.0.1)");
        Console.Error.WriteLine("--port Listen port  (default: 3000)");
    }
}