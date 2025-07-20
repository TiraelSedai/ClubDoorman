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
                services.AddSingleton<TelegramBotClient>(provider => new TelegramBotClient(Config.BotApi));
                services.AddSingleton<ITelegramBotClient>(provider => provider.GetRequiredService<TelegramBotClient>());
                services.AddSingleton<ITelegramBotClientWrapper>(provider => new TelegramBotClientWrapper(provider.GetRequiredService<TelegramBotClient>()));
                
                // Классификаторы и менеджеры
                services.AddSingleton<ISpamHamClassifier, SpamHamClassifier>();
                services.AddSingleton<IMimicryClassifier, MimicryClassifier>();
                services.AddSingleton<IBadMessageManager, BadMessageManager>();
                services.AddSingleton<IAiChecks>(provider => new AiChecks(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ILogger<AiChecks>>()));
                services.AddSingleton<GlobalStatsManager>();
                services.AddSingleton<ISuspiciousUsersStorage, SuspiciousUsersStorage>();
                
                // Новые сервисы
                services.AddSingleton<IUpdateDispatcher, UpdateDispatcher>();
                services.AddSingleton<IStatisticsService>(provider => new StatisticsService(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ILogger<StatisticsService>>(), provider.GetRequiredService<IChatLinkFormatter>()));
                services.AddSingleton<ICaptchaService, CaptchaService>();
                services.AddSingleton<IModerationService, ModerationService>();
                services.AddSingleton<IntroFlowService>(provider => new IntroFlowService(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ILogger<IntroFlowService>>(), provider.GetRequiredService<ICaptchaService>(), provider.GetRequiredService<IUserManager>(), provider.GetRequiredService<IAiChecks>(), provider.GetRequiredService<IStatisticsService>(), provider.GetRequiredService<GlobalStatsManager>(), provider.GetRequiredService<IModerationService>()));
                services.AddSingleton<IChatLinkFormatter, ChatLinkFormatter>();
                
                // Обработчики обновлений
                services.AddSingleton<IUpdateHandler, MessageHandler>();
                services.AddSingleton<IUpdateHandler>(provider => new CallbackQueryHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ICaptchaService>(), provider.GetRequiredService<IUserManager>(), provider.GetRequiredService<IBadMessageManager>(), provider.GetRequiredService<IStatisticsService>(), provider.GetRequiredService<IAiChecks>(), provider.GetRequiredService<IModerationService>(), provider.GetRequiredService<ILogger<CallbackQueryHandler>>()));
                services.AddSingleton<IUpdateHandler>(provider => new ChatMemberHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<IUserManager>(), provider.GetRequiredService<ILogger<ChatMemberHandler>>(), provider.GetRequiredService<IntroFlowService>()));
                
                // Обработчики команд
                services.AddSingleton<ICommandHandler>(provider => new StartCommandHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ILogger<StartCommandHandler>>()));
                services.AddSingleton<StartCommandHandler>(provider => new StartCommandHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ILogger<StartCommandHandler>>()));
                services.AddSingleton<ICommandHandler>(provider => new SuspiciousCommandHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<IModerationService>(), provider.GetRequiredService<ILogger<SuspiciousCommandHandler>>()));
                services.AddSingleton<SuspiciousCommandHandler>(provider => new SuspiciousCommandHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<IModerationService>(), provider.GetRequiredService<ILogger<SuspiciousCommandHandler>>()));
                
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
