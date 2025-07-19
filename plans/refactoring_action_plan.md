# План действий по рефакторингу

## Этап 1: Устранение дублирования кода (2-3 дня)

### Шаг 1.1: Создание утилитарных классов

#### 1.1.1 Создать UserUtils.cs
```bash
# Создать файл
touch ClubDoorman/Infrastructure/UserUtils.cs
```

**Содержимое UserUtils.cs:**
```csharp
using Telegram.Bot.Types;

namespace ClubDoorman.Infrastructure;

public static class UserUtils
{
    public static string FullName(string firstName, string? lastName) =>
        $"{firstName} {lastName}".Trim();
        
    public static string FullName(User user) =>
        FullName(user.FirstName, user.LastName);
        
    public static string AdminDisplayName(User user) =>
        !string.IsNullOrEmpty(user.FirstName)
            ? FullName(user.FirstName, user.LastName)
            : (!string.IsNullOrEmpty(user.Username) ? "@" + user.Username : "гость");
}
```

#### 1.1.2 Создать MessageUtils.cs
```bash
# Создать файл
touch ClubDoorman/Infrastructure/MessageUtils.cs
```

**Содержимое MessageUtils.cs:**
```csharp
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Infrastructure;

public static class MessageUtils
{
    public static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup || chat.Type == ChatType.Group
            ? LinkToSuperGroupMessage(chat, messageId)
            : LinkToGroupWithNameMessage(chat, messageId);
            
    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => 
        $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";
        
    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => 
        $"https://t.me/{chat.Username}/{messageId}";
        
    public static string GetChatLink(Chat chat) =>
        !string.IsNullOrEmpty(chat.Username)
            ? $"https://t.me/{chat.Username}"
            : $"https://t.me/c/{chat.Id.ToString()[4..]}";
}
```

### Шаг 1.2: Замена дублированных методов

#### 1.2.1 Обновить MessageHandler.cs
```bash
# Найти и заменить все дублированные методы
grep -n "FullName\|LinkToMessage" ClubDoorman/Handlers/MessageHandler.cs
```

**Заменить:**
```csharp
// УДАЛИТЬ эти методы:
private static string FullName(string firstName, string? lastName) => ...
private static string LinkToMessage(Chat chat, long messageId) => ...
private static string LinkToSuperGroupMessage(Chat chat, long messageId) => ...
private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => ...

// ДОБАВИТЬ using:
using ClubDoorman.Infrastructure;

// ЗАМЕНИТЬ все вызовы на:
UserUtils.FullName(...)
MessageUtils.LinkToMessage(...)
```

#### 1.2.2 Обновить Worker.cs
```bash
# Аналогично для Worker.cs
grep -n "FullName\|LinkToMessage" ClubDoorman/Worker.cs
```

#### 1.2.3 Обновить остальные файлы
```bash
# Обновить все файлы с дублированными методами
find ClubDoorman -name "*.cs" -exec grep -l "FullName\|LinkToMessage" {} \;
```

### Шаг 1.3: Тестирование
```bash
# Запустить тесты
dotnet test

# Проверить компиляцию
dotnet build
```

## Этап 2: Разделение MessageHandler.cs (1 неделя)

### Шаг 2.1: Создание структуры папок
```bash
# Создать новые папки
mkdir -p ClubDoorman/Handlers/MessageHandlers
mkdir -p ClubDoorman/Handlers/MessageActions
```

### Шаг 2.2: Создание интерфейсов

#### 2.2.1 Создать IMessageHandlerStrategy.cs
```csharp
namespace ClubDoorman.Handlers.MessageHandlers;

public interface IMessageHandlerStrategy
{
    bool CanHandle(Message message);
    Task HandleAsync(Message message, CancellationToken cancellationToken);
}
```

#### 2.2.2 Создать IMessageAction.cs
```csharp
namespace ClubDoorman.Handlers.MessageActions;

public interface IMessageAction
{
    Task ExecuteAsync(Message message, string reason, CancellationToken cancellationToken);
}
```

### Шаг 2.3: Создание конкретных обработчиков

#### 2.3.1 Создать CommandMessageHandler.cs
```csharp
namespace ClubDoorman.Handlers.MessageHandlers;

public class CommandMessageHandler : IMessageHandlerStrategy
{
    private readonly TelegramBotClient _bot;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandMessageHandler> _logger;

    public CommandMessageHandler(
        TelegramBotClient bot,
        IServiceProvider serviceProvider,
        ILogger<CommandMessageHandler> logger)
    {
        _bot = bot;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public bool CanHandle(Message message) =>
        message.Text?.StartsWith("/") == true;

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        var commandText = message.Text!.Split(' ')[0].ToLower();
        var command = commandText.StartsWith("/") ? commandText.Substring(1) : commandText;

        if (command == "start")
        {
            var startHandler = _serviceProvider.GetRequiredService<StartCommandHandler>();
            await startHandler.HandleAsync(message, cancellationToken);
            return;
        }

        // Админские команды
        var isAdminChat = message.Chat.Id == Config.AdminChatId || message.Chat.Id == Config.LogAdminChatId;
        if (isAdminChat && message.ReplyToMessage != null && (command == "spam" || command == "ham" || command == "check"))
        {
            await HandleAdminCommandAsync(message, command, cancellationToken);
        }
    }

    private async Task HandleAdminCommandAsync(Message message, string command, CancellationToken cancellationToken)
    {
        // Логика обработки админских команд
    }
}
```

#### 2.3.2 Создать UserMessageHandler.cs
```csharp
namespace ClubDoorman.Handlers.MessageHandlers;

public class UserMessageHandler : IMessageHandlerStrategy
{
    private readonly TelegramBotClient _bot;
    private readonly IModerationService _moderationService;
    private readonly IUserManager _userManager;
    private readonly ILogger<UserMessageHandler> _logger;

    public UserMessageHandler(
        TelegramBotClient bot,
        IModerationService moderationService,
        IUserManager userManager,
        ILogger<UserMessageHandler> logger)
    {
        _bot = bot;
        _moderationService = moderationService;
        _userManager = userManager;
        _logger = logger;
    }

    public bool CanHandle(Message message) =>
        message.From != null && message.SenderChat == null;

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        // Логика обработки сообщений пользователей
        // Вынести из HandleUserMessageAsync
    }
}
```

#### 2.3.3 Создать ChannelMessageHandler.cs
```csharp
namespace ClubDoorman.Handlers.MessageHandlers;

public class ChannelMessageHandler : IMessageHandlerStrategy
{
    private readonly TelegramBotClient _bot;
    private readonly ILogger<ChannelMessageHandler> _logger;

    public ChannelMessageHandler(
        TelegramBotClient bot,
        ILogger<ChannelMessageHandler> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public bool CanHandle(Message message) =>
        message.SenderChat != null;

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        // Логика обработки сообщений каналов
        // Вынести из HandleChannelMessageAsync
    }
}
```

#### 2.3.4 Создать NewMemberHandler.cs
```csharp
namespace ClubDoorman.Handlers.MessageHandlers;

public class NewMemberHandler : IMessageHandlerStrategy
{
    private readonly TelegramBotClient _bot;
    private readonly ICaptchaService _captchaService;
    private readonly IUserManager _userManager;
    private readonly ILogger<NewMemberHandler> _logger;

    public NewMemberHandler(
        TelegramBotClient bot,
        ICaptchaService captchaService,
        IUserManager userManager,
        ILogger<NewMemberHandler> logger)
    {
        _bot = bot;
        _captchaService = captchaService;
        _userManager = userManager;
        _logger = logger;
    }

    public bool CanHandle(Message message) =>
        message.NewChatMembers != null;

    public async Task HandleAsync(Message message, CancellationToken cancellationToken)
    {
        // Логика обработки новых участников
        // Вынести из HandleNewMembersAsync
    }
}
```

### Шаг 2.4: Создание действий

#### 2.4.1 Создать DeleteAndReportAction.cs
```csharp
namespace ClubDoorman.Handlers.MessageActions;

public class DeleteAndReportAction : IMessageAction
{
    private readonly TelegramBotClient _bot;
    private readonly ILogger<DeleteAndReportAction> _logger;

    public DeleteAndReportAction(
        TelegramBotClient bot,
        ILogger<DeleteAndReportAction> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public async Task ExecuteAsync(Message message, string reason, CancellationToken cancellationToken)
    {
        // Логика удаления и репорта
        // Вынести из DeleteAndReportMessage
    }
}
```

### Шаг 2.5: Рефакторинг основного MessageHandler

#### 2.5.1 Обновить MessageHandler.cs
```csharp
namespace ClubDoorman.Handlers;

public class MessageHandler : IUpdateHandler
{
    private readonly IEnumerable<IMessageHandlerStrategy> _strategies;
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(
        IEnumerable<IMessageHandlerStrategy> strategies,
        ILogger<MessageHandler> logger)
    {
        _strategies = strategies;
        _logger = logger;
    }

    public bool CanHandle(Update update)
    {
        return update.Message != null || update.EditedMessage != null;
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        var message = update.EditedMessage ?? update.Message!;
        var chat = message.Chat;

        // Проверка whitelist
        var isAdminChat = chat.Id == Config.AdminChatId || chat.Id == Config.LogAdminChatId;
        if (!Config.IsChatAllowed(chat.Id) && !isAdminChat)
        {
            _logger.LogDebug("Чат {ChatId} не в whitelist - игнорируем", chat.Id);
            return;
        }

        // Игнорировать отключённые чаты
        if (Config.DisabledChats.Contains(chat.Id))
            return;

        // Автоматически добавляем чат в конфиг
        ChatSettingsManager.EnsureChatInConfig(chat.Id, chat.Title);

        // Удаление сообщений о бане ботом
        if (message.LeftChatMember != null && message.From?.Id == _bot.BotId)
        {
            await HandleBotBanMessage(message, cancellationToken);
            return;
        }

        // Найти подходящий обработчик
        var handler = _strategies.FirstOrDefault(s => s.CanHandle(message));
        if (handler != null)
        {
            await handler.HandleAsync(message, cancellationToken);
        }
    }

    private async Task HandleBotBanMessage(Message message, CancellationToken cancellationToken)
    {
        // Логика удаления сообщений о бане
    }
}
```

### Шаг 2.6: Обновление DI

#### 2.6.1 Обновить Program.cs
```csharp
// Добавить регистрацию новых сервисов
builder.Services.AddScoped<IMessageHandlerStrategy, CommandMessageHandler>();
builder.Services.AddScoped<IMessageHandlerStrategy, UserMessageHandler>();
builder.Services.AddScoped<IMessageHandlerStrategy, ChannelMessageHandler>();
builder.Services.AddScoped<IMessageHandlerStrategy, NewMemberHandler>();

builder.Services.AddScoped<IMessageAction, DeleteAndReportAction>();
```

## Этап 3: Разделение Worker.cs (1 неделя)

### Шаг 3.1: Создание структуры папок
```bash
mkdir -p ClubDoorman/Worker/Loops
mkdir -p ClubDoorman/Worker/Handlers
```

### Шаг 3.2: Создание интерфейсов циклов

#### 3.2.1 Создать ILoop.cs
```csharp
namespace ClubDoorman.Worker.Loops;

public interface ILoop
{
    Task StartAsync(CancellationToken cancellationToken);
}
```

### Шаг 3.3: Создание конкретных циклов

#### 3.3.1 Создать CaptchaLoop.cs
```csharp
namespace ClubDoorman.Worker.Loops;

public class CaptchaLoop : ILoop
{
    private readonly ICaptchaService _captchaService;
    private readonly ILogger<CaptchaLoop> _logger;

    public CaptchaLoop(
        ICaptchaService captchaService,
        ILogger<CaptchaLoop> logger)
    {
        _captchaService = captchaService;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            await _captchaService.BanExpiredCaptchaUsersAsync();
        }
    }
}
```

#### 3.3.2 Создать BanlistRefreshLoop.cs
```csharp
namespace ClubDoorman.Worker.Loops;

public class BanlistRefreshLoop : ILoop
{
    private readonly IUserManager _userManager;
    private readonly ILogger<BanlistRefreshLoop> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(12));

    public BanlistRefreshLoop(
        IUserManager userManager,
        ILogger<BanlistRefreshLoop> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Начальное обновление
        try 
        {
            _logger.LogInformation("Начальное обновление банлиста");
            await _userManager.RefreshBanlist();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при начальном обновлении банлиста");
        }

        // Периодическое обновление
        while (await _timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                _logger.LogInformation("Обновляем банлист из lols.bot");
                await _userManager.RefreshBanlist();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении банлиста");
            }
        }
    }
}
```

### Шаг 3.4: Рефакторинг основного Worker

#### 3.4.1 Обновить Worker.cs
```csharp
namespace ClubDoorman;

internal sealed class Worker : BackgroundService
{
    private readonly TelegramBotClient _bot = new(Config.BotApi);
    private readonly IEnumerable<ILoop> _loops;
    private readonly IUpdateDispatcher _updateDispatcher;
    private readonly ILogger<Worker> _logger;
    private User _me = default!;

    public Worker(
        IEnumerable<ILoop> loops,
        IUpdateDispatcher updateDispatcher,
        ILogger<Worker> logger)
    {
        _loops = loops;
        _updateDispatcher = updateDispatcher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ChatSettingsManager.InitConfigFileIfMissing();
        
        // Запуск всех циклов
        var loopTasks = _loops.Select(loop => loop.StartAsync(stoppingToken));
        
        // Основной цикл обработки
        var mainLoopTask = MainUpdateLoop(stoppingToken);
        
        // Ожидание всех задач
        await Task.WhenAll(loopTasks.Append(mainLoopTask));
    }

    private async Task MainUpdateLoop(CancellationToken stoppingToken)
    {
        const string offsetPath = "data/offset.txt";
        var offset = LoadOffset(offsetPath);
        
        _me = await _bot.GetMe(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                offset = await UpdateLoop(offset, stoppingToken);
                if (offset % 100 == 0)
                    await SaveOffset(offsetPath, offset, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "ExecuteAsync");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }

    private int LoadOffset(string offsetPath)
    {
        if (File.Exists(offsetPath))
        {
            var lines = File.ReadAllLines(offsetPath);
            if (lines.Length > 0 && int.TryParse(lines[0], out var offset))
                return offset;
        }
        return 0;
    }

    private async Task SaveOffset(string offsetPath, int offset, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(offsetPath, offset.ToString(), cancellationToken);
    }

    private async Task<int> UpdateLoop(int offset, CancellationToken stoppingToken)
    {
        var updates = await _bot.GetUpdates(
            offset,
            limit: 100,
            timeout: 100,
            allowedUpdates: [UpdateType.Message, UpdateType.EditedMessage, UpdateType.ChatMember, UpdateType.CallbackQuery],
            cancellationToken: stoppingToken
        );
        
        if (updates.Length == 0)
            return offset;
            
        offset = updates.Max(x => x.Id) + 1;
        
        foreach (var update in updates)
        {
            try
            {
                await _updateDispatcher.DispatchAsync(update, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "UpdateLoop");
            }
        }
        
        return offset;
    }
}
```

## Этап 4: Разделение Config.cs (2-3 дня)

### Шаг 4.1: Создание структуры папок
```bash
mkdir -p ClubDoorman/Infrastructure/Config
```

### Шаг 4.2: Создание отдельных конфигурационных классов

#### 4.2.1 Создать FeatureFlags.cs
```csharp
namespace ClubDoorman.Infrastructure.Config;

public static class FeatureFlags
{
    public static bool BlacklistAutoBan { get; } = !GetEnvironmentBool("DOORMAN_BLACKLIST_AUTOBAN_DISABLE");
    public static bool ChannelAutoBan { get; } = !GetEnvironmentBool("DOORMAN_CHANNELS_AUTOBAN_DISABLE");
    public static bool LookAlikeAutoBan { get; } = !GetEnvironmentBool("DOORMAN_LOOKALIKE_AUTOBAN_DISABLE");
    public static bool LowConfidenceHamForward { get; } = GetEnvironmentBool("DOORMAN_LOW_CONFIDENCE_HAM_ENABLE");
    public static bool ApproveButtonEnabled { get; } = GetEnvironmentBool("DOORMAN_APPROVE_BUTTON");
    public static bool ButtonAutoBan { get; } = !GetEnvironmentBool("DOORMAN_BUTTON_AUTOBAN_DISABLE");
    public static bool HighConfidenceAutoBan { get; } = !GetEnvironmentBool("DOORMAN_HIGH_CONFIDENCE_AUTOBAN_DISABLE");
    public static bool GlobalApprovalMode { get; } = !GetEnvironmentBool("DOORMAN_GROUP_APPROVAL_MODE");
    public static bool UseNewApprovalSystem { get; } = GetEnvironmentBool("DOORMAN_USE_NEW_APPROVAL_SYSTEM");
    public static bool DisableMediaFiltering { get; } = GetEnvironmentBool("DOORMAN_DISABLE_MEDIA_FILTERING");

    private static bool GetEnvironmentBool(string envName) =>
        Environment.GetEnvironmentVariable(envName)?.ToLowerInvariant() is "true" or "1" or "yes";
}
```

#### 4.2.2 Создать ChatConfig.cs
```csharp
namespace ClubDoorman.Infrastructure.Config;

public static class ChatConfig
{
    public static HashSet<long> DisabledChats { get; } = ParseChatIds("DOORMAN_DISABLED_CHATS");
    public static HashSet<long> WhitelistChats { get; } = ParseChatIds("DOORMAN_WHITELIST");
    public static HashSet<long> NoVpnAdGroups { get; } = ParseChatIds("NO_VPN_AD_GROUPS");
    public static HashSet<long> AiEnabledChats { get; } = ParseChatIds("DOORMAN_AI_ENABLED_CHATS");
    public static HashSet<long> MediaFilteringDisabledChats { get; } = ParseChatIds("DOORMAN_MEDIA_FILTERING_DISABLED_CHATS");

    public static bool IsChatAllowed(long chatId) =>
        WhitelistChats.Count == 0 || WhitelistChats.Contains(chatId);

    public static bool IsPrivateStartAllowed() =>
        WhitelistChats.Count == 0 || WhitelistChats.Contains(0);

    public static bool IsAiEnabledForChat(long chatId) =>
        AiEnabledChats.Count == 0 || AiEnabledChats.Contains(chatId);

    public static bool IsMediaFilteringDisabledForChat(long chatId) =>
        DisableMediaFiltering || MediaFilteringDisabledChats.Contains(chatId);

    private static HashSet<long> ParseChatIds(string envVar)
    {
        var value = Environment.GetEnvironmentVariable(envVar);
        if (string.IsNullOrEmpty(value))
            return new HashSet<long>();

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => long.TryParse(id.Trim(), out var val) ? val : (long?)null)
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .ToHashSet();
    }
}
```

#### 4.2.3 Создать EnvironmentConfig.cs
```csharp
namespace ClubDoorman.Infrastructure.Config;

public static class EnvironmentConfig
{
    public static string BotApi { get; } = Environment.GetEnvironmentVariable("DOORMAN_BOT_API") 
        ?? throw new InvalidOperationException("DOORMAN_BOT_API is required");

    public static long AdminChatId { get; } = long.TryParse(
        Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT"), 
        out var adminChat) ? adminChat : throw new InvalidOperationException("DOORMAN_ADMIN_CHAT is required");

    public static long LogAdminChatId { get; } = GetLogAdminChatId();

    public static string? ClubServiceToken { get; } = Environment.GetEnvironmentVariable("DOORMAN_CLUB_SERVICE_TOKEN");

    public static string ClubUrl { get; } = GetClubUrlOrDefault();

    public static string? OpenRouterApi { get; } = Environment.GetEnvironmentVariable("DOORMAN_OPENROUTER_API");

    private static long GetLogAdminChatId()
    {
        var logChatVar = Environment.GetEnvironmentVariable("DOORMAN_LOG_ADMIN_CHAT");
        if (string.IsNullOrEmpty(logChatVar))
            return AdminChatId;

        return long.TryParse(logChatVar, out var logChatId) ? logChatId : AdminChatId;
    }

    private static string GetClubUrlOrDefault()
    {
        var clubUrl = Environment.GetEnvironmentVariable("DOORMAN_CLUB_URL");
        return !string.IsNullOrEmpty(clubUrl) ? clubUrl : "https://vas3k.club/";
    }
}
```

### Шаг 4.3: Обновление основного Config.cs

#### 4.3.1 Упростить Config.cs
```csharp
namespace ClubDoorman.Infrastructure;

internal static class Config
{
    // Делегирование к новым классам
    public static bool BlacklistAutoBan => FeatureFlags.BlacklistAutoBan;
    public static bool ChannelAutoBan => FeatureFlags.ChannelAutoBan;
    public static bool LookAlikeAutoBan => FeatureFlags.LookAlikeAutoBan;
    public static bool LowConfidenceHamForward => FeatureFlags.LowConfidenceHamForward;
    public static bool ApproveButtonEnabled => FeatureFlags.ApproveButtonEnabled;
    public static bool ButtonAutoBan => FeatureFlags.ButtonAutoBan;
    public static bool HighConfidenceAutoBan => FeatureFlags.HighConfidenceAutoBan;
    public static bool GlobalApprovalMode => FeatureFlags.GlobalApprovalMode;
    public static bool UseNewApprovalSystem => FeatureFlags.UseNewApprovalSystem;
    public static bool DisableMediaFiltering => FeatureFlags.DisableMediaFiltering;

    public static string BotApi => EnvironmentConfig.BotApi;
    public static long AdminChatId => EnvironmentConfig.AdminChatId;
    public static long LogAdminChatId => EnvironmentConfig.LogAdminChatId;
    public static string? ClubServiceToken => EnvironmentConfig.ClubServiceToken;
    public static string ClubUrl => EnvironmentConfig.ClubUrl;
    public static string? OpenRouterApi => EnvironmentConfig.OpenRouterApi;

    public static HashSet<long> DisabledChats => ChatConfig.DisabledChats;
    public static HashSet<long> WhitelistChats => ChatConfig.WhitelistChats;
    public static HashSet<long> NoVpnAdGroups => ChatConfig.NoVpnAdGroups;
    public static HashSet<long> AiEnabledChats => ChatConfig.AiEnabledChats;
    public static HashSet<long> MediaFilteringDisabledChats => ChatConfig.MediaFilteringDisabledChats;

    public static bool IsChatAllowed(long chatId) => ChatConfig.IsChatAllowed(chatId);
    public static bool IsPrivateStartAllowed() => ChatConfig.IsPrivateStartAllowed();
    public static bool IsAiEnabledForChat(long chatId) => ChatConfig.IsAiEnabledForChat(chatId);
    public static bool IsMediaFilteringDisabledForChat(long chatId) => ChatConfig.IsMediaFilteringDisabledForChat(chatId);
}
```

## Этап 5: Тестирование и проверка (1 день)

### Шаг 5.1: Компиляция
```bash
dotnet build
```

### Шаг 5.2: Запуск тестов
```bash
dotnet test
```

### Шаг 5.3: Проверка метрик
```bash
# Проверить размеры файлов
find ClubDoorman -name "*.cs" -exec wc -l {} + | sort -nr

# Проверить количество функций
find ClubDoorman -name "*.cs" -exec sh -c 'echo "=== $1 ==="; grep -c "public\|private\|internal" "$1"' _ {} \;
```

## Ожидаемые результаты

### До рефакторинга:
- MessageHandler.cs: 1034 строки, 40 функций
- Worker.cs: 736 строк, 39 функций
- Config.cs: 278 строк, 41 функция

### После рефакторинга:
- MessageHandler.cs: ~150 строк, ~10 функций
- Worker.cs: ~200 строк, ~15 функций
- Config.cs: ~50 строк, ~10 функций
- Новые файлы: 15-20 файлов по 50-150 строк каждый

### Устранение дублирования:
- Удалено 6 дублированных FullName()
- Удалено 3 дублированных LinkToMessage()
- Создано 2 утилитарных класса

## Риски и митигация

### Риски:
1. **Временная нестабильность** - рефакторинг может ввести баги
2. **Сложность тестирования** - нужно обновить тесты
3. **Время разработки** - рефакторинг займет значительное время

### Митигация:
1. **Поэтапный рефакторинг** - делать изменения небольшими частями
2. **Полное покрытие тестами** - писать тесты перед рефакторингом
3. **Code review** - тщательная проверка каждого изменения
4. **Feature flags** - возможность отката изменений 