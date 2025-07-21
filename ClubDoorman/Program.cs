using Serilog;
using Serilog.Events;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.Handlers;
using ClubDoorman.Handlers.Commands;
using Telegram.Bot;
using DotNetEnv;

namespace ClubDoorman;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Загружаем переменные из .env файла если он существует
        var currentDir = Directory.GetCurrentDirectory();
        var envPath = Path.Combine(currentDir, ".env");
        
        if (File.Exists(envPath))
        {
            Console.WriteLine($"📄 Загружаем переменные из файла: {envPath}");
            Env.Load(envPath);
        }
        else
        {
            Console.WriteLine("📄 Файл .env не найден, используем переменные окружения");
            Console.WriteLine($"🔍 Искали в: {envPath}");
        }
        
        InitData();
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog(
                (_, _, config) =>
                {
                    // Создаем директорию для логов если её нет
                    var logsDir = "logs";
                    if (!Directory.Exists(logsDir))
                    {
                        Directory.CreateDirectory(logsDir);
                    }
                    
                    config
                        .MinimumLevel.Verbose()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .MinimumLevel.Override("System", LogEventLevel.Information)
                        .Enrich.FromLogContext()
                        .Enrich.WithProperty("Application", "ClubDoorman")
                        .WriteTo.Async(a => a.Console())
                        .WriteTo.Async(a => a.File(
                            path: Path.Combine(logsDir, "clubdoorman-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                        ))
                        .WriteTo.Async(a => a.File(
                            path: Path.Combine(logsDir, "errors-.log"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            restrictedToMinimumLevel: LogEventLevel.Error,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                        ));
                }
            )
            .ConfigureServices(services =>
            {
                // Проверяем конфигурацию бота
                if (string.IsNullOrEmpty(Config.BotApi))
                {
                    throw new InvalidOperationException(
                        "❌ Бот не может запуститься: DOORMAN_BOT_API не настроен или равен 'test-bot-token'. " +
                        "Установите переменную окружения DOORMAN_BOT_API с валидным токеном бота."
                    );
                }

                Console.WriteLine($"🤖 Запуск бота с токеном: {Config.BotApi.Substring(0, Math.Min(Config.BotApi.Length, 10))}...");
                
                services.AddHostedService<Worker>(provider => new Worker(
                    provider.GetRequiredService<ILogger<Worker>>(),
                    provider.GetRequiredService<IUpdateDispatcher>(),
                    provider.GetRequiredService<ICaptchaService>(),
                    provider.GetRequiredService<IStatisticsService>(),
                    provider.GetRequiredService<ISpamHamClassifier>(),
                    provider.GetRequiredService<IUserManager>(),
                    provider.GetRequiredService<IBadMessageManager>(),
                    provider.GetRequiredService<IAiChecks>(),
                    provider.GetRequiredService<IChatLinkFormatter>(),
                    provider.GetRequiredService<ITelegramBotClientWrapper>(),
                    provider.GetRequiredService<IMessageService>()
                ));
                
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
                services.AddSingleton<ICaptchaService>(provider => new CaptchaService(
                    provider.GetRequiredService<ITelegramBotClientWrapper>(),
                    provider.GetRequiredService<ILogger<CaptchaService>>(),
                    provider.GetRequiredService<IMessageService>()));
                services.AddSingleton<IModerationService>(provider => new ModerationService(
                    provider.GetRequiredService<ISpamHamClassifier>(),
                    provider.GetRequiredService<IMimicryClassifier>(),
                    provider.GetRequiredService<IBadMessageManager>(),
                    provider.GetRequiredService<IUserManager>(),
                    provider.GetRequiredService<IAiChecks>(),
                    provider.GetRequiredService<ISuspiciousUsersStorage>(),
                    provider.GetRequiredService<ITelegramBotClient>(),
                    provider.GetRequiredService<IMessageService>(),
                    provider.GetRequiredService<ILogger<ModerationService>>()));
                services.AddSingleton<IntroFlowService>(provider => new IntroFlowService(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ILogger<IntroFlowService>>(), provider.GetRequiredService<ICaptchaService>(), provider.GetRequiredService<IUserManager>(), provider.GetRequiredService<IAiChecks>(), provider.GetRequiredService<IStatisticsService>(), provider.GetRequiredService<GlobalStatsManager>(), provider.GetRequiredService<IModerationService>(), provider.GetRequiredService<IMessageService>()));
                services.AddSingleton<IChatLinkFormatter, ChatLinkFormatter>();
                services.AddSingleton<IUserFlowLogger, UserFlowLogger>();
                
                // Централизованная система сообщений
                services.AddSingleton<MessageTemplates>();
                services.AddSingleton<IMessageService, MessageService>();
                
                // Обработчики обновлений
                services.AddSingleton<IUpdateHandler>(provider => new MessageHandler(
                    provider.GetRequiredService<ITelegramBotClientWrapper>(),
                    provider.GetRequiredService<IModerationService>(),
                    provider.GetRequiredService<ICaptchaService>(),
                    provider.GetRequiredService<IUserManager>(),
                    provider.GetRequiredService<ISpamHamClassifier>(),
                    provider.GetRequiredService<IBadMessageManager>(),
                    provider.GetRequiredService<IAiChecks>(),
                    provider.GetRequiredService<GlobalStatsManager>(),
                    provider.GetRequiredService<IStatisticsService>(),
                    provider.GetRequiredService<IServiceProvider>(),
                    provider.GetRequiredService<IUserFlowLogger>(),
                    provider.GetRequiredService<IMessageService>(),
                    provider.GetRequiredService<IChatLinkFormatter>(),
                    provider.GetRequiredService<ILogger<MessageHandler>>()));
                services.AddSingleton<IUpdateHandler>(provider => new CallbackQueryHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ICaptchaService>(), provider.GetRequiredService<IUserManager>(), provider.GetRequiredService<IBadMessageManager>(), provider.GetRequiredService<IStatisticsService>(), provider.GetRequiredService<IAiChecks>(), provider.GetRequiredService<IModerationService>(), provider.GetRequiredService<IMessageService>(), provider.GetRequiredService<ILogger<CallbackQueryHandler>>()));
                services.AddSingleton<IUpdateHandler>(provider => new ChatMemberHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<IUserManager>(), provider.GetRequiredService<ILogger<ChatMemberHandler>>(), provider.GetRequiredService<IntroFlowService>(), provider.GetRequiredService<IMessageService>()));
                
                // Обработчики команд
                services.AddSingleton<ICommandHandler>(provider => new StartCommandHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ILogger<StartCommandHandler>>(), provider.GetRequiredService<IMessageService>()));
                services.AddSingleton<StartCommandHandler>(provider => new StartCommandHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<ILogger<StartCommandHandler>>(), provider.GetRequiredService<IMessageService>()));
                services.AddSingleton<ICommandHandler>(provider => new SuspiciousCommandHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<IModerationService>(), provider.GetRequiredService<IMessageService>(), provider.GetRequiredService<ILogger<SuspiciousCommandHandler>>()));
                services.AddSingleton<SuspiciousCommandHandler>(provider => new SuspiciousCommandHandler(provider.GetRequiredService<ITelegramBotClientWrapper>(), provider.GetRequiredService<IModerationService>(), provider.GetRequiredService<IMessageService>(), provider.GetRequiredService<ILogger<SuspiciousCommandHandler>>()));
                
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
                
                // Логируем статус AI и Mimicry систем после полной инициализации
                if (Config.OpenRouterApi != null)
                {
                    Console.WriteLine("🤖 AI анализ: ВКЛЮЧЕН");
                }
                else
                {
                    Console.WriteLine("🤖 AI анализ: ОТКЛЮЧЕН (DOORMAN_OPENROUTER_API не настроен)");
                }
                
                if (Config.SuspiciousDetectionEnabled)
                {
                    Console.WriteLine($"🎭 Система мимикрии: ВКЛЮЧЕНА (порог: {Config.MimicryThreshold:F1})");
                }
                else
                {
                    Console.WriteLine("🎭 Система мимикрии: ОТКЛЮЧЕНА (DOORMAN_SUSPICIOUS_DETECTION_ENABLE не установлен)");
                }
                
                // Информация о загруженных переменных окружения
                Console.WriteLine("📋 Загруженные переменные окружения:");
                Console.WriteLine($"   • DOORMAN_BOT_API: {(string.IsNullOrEmpty(Config.BotApi) ? "не найдено" : "найдено")}");
                Console.WriteLine($"   • DOORMAN_ADMIN_CHAT: {Config.AdminChatId}");
                Console.WriteLine($"   • DOORMAN_LOG_ADMIN_CHAT: {Config.LogAdminChatId}");
                Console.WriteLine($"   • DOORMAN_OPENROUTER_API: {(Config.OpenRouterApi != null ? "найдено" : "не найдено")}");
                Console.WriteLine($"   • DOORMAN_SUSPICIOUS_DETECTION_ENABLE: {Config.SuspiciousDetectionEnabled}");
                Console.WriteLine($"   • DOORMAN_MIMICRY_THRESHOLD: {Config.MimicryThreshold:F1}");
                Console.WriteLine($"   • DOORMAN_SUSPICIOUS_TO_APPROVED_COUNT: {Config.SuspiciousToApprovedMessageCount}");
                Console.WriteLine($"   • DOORMAN_USE_NEW_APPROVAL_SYSTEM: {Config.UseNewApprovalSystem}");
                Console.WriteLine($"   • DOORMAN_GLOBAL_APPROVAL_MODE: {Config.GlobalApprovalMode}");
                Console.WriteLine($"   • DOORMAN_BLACKLIST_AUTOBAN_DISABLE: {!Config.BlacklistAutoBan}");
                Console.WriteLine($"   • DOORMAN_CHANNELS_AUTOBAN_DISABLE: {!Config.ChannelAutoBan}");
                Console.WriteLine($"   • DOORMAN_BUTTON_AUTOBAN_DISABLE: {!Config.ButtonAutoBan}");
                Console.WriteLine($"   • DOORMAN_HIGH_CONFIDENCE_AUTOBAN_DISABLE: {!Config.HighConfidenceAutoBan}");
                Console.WriteLine($"   • DOORMAN_LOW_CONFIDENCE_HAM_ENABLE: {Config.LowConfidenceHamForward}");
                Console.WriteLine($"   • DOORMAN_APPROVE_BUTTON: {Config.ApproveButtonEnabled}");
                Console.WriteLine($"   • DOORMAN_DISABLE_MEDIA_FILTERING: {Config.DisableMediaFiltering}");
                Console.WriteLine($"   • DOORMAN_PRIVATE_START_DISABLE: {!Config.IsPrivateStartAllowed()}");
                Console.WriteLine($"   • Отключенные чаты: {Config.DisabledChats.Count}");
                Console.WriteLine($"   • Белый список чатов: {Config.WhitelistChats.Count}");
                Console.WriteLine($"   • AI-включенные чаты: {Config.AiEnabledChats.Count}");
                Console.WriteLine($"   • Группы без VPN-рекламы: {Config.NoVpnAdGroups.Count}");
                Console.WriteLine($"   • Чаты с отключенной фильтрацией медиа: {Config.MediaFilteringDisabledChats.Count}");
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
