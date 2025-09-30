
using BenchmarkDotNet.Loggers;
using Core.Enums;
using Core.Models;
using Core.Options;
using GroundLayerLibrary;
using GroundLayerLibrary.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObjectLayerLibrary.Interfaces;
using ObjectLayerLibrary.Services;
using StackExchange.Redis;
using static System.Net.Mime.MediaTypeNames;

public class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.test.json", optional: false);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure options
                services.Configure<AppSettings>(
                    context.Configuration.GetSection("AppSettings"));

                services.AddSingleton<ITiledLayerService, GroundLayerService>();
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

public class ConsoleApp(
        IHostApplicationLifetime appLifetime,
        ILogger<ConsoleApp> logger,
        IConnectionMultiplexer redis,
        IObjectLayerService<GameObject> objectLayerService,
        ITiledLayerService tiledLayerService) : IHostedService
{

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Application starting...");

        try
        {
            var server = redis.GetServer(redis.GetEndPoints().First());
            await CleanupGameDataAsync(redis);

            Console.WriteLine($"Уставаливаем типы тайла X: {50}, Y: {50} на тип: {TileTypeEnum.Mountain}");
            tiledLayerService.SetTileType(50, 50, TileTypeEnum.Mountain);
            var tileTypeTest1 = tiledLayerService.GetTileType(50, 50);
            Console.WriteLine($"Получаем тип тайла X: {50}, Y: {50}. Он равен: {tileTypeTest1}");

            Console.WriteLine();

            Console.WriteLine($"Уставаливаем типы тайла X: {50}, Y: {50} на тип: {TileTypeEnum.Plain}");
            tiledLayerService.SetTileType(50, 50, TileTypeEnum.Plain);
            var tileTypeTest2 = tiledLayerService.GetTileType(50, 50);
            Console.WriteLine($"Получаем тип тайла X: {50}, Y: {50}. Он равен: {tileTypeTest2}");
            Console.ReadKey();

            Console.WriteLine();

            Console.WriteLine($"Уставаливаем типы тайла на RANGE X: {50}, Y: {50} до X: {100}, Y: {100} на тип: {TileTypeEnum.Mountain}");
            tiledLayerService.SetTileTypeRange(50, 50, 100, 100, TileTypeEnum.Mountain);
            var tileTypeTest3 = tiledLayerService.GetTileType(90, 90);
            Console.WriteLine($"Получаем тип тайла X: {90}, Y: {90}. Он равен: {tileTypeTest3}");

            Console.WriteLine();

            Console.WriteLine($"Уставаливаем типы тайла на RANGE X: {50}, Y: {50} до X: {100}, Y: {100} на тип: {TileTypeEnum.Plain}");
            tiledLayerService.SetTileTypeRange(50, 50, 100, 100, TileTypeEnum.Plain);
            var tileTypeTest4 = tiledLayerService.GetTileType(90, 90);
            Console.WriteLine($"Получаем тип тайла X: {90}, Y: {90}. Он равен: {tileTypeTest4}");
            Console.ReadKey();

            Console.WriteLine();

            Console.WriteLine($"Уставаливаем типы тайла на RANGE X: {50}, Y: {50} до X: {100}, Y: {100} на тип: {TileTypeEnum.Mountain}");
            tiledLayerService.SetTileTypeRange(50, 50, 100, 100, TileTypeEnum.Mountain);
            var canPlaceObjectTest1 = tiledLayerService.CanPlaceObjectInArea(50, 50, 100, 100);
            Console.WriteLine($"Получаем ответ, можно ли распологать тут объект: {canPlaceObjectTest1}");

            Console.WriteLine();

            Console.WriteLine($"Уставаливаем типы тайла на RANGE X: {50}, Y: {50} до X: {100}, Y: {100} на тип: {TileTypeEnum.Plain}");
            tiledLayerService.SetTileTypeRange(50, 50, 100, 100, TileTypeEnum.Plain);
            var canPlaceObjectTest2 = tiledLayerService.CanPlaceObjectInArea(50, 50, 100, 100);
            Console.WriteLine($"Получаем ответ, можно ли распологать тут объект: {canPlaceObjectTest2}");
            Console.ReadKey();

            Console.WriteLine();

            Console.WriteLine($"Добавляем объект по координатам X: {50}, Y: {50}, Длинной: {4}, Шириной: {4}. Ключом: {"test1"}");
            await objectLayerService.AddObjectAsync(new GameObject("test1", 50, 50, 4, 4));
            Console.WriteLine($"Добавляем объект по координатам X: {101}, Y: {101}, Длинной: {4}, Шириной: {4}. Ключом: {"test2"}");
            await objectLayerService.AddObjectAsync(new GameObject("test2", 101, 101, 4, 4));
            var getObjectTest1 = await objectLayerService.GetObjectByCoordinatesAsync(50, 50);
            Console.WriteLine($"Получаем объект по области X: {10}, Y: {10}.  Длинной: {60}, Шириной: {60}. методом GetObjectByCoordinates с ключом: {getObjectTest1.Value.Id}");
            var getObjectTest2 = await objectLayerService.GetObjectsInAreaAsync(10, 10, 60, 60);

            Console.WriteLine($"Получаем массив по координатам X: {10}, Y: {10}, Длинной: {60}, Шириной: {60}. методом GetObjectsInAreaAsync с {getObjectTest2.Count()} элементами");
            for (int i = 0; i < getObjectTest2.Count(); i++)
            {
                Console.WriteLine($"Получаем {i}элемент из массива по координатам X: {10}, Y: {10}, Длинной: {60}, Шириной: {60}. методом GetObjectsInAreaAsync с ключом: {getObjectTest2[i].Id}");
            }

            Console.WriteLine();
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred");
        }
        finally
        {
            appLifetime.StopApplication();
        }

        return;
    }

    public async Task CleanupGameDataAsync(IConnectionMultiplexer redis)
    {
        var db = redis.GetDatabase();
        var server = redis.GetServer(redis.GetEndPoints().First());

        var patterns = new[] { "object*" };

        foreach (var pattern in patterns)
        {
            var keys = server.Keys(pattern: pattern).ToArray();
            if (keys.Length > 0)
            {
                await db.KeyDeleteAsync(keys);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Application stopping...");
        return Task.CompletedTask;
    }
}