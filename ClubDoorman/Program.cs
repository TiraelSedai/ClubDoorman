using Serilog;
using Serilog.Events;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.Handlers;
using ClubDoorman.Handlers.Commands;
using Telegram.Bot;

namespace ClubDoorman;

public class Program
{
    public static async Task Main(string[] args)
    {
        InitData();
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog(
                (_, _, config) =>
                {
                    config
                        .MinimumLevel.Verbose()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .MinimumLevel.Override("System", LogEventLevel.Information)
                        .Enrich.FromLogContext()
                        .WriteTo.Async(a => a.Console());
                }
            )
            .ConfigureServices(services =>
            {
                services.AddHostedService<Worker>();
                
                // Telegram Bot Client
                services.AddSingleton(provider => new TelegramBotClient(Config.BotApi));
                
                // Классификаторы и менеджеры
                services.AddSingleton<SpamHamClassifier>();
                services.AddSingleton<BadMessageManager>();
                services.AddSingleton<AiChecks>();
                services.AddSingleton<GlobalStatsManager>();
                
                // Новые сервисы
                services.AddSingleton<IUpdateDispatcher, UpdateDispatcher>();
                services.AddSingleton<IStatisticsService, StatisticsService>();
                services.AddSingleton<ICaptchaService, CaptchaService>();
                services.AddSingleton<IModerationService, ModerationService>();
                services.AddSingleton<IntroFlowService>();
                
                // Обработчики обновлений
                services.AddSingleton<IUpdateHandler, MessageHandler>();
                services.AddSingleton<IUpdateHandler, CallbackQueryHandler>();
                services.AddSingleton<IUpdateHandler, ChatMemberHandler>();
                
                // Обработчики команд
                services.AddSingleton<ICommandHandler, StartCommandHandler>();
                
                // Условная регистрация системы одобрения
                if (Config.UseNewApprovalSystem)
                {
                    services.AddSingleton<ApprovedUsersStorageV2>();
                    services.AddSingleton<UserManagerV2>();
                    services.AddSingleton<IUserManager>(provider => provider.GetRequiredService<UserManagerV2>());
                }
                else
                {
                    services.AddSingleton<ApprovedUsersStorage>();
                    services.AddSingleton<UserManager>();
                    services.AddSingleton<IUserManager>(provider => provider.GetRequiredService<UserManager>());
                }
            })
            .Build();

        await host.RunAsync();
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
