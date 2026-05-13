using MyWebApp.Data;
using MyWebApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var config = AppConfig.Parse(args);

        if (config.RunMigration)
        {
            Console.WriteLine("[mywebapp] Running database migration…");
            try
            {
                await DbMigrator.RunAsync(config.ConnectionString);
                Console.WriteLine("[mywebapp] Migration finished successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[mywebapp] Migration failed: {ex.Message}");
                Environment.Exit(1);
            }
            return;
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args });
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        var listenFds = Environment.GetEnvironmentVariable("LISTEN_FDS");
        if (int.TryParse(listenFds, out var fdCount) && fdCount > 0)
        {
            Console.WriteLine($"[mywebapp] Using systemd socket activation ({fdCount} socket(s)).");
            builder.WebHost.UseKestrel(opts =>
            {
                for (var i = 0; i < fdCount; i++)
                    opts.ListenHandle((ulong)(3 + i));
            });
        }
        else
        {
            builder.WebHost.UseKestrel(opts =>
            {
                opts.Listen(System.Net.IPAddress.Parse(config.Host), config.Port);
            });
            Console.WriteLine($"[mywebapp] Listening on http://{config.Host}:{config.Port}");
        }

        builder.Services.AddSingleton(config);
        builder.Services.AddScoped<Database>();
        builder.Services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.PropertyNamingPolicy =
                    System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
            });

        var app = builder.Build();
        app.MapControllers();
        await app.RunAsync();
    }
}