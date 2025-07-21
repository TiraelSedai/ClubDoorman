using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.Json;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.Models;

namespace ClubDoorman;

internal sealed class Worker(
    ILogger<Worker> logger,
    IUpdateDispatcher updateDispatcher,
    ICaptchaService captchaService,
    IStatisticsService statisticsService,
    ISpamHamClassifier classifier,
    IUserManager userManager,
    IBadMessageManager badMessageManager,
    IAiChecks aiChecks,
    IChatLinkFormatter chatLinkFormatter,
    ITelegramBotClientWrapper bot
) : BackgroundService
{
    // Классы CaptchaInfo и Stats перенесены в Models

    private readonly ITelegramBotClientWrapper _bot = bot;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));
    private readonly PeriodicTimer _banlistRefreshTimer = new(TimeSpan.FromHours(12));
    private readonly PeriodicTimer _membersCountUpdateTimer = new(TimeSpan.FromHours(8));
    private readonly ILogger<Worker> _logger = logger;
    private readonly IUpdateDispatcher _updateDispatcher = updateDispatcher;
    private readonly ICaptchaService _captchaService = captchaService;
    private readonly IStatisticsService _statisticsService = statisticsService;
    private readonly ISpamHamClassifier _classifier = classifier;
    private readonly IUserManager _userManager = userManager;
    private readonly IBadMessageManager _badMessageManager = badMessageManager;
    private readonly IAiChecks _aiChecks = aiChecks;
    private readonly IChatLinkFormatter _chatLinkFormatter = chatLinkFormatter;
    private readonly GlobalStatsManager _globalStatsManager = new();
    private User _me = default!;
    
            // Группы, где не показывать рекламу (из .env NO_VPN_AD_GROUPS)
        private static readonly HashSet<long> NoVpnAdGroups = 
        (Environment.GetEnvironmentVariable("NO_VPN_AD_GROUPS") ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(id => long.TryParse(id.Trim(), out var val) ? val : (long?)null)
        .Where(id => id.HasValue)
        .Select(id => id.Value)
        .ToHashSet();

    static Worker()
    {
        var envVar = Environment.GetEnvironmentVariable("NO_VPN_AD_GROUPS");
        Console.WriteLine($"[DEBUG] NO_VPN_AD_GROUPS env var: '{envVar}'");
        Console.WriteLine($"[DEBUG] Loaded {NoVpnAdGroups.Count} groups without ads: [{string.Join(", ", NoVpnAdGroups)}]");
        
        var whitelistVar = Environment.GetEnvironmentVariable("DOORMAN_WHITELIST");
        Console.WriteLine($"[DEBUG] DOORMAN_WHITELIST env var: '{whitelistVar}'");
        Console.WriteLine($"[DEBUG] Loaded {Config.WhitelistChats.Count} whitelist groups: [{string.Join(", ", Config.WhitelistChats)}]");
        Console.WriteLine($"[DEBUG] Private /start allowed: {Config.IsPrivateStartAllowed()}");
        
        var logChatVar = Environment.GetEnvironmentVariable("DOORMAN_LOG_ADMIN_CHAT");
        Console.WriteLine($"[DEBUG] DOORMAN_LOG_ADMIN_CHAT env var: '{logChatVar}'");
        Console.WriteLine($"[DEBUG] Log admin chat ID: {Config.LogAdminChatId}");
        Console.WriteLine($"[DEBUG] Using separate log chat: {Config.LogAdminChatId != Config.AdminChatId}");
        
        var testBlacklistVar = Environment.GetEnvironmentVariable("DOORMAN_TEST_BLACKLIST_IDS");
        Console.WriteLine($"[DEBUG] DOORMAN_TEST_BLACKLIST_IDS env var: '{testBlacklistVar}'");
    }

    private async Task CaptchaLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), token);
            await _captchaService.BanExpiredCaptchaUsersAsync();
        }
    }

    private async Task RefreshBanlistLoop(CancellationToken token)
    {
        // Обновляем банлист сразу после запуска бота
        try 
        {
            _logger.LogInformation("Начальное обновление банлиста из lols.bot при старте бота");
            await _userManager.RefreshBanlist();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при начальном обновлении банлиста");
        }

        while (await _banlistRefreshTimer.WaitForNextTickAsync(token))
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

    private async Task UpdateMembersCountLoop(CancellationToken stoppingToken)
    {
        while (await _membersCountUpdateTimer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _globalStatsManager.UpdateAllMembersAsync(_bot);
                _logger.LogInformation("Обновлено количество участников во всех чатах для статистики");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при обновлении количества участников чатов");
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ChatSettingsManager.InitConfigFileIfMissing();
        _ = CaptchaLoop(stoppingToken);
        _ = ReportStatisticsLoop(stoppingToken);
        _ = RefreshBanlistLoop(stoppingToken);
        _ = UpdateMembersCountLoop(stoppingToken);
        _classifier.Touch();
        const string offsetPath = "data/offset.txt";
        var offset = 0;
        if (System.IO.File.Exists(offsetPath))
        {
            var lines = await System.IO.File.ReadAllLinesAsync(offsetPath, stoppingToken);
            if (lines.Length > 0 && int.TryParse(lines[0], out offset))
                _logger.LogDebug("offset read ok");
        }

        _me = await _bot.GetMe(cancellationToken: stoppingToken);
        
        _ = Task.Run(async () => {
            try {
                await _globalStatsManager.UpdateAllMembersAsync(_bot);
                await _globalStatsManager.UpdateZeroMemberChatsAsync(_bot);
                _logger.LogInformation("Первичное обновление количества участников во всех чатах для статистики");
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Ошибка при первичном обновлении количества участников");
            }
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                offset = await UpdateLoop(offset, stoppingToken);
                if (offset % 100 == 0)
                    await System.IO.File.WriteAllTextAsync(offsetPath, offset.ToString(), stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogError(e, "ExecuteAsync");
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
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
        string? mediaGroupId = null;
        foreach (var update in updates)
        {
            try
            {
                var prevMediaGroup = mediaGroupId;
                mediaGroupId = update.Message?.MediaGroupId;
                if (prevMediaGroup != null && prevMediaGroup == mediaGroupId)
                {
                    _logger.LogDebug("2+ message from an album, it could not have any text/caption, skip");
                    continue;
                }
                await _updateDispatcher.DispatchAsync(update, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "UpdateLoop");
            }
        }
        return offset;
    }

    // Метод HandleUpdate удален - логика перенесена в MessageHandler и CallbackQueryHandler через UpdateDispatcher

    private async Task AutoBan(Message message, string reason, CancellationToken stoppingToken)
    {
        var user = message.From;
        var forward = await _bot.ForwardMessage(
            new ChatId(Config.AdminChatId),
            message.Chat.Id,
            message.MessageId,
            cancellationToken: stoppingToken
        );
        await _bot.SendMessage(
            Config.AdminChatId,
            $"Авто-бан: {reason}{Environment.NewLine}Юзер {FullName(user.FirstName, user.LastName)} из чата {message.Chat.Title}{Environment.NewLine}{LinkToMessage(message.Chat, message.MessageId)}",
            replyParameters: forward,
            cancellationToken: stoppingToken
        );
        await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: stoppingToken);
        await _bot.BanChatMember(message.Chat, user.Id, revokeMessages: false, cancellationToken: stoppingToken);
        _globalStatsManager.IncBan(message.Chat.Id, message.Chat.Title ?? "");
        if (_userManager.RemoveApproval(user.Id, message.Chat.Id, removeAll: true))
        {
            await _bot.SendMessage(
                Config.AdminChatId,
                $"⚠️ Пользователь {FullName(user.FirstName, user.LastName)} удален из списка одобренных после автобана",
                cancellationToken: stoppingToken
            );
        }
    }

    private async Task AutoBanWithLogging(Message message, string reason, CancellationToken stoppingToken)
    {
        var user = message.From;
        
        // Пересылаем сообщение в лог-чат перед удалением
        try
        {
            var forward = await _bot.ForwardMessage(
                new ChatId(Config.LogAdminChatId),
                message.Chat.Id,
                message.MessageId,
                cancellationToken: stoppingToken
            );
            
            await _bot.SendMessage(
                Config.LogAdminChatId,
                $"🚫 Автобан из блэклиста: {reason}{Environment.NewLine}Юзер {FullName(user.FirstName, user.LastName)} из чата {message.Chat.Title}{Environment.NewLine}{LinkToMessage(message.Chat, message.MessageId)}",
                replyParameters: forward,
                cancellationToken: stoppingToken
            );
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось переслать сообщение в лог-чат");
        }
        
        // Удаляем сообщение
        try
        {
            await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось удалить сообщение пользователя из блэклиста");
        }
        
        // Баним пользователя
        try
        {
            await _bot.BanChatMember(message.Chat, user.Id, revokeMessages: false, cancellationToken: stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя из блэклиста");
        }
        
        // Обновляем статистику
        _statisticsService.IncrementBlacklistBan(message.Chat.Id);
        _globalStatsManager.IncBan(message.Chat.Id, message.Chat.Title ?? "");
        
        // Удаляем из списка одобренных
        if (_userManager.RemoveApproval(user.Id))
        {
            await _bot.SendMessage(
                Config.AdminChatId,
                $"⚠️ Пользователь {FullName(user.FirstName, user.LastName)} удален из списка одобренных после автобана по блэклисту",
                cancellationToken: stoppingToken
            );
        }
        
        _logger.LogInformation("Автобан по блэклисту: пользователь {User} (id={UserId}) забанен в чате {ChatTitle} (id={ChatId})", 
            FullName(user.FirstName, user.LastName), user.Id, message.Chat.Title, message.Chat.Id);
    }

    private async Task HandleBadMessage(Message message, User user, CancellationToken stoppingToken)
    {
        try
        {
            var chat = message.Chat;
            _statisticsService.IncrementKnownBadMessage(chat.Id);
            await _bot.DeleteMessage(chat, message.MessageId, stoppingToken);
            await _bot.BanChatMember(chat, user.Id, cancellationToken: stoppingToken);
            _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");
            if (_userManager.RemoveApproval(user.Id, message.Chat.Id, removeAll: true))
            {
                await _bot.SendMessage(
                    Config.AdminChatId,
                    $"⚠️ Пользователь {FullName(user.FirstName, user.LastName)} удален из списка одобренных после бана за спам",
                    cancellationToken: stoppingToken
                );
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban");
        }
    }

    // Метод HandleCallback удален - логика перенесена в CallbackQueryHandler

    // Метод HandleCaptchaCallback удален - логика перенесена в CallbackQueryHandler 
    // (начало метода удалено, продолжение удаляется ниже)

    // Оставшаяся часть метода HandleCaptchaCallback удалена

    private readonly List<string> _namesBlacklist = ["p0rn", "porn", "порн", "п0рн", "pоrn", "пoрн", "bot"];

    // УДАЛЕН: BanUserForLongName - логика перенесена в IntroFlowService

        // УДАЛЕН: IntroFlow - логика перенесена в IntroFlowService



    private async Task ReportStatisticsLoop(CancellationToken ct)
    {
        while (await _timer.WaitForNextTickAsync(ct))
        {
            if (DateTimeOffset.UtcNow.Hour != 12)
                continue;

            try
            {
                var report = await _statisticsService.GenerateReportAsync();
                _statisticsService.ClearStats();
                
                await _bot.SendMessage(Config.AdminChatId, report, parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to sent report to admin chat");
            }
        }
    }

    // УДАЛЕН: BanIfBlacklisted - логика перенесена в IntroFlowService

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";



    // УДАЛЕН: HandleChatMemberUpdated - логика перенесена в ChatMemberHandler

    // УДАЛЕН: DontDeleteButReportMessage - логика перенесена в MessageHandler

    // УДАЛЕН: DeleteAndReportMessage - логика перенесена в MessageHandler

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup ? LinkToSuperGroupMessage(chat, messageId)
        : chat.Username == null ? ""
        : LinkToGroupWithNameMessage(chat, messageId);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";

    private async Task AdminChatMessage(Message message)
    {
        if (message is { ReplyToMessage: { } replyToMessage, Text: "/spam" or "/ham" or "/check" })
        {
            if (replyToMessage.From?.Id == _me.Id && replyToMessage.ForwardDate == null)
            {
                await _bot.SendMessage(
                    message.Chat.Id,
                    "⚠️ Похоже что вы промахнулись и реплайнули на сообщение бота, а не форвард",
                    replyParameters: replyToMessage
                );
                return;
            }
            var text = replyToMessage.Text ?? replyToMessage.Caption;
            _logger.LogDebug("Админская команда {Command}: извлечен текст='{Text}' (длина={Length})", 
                message.Text, string.IsNullOrWhiteSpace(text) ? "[ПУСТОЙ]" : text.Length > 100 ? text.Substring(0, 100) + "..." : text, 
                text?.Length ?? 0);
                
            if (!string.IsNullOrWhiteSpace(text))
            {
                switch (message.Text)
                {
                    case "/check":
                    {
                        var emojis = SimpleFilters.TooManyEmojis(text);
                        var normalized = TextProcessor.NormalizeText(text);
                        var lookalike = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(normalized);
                        var hasStopWords = SimpleFilters.HasStopWords(normalized);
                        var (spam, score) = await _classifier.IsSpam(normalized);
                        var lookAlikeMsg = lookalike.Count == 0 ? "отсутствуют" : string.Join(", ", lookalike);
                        var msg =
                            $"*Результат проверки:*\n"
                            + $"• Много эмодзи: *{emojis}*\n"
                            + $"• Найдены стоп-слова: *{hasStopWords}*\n"
                            + $"• Маскирующиеся слова: *{lookAlikeMsg}*\n"
                            + $"• ML классификатор: спам *{spam}*, скор *{score}*\n\n"
                            + $"_Если простые фильтры отработали, то в датасет добавлять не нужно_";
                        await _bot.SendMessage(message.Chat, msg, parseMode: ParseMode.Markdown);
                        break;
                    }
                    case "/spam":
                        _logger.LogInformation("🔥 Обрабатываем команду /spam для текста: '{Text}'", text);
                        await _classifier.AddSpam(text);
                        await _badMessageManager.MarkAsBad(text);
                        await _bot.SendMessage(
                            message.Chat.Id,
                            "✅ Сообщение добавлено как пример спама в датасет, а также в список авто-бана",
                            replyParameters: replyToMessage
                        );
                        _logger.LogInformation("✅ Команда /spam успешно выполнена");
                        break;
                    case "/ham":
                        _logger.LogInformation("✅ Обрабатываем команду /ham для текста: '{Text}'", text);
                        await _classifier.AddHam(text);
                        await _bot.SendMessage(
                            message.Chat.Id,
                            "✅ Сообщение добавлено как пример НЕ-спама в датасет",
                            replyParameters: replyToMessage
                        );
                        _logger.LogInformation("✅ Команда /ham успешно выполнена");
                        break;
                }
            }
            else
            {
                _logger.LogWarning("❌ Команда {Command} не выполнена: текст сообщения пустой или отсутствует", message.Text);
                await _bot.SendMessage(
                    message.Chat.Id,
                    "⚠️ Не могу выполнить команду: сообщение не содержит текста",
                    replyParameters: replyToMessage
                );
            }
        }
        // Команда статистики по группам
        else if (message.Text?.Trim().ToLower() == "/stat" || message.Text?.Trim().ToLower() == "/stats")
        {
            var report = _statisticsService.GetAllStats();
            var sb = new StringBuilder();
            sb.AppendLine("📊 *Статистика по группам:*\n");
            foreach (var (chatId, stats) in report.OrderBy(x => x.Value.ChatTitle))
            {
                var sum = stats.KnownBadMessage + stats.BlacklistBanned + stats.StoppedCaptcha + stats.LongNameBanned;
                if (sum == 0) continue;
                Chat? chat = null;
                try { chat = await _bot.GetChat(chatId); } catch { }
                sb.AppendLine();
                if (chat != null)
                    sb.AppendLine($"{_chatLinkFormatter.GetChatLink(chat)} (`{chat.Id}`) [{ChatSettingsManager.GetChatType(chat.Id)}]:");
                else
                    sb.AppendLine($"{_chatLinkFormatter.GetChatLink(chatId, stats.ChatTitle)} (`{chatId}`) [{ChatSettingsManager.GetChatType(chatId)}]:");
                sb.AppendLine($"▫️ Всего блокировок: *{sum}*");
                if (stats.BlacklistBanned > 0)
                    sb.AppendLine($"▫️ По блеклистам: *{stats.BlacklistBanned}*");
                if (stats.StoppedCaptcha > 0)
                    sb.AppendLine($"▫️ Не прошли капчу: *{stats.StoppedCaptcha}*");
                if (stats.KnownBadMessage > 0)
                    sb.AppendLine($"▫️ Известные спам-сообщения: *{stats.KnownBadMessage}*");
                if (stats.LongNameBanned > 0)
                    sb.AppendLine($"▫️ За длинные имена: *{stats.LongNameBanned}*");
            }
            if (sb.Length <= 35)
                sb.AppendLine("\nНичего интересного не произошло 🎉");
                            await _bot.SendMessage(message.Chat, sb.ToString(), parseMode: ParseMode.Markdown);
            return;
        }
        // Добавляю обработку команды /say
        if (message.Text != null && message.Text.StartsWith("/say "))
        {
            var parts = message.Text.Split(' ', 3);
            if (parts.Length < 3)
            {
                await _bot.SendMessage(message.Chat, "Формат: /say @username сообщение или /say user_id сообщение");
                return;
            }
            var target = parts[1];
            var textToSend = parts[2];
            long? userId = null;
            if (target.StartsWith("@"))
            {
                // Пробуем найти userId по username среди недавних пользователей (по кэшу)
                // Если нет — не отправляем
                userId = TryFindUserIdByUsername(target.Substring(1));
            }
            else if (long.TryParse(target, out var id))
            {
                userId = id;
            }
            if (userId == null)
            {
                await _bot.SendMessage(message.Chat, $"Не удалось найти пользователя {target}. Сообщение не отправлено.");
                return;
            }
            try
            {
                await _bot.SendMessage(userId.Value, textToSend, parseMode: ParseMode.Markdown);
                await _bot.SendMessage(message.Chat, $"Сообщение отправлено пользователю {target}");
            }
            catch (Exception ex)
            {
                await _bot.SendMessage(message.Chat, $"Не удалось доставить сообщение пользователю {target}: {ex.Message}");
            }
            return;
        }
    }

    // Вспомогательная функция для поиска userId по username среди недавних пользователей (по кэшу)
    private long? TryFindUserIdByUsername(string username)
    {
        // Можно использовать MemoryCache или другой кэш, если он есть
        // Здесь пример с MemoryCache: ищем по ключам, где username встречался
        foreach (var item in MemoryCache.Default)
        {
            if (item.Value is string text && text.Contains(username, StringComparison.OrdinalIgnoreCase))
            {
                // Ключи вида chatId_userId
                var parts = item.Key.ToString().Split('_');
                if (parts.Length == 2 && long.TryParse(parts[1], out var uid))
                    return uid;
            }
        }
        return null;
    }

    private static string UserToKey(long chatId, User user) => $"{chatId}_{user.Id}";
    
    /// <summary>
    /// Проверяет, одобрен ли пользователь с учетом текущей системы одобрения
    /// </summary>
    private bool IsUserApproved(long userId, long? chatId = null)
    {
        if (Config.UseNewApprovalSystem)
        {
            // Новая система: проверяем с учетом режима (глобального или группового)
            return _userManager.Approved(userId, chatId);
        }
        else
        {
            // Старая система: проверяем только глобально
            return _userManager.Approved(userId);
        }
    }

    /// <summary>
    /// Определяет, является ли сообщение из обсуждения канала
    /// </summary>
    private async Task<bool> IsChannelDiscussion(Chat chat, Message message)
    {
        try
        {
            // Если это не супергруппа, то это точно не обсуждение
            if (chat.Type != ChatType.Supergroup)
                return false;

            // Проверяем, есть ли у группы связанный канал
            // Для получения LinkedChatId нужно использовать GetChatFullInfo
            var hasLinkedChannel = false;
            try
            {
                var chatFull = await _bot.GetChatFullInfo(chat.Id);
                hasLinkedChannel = chatFull.LinkedChatId != null;
            }
            catch
            {
                // Если не удалось получить полную информацию, считаем что связанного канала нет
                hasLinkedChannel = false;
            }
            
            // Обсуждение канала если:
            // 1. Есть связанный канал И сообщение автоматически переслано
            // 2. ИЛИ просто есть связанный канал (пользователи пишут в обсуждении)
            var isDiscussion = hasLinkedChannel && (message.IsAutomaticForward || true);
            
            if (isDiscussion)
            {
                _logger.LogDebug("Обнаружено обсуждение канала: chat={ChatId}, hasLinkedChannel={HasLinked}, autoForward={AutoForward}", 
                    chat.Id, hasLinkedChannel, message.IsAutomaticForward);
            }
            
            return isDiscussion;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось определить тип чата {ChatId}", chat.Id);
            return false;
        }
    }

    // Методы BanNoCaptchaUsers, CaptchaAttempts и UnbanUserLater удалены - логика перенесена в CaptchaService

    private void DeleteMessageLater(Message message, TimeSpan after = default, CancellationToken cancellationToken = default)
    {
        if (after == default)
            after = TimeSpan.FromMinutes(5);
        _ = Task.Run(
            async () =>
            {
                try
                {
                    await Task.Delay(after, cancellationToken);
                    await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessage");
                }
            },
            cancellationToken
        );
    }

    private static string AdminDisplayName(User user)
    {
        return !string.IsNullOrEmpty(user.Username)
            ? user.Username
            : FullName(user.FirstName, user.LastName);
    }
}
