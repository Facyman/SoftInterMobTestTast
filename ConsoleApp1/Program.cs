
using Core.Options;
using GroundLayerLibrary;
using GroundLayerLibrary.Enums;
using GroundLayerLibrary.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObjectLayerLibrary.Interfaces;
using ObjectLayerLibrary.Models;
using ObjectLayerLibrary.Services;
using StackExchange.Redis;

public class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure options
                services.Configure<AppSettings>(
                    context.Configuration.GetSection("AppSettings"));

                services.AddSingleton<ITiledLayer, GroundLayerService>();
                services.AddSingleton<ICoordinateConverterService, CoordinateConverterService>();
                services.AddSingleton<IObjectStoreService<GameObject>, GameObjectStoreService>();
                services.AddSingleton<IObjectLayerService<GameObject>, ObjectLayerService>();
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var configuration = sp.GetRequiredService<IConfiguration>();
                    var redisConnectionString = configuration.GetConnectionString("Redis")
                        ?? "localhost:6379";

                    return ConnectionMultiplexer.Connect(redisConnectionString);
                });

                services.AddHostedService<ConsoleApp>();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
            })
            .UseConsoleLifetime()
            .Build();

        await host.RunAsync();
    }
}

public class ConsoleApp : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<ConsoleApp> _logger;

    public ConsoleApp(
        IHostApplicationLifetime appLifetime,
        ILogger<ConsoleApp> logger)
    {
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Application starting...");

        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred");
        }
        finally
        {
            _appLifetime.StopApplication();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Application stopping...");
        return Task.CompletedTask;
    }
}