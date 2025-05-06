using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Telegram.Bot;

namespace ClubDoorman;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            InitData();
            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog(
                    (_, _, config) =>
                    {
                        config
                            .MinimumLevel.Debug()
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database", LogEventLevel.Warning)
                            .Enrich.FromLogContext()
                            .WriteTo.Async(a =>
                                a.Console(
                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Scope} {Message:lj}{NewLine}{Exception}"
                                )
                            );
                    }
                )
                .ConfigureServices(services =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(Config.BotApi));
                    services.AddSingleton<CaptchaManager>();
                    services.AddSingleton<MessageProcessor>();
                    services.AddSingleton<StatisticsReporter>();
                    services.AddSingleton<SpamHamClassifier>();
                    services.AddSingleton<UserManager>();
                    services.AddSingleton<AdminCommandHandler>();
                    services.AddSingleton<ReactionHandler>();
                    services.AddSingleton<BadMessageManager>();
                    services.AddSingleton<AiChecks>();
                    services.AddDbContext<AppDbContext>(opts => opts.UseSqlite("Data Source=data/app.db"));
                    services.AddHybridCache();
                })
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            await host.RunAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine("Unhandled exception in Main");
            Console.WriteLine(e.Message);
        }
    }

    private static void InitData()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var dataInit = Path.Combine(basePath, "data_init");
        if (!Directory.Exists(dataInit))
            return;

        var data = Path.Combine(basePath, "data");
        if (!Directory.Exists(data))
            Directory.CreateDirectory(data);
        foreach (var sourceFullPath in Directory.EnumerateFiles(dataInit))
        {
            var file = Path.GetFileName(sourceFullPath);
            var target = Path.Combine(data, file);
            if (!File.Exists(target))
                File.Copy(sourceFullPath, target);
        }
    }
}
