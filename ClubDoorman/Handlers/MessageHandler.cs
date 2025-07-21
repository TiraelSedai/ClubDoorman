using System.Collections.Concurrent;
using System.Runtime.Caching;
using ClubDoorman.Handlers.Commands;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Extensions;

namespace ClubDoorman.Handlers;

/// <summary>
/// Обработчик сообщений
/// </summary>
public class MessageHandler : IUpdateHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IModerationService _moderationService;
    private readonly ICaptchaService _captchaService;
    private readonly IUserManager _userManager;
    private readonly ISpamHamClassifier _classifier;
    private readonly IBadMessageManager _badMessageManager;
    private readonly IAiChecks _aiChecks;
    private readonly GlobalStatsManager _globalStatsManager;
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<MessageHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IMessageService _messageService;

    // Флаги присоединившихся пользователей (временные)
    private static readonly ConcurrentDictionary<string, byte> _joinedUserFlags = new();

    /// <summary>
    /// Создает экземпляр обработчика сообщений.
    /// </summary>
    /// <param name="bot">Клиент Telegram бота</param>
    /// <param name="moderationService">Сервис модерации</param>
    /// <param name="captchaService">Сервис капчи</param>
    /// <param name="userManager">Менеджер пользователей</param>
    /// <param name="classifier">Классификатор спама</param>
    /// <param name="badMessageManager">Менеджер плохих сообщений</param>
    /// <param name="aiChecks">AI проверки</param>
    /// <param name="globalStatsManager">Менеджер глобальной статистики</param>
    /// <param name="statisticsService">Сервис статистики</param>
    /// <param name="serviceProvider">Провайдер сервисов</param>
    /// <param name="userFlowLogger">Логгер пользовательского флоу</param>
    /// <param name="messageService">Сервис уведомлений</param>
    /// <param name="logger">Логгер</param>
    /// <exception cref="ArgumentNullException">Если любой из параметров равен null</exception>
    public MessageHandler(
        ITelegramBotClientWrapper bot,
        IModerationService moderationService,
        ICaptchaService captchaService,
        IUserManager userManager,
        ISpamHamClassifier classifier,
        IBadMessageManager badMessageManager,
        IAiChecks aiChecks,
        GlobalStatsManager globalStatsManager,
        IStatisticsService statisticsService,
        IServiceProvider serviceProvider,
        IUserFlowLogger userFlowLogger,
        IMessageService messageService,
        ILogger<MessageHandler> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService));
        _captchaService = captchaService ?? throw new ArgumentNullException(nameof(captchaService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _badMessageManager = badMessageManager ?? throw new ArgumentNullException(nameof(badMessageManager));
        _aiChecks = aiChecks ?? throw new ArgumentNullException(nameof(aiChecks));
        _globalStatsManager = globalStatsManager ?? throw new ArgumentNullException(nameof(globalStatsManager));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _userFlowLogger = userFlowLogger ?? throw new ArgumentNullException(nameof(userFlowLogger));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Проверяет, может ли обработчик обработать данное обновление.
    /// </summary>
    /// <param name="update">Обновление для проверки</param>
    /// <returns>true, если обновление содержит сообщение</returns>
    public bool CanHandle(Update update)
    {
        return update?.Message != null || update?.EditedMessage != null;
    }

    /// <summary>
    /// Обрабатывает обновление, содержащее сообщение.
    /// </summary>
    /// <param name="update">Обновление для обработки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <exception cref="ArgumentNullException">Если update равен null</exception>
    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update == null) throw new ArgumentNullException(nameof(update));
        if (update.Message == null && update.EditedMessage == null) 
            throw new ArgumentNullException(nameof(update.Message));

        var message = update.EditedMessage ?? update.Message!;
        var chat = message.Chat;

        _logger.LogDebug("MessageHandler получил сообщение {MessageId} в чате {ChatId} от пользователя {UserId}", 
            message.MessageId, chat.Id, message.From?.Id);

        // Проверка whitelist - если активен, работаем только в разрешённых чатах
        // ИСКЛЮЧЕНИЕ: админ-чаты всегда обрабатываются (для команд /spam, /ham и т.д.)
        var isAdminChat = chat.Id == Config.AdminChatId || chat.Id == Config.LogAdminChatId;
        
        if (!Config.IsChatAllowed(chat.Id) && !isAdminChat)
        {
            _logger.LogDebug("Чат {ChatId} ({ChatTitle}) не в whitelist - игнорируем", chat.Id, chat.Title);
            return;
        }

        // Игнорировать полностью отключённые чаты
        if (Config.DisabledChats.Contains(chat.Id))
            return;

        // Автоматически добавляем чат в конфиг
        ChatSettingsManager.EnsureChatInConfig(chat.Id, chat.Title);

        // Обработка команд
        if (message.Text?.StartsWith("/") == true)
        {
            await HandleCommandAsync(message, cancellationToken);
            return;
        }

        // Для приватных чатов обрабатываем только команды, остальное игнорируем
        if (chat.Type == ChatType.Private)
        {
            _logger.LogDebug("Приватный чат {ChatId} - обрабатываем только команды", chat.Id);
            return;
        }

        // Обработка новых участников
        if (message.NewChatMembers != null && chat.Id != Config.AdminChatId)
        {
            await HandleNewMembersAsync(message, cancellationToken);
            return;
        }

        // Удаление сообщений о бане ботом
        if (message.LeftChatMember != null && message.From?.Id == _bot.BotId)
        {
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                _logger.LogDebug("Удалено сообщение о бане/исключении пользователя");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось удалить сообщение о бане/исключении");
            }
            return;
        }

        // Сообщения от каналов
        if (message.SenderChat != null)
        {
            await HandleChannelMessageAsync(message, cancellationToken);
            return;
        }

        // Обычные сообщения пользователей
        await HandleUserMessageAsync(message, cancellationToken);
    }

    private async Task HandleCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var commandText = message.Text!.Split(' ')[0].ToLower();
        var command = commandText.StartsWith("/") ? commandText.Substring(1) : commandText;

        // Обработка команды /start
        if (command == "start")
        {
            // Получаем StartCommandHandler из DI и делегируем обработку
            var startHandler = _serviceProvider.GetRequiredService<StartCommandHandler>();
            await startHandler.HandleAsync(message, cancellationToken);
            return;
        }

        // Обработка команды /suspicious
        if (command == "suspicious")
        {
            // Получаем SuspiciousCommandHandler из DI и делегируем обработку
            var suspiciousHandler = _serviceProvider.GetRequiredService<SuspiciousCommandHandler>();
            await suspiciousHandler.HandleAsync(message, cancellationToken);
            return;
        }

        // Админские команды (/spam, /ham, /check) - только в админ-чатах
        var isAdminChat = message.Chat.Id == Config.AdminChatId || message.Chat.Id == Config.LogAdminChatId;
        if (isAdminChat && message.ReplyToMessage != null && (command == "spam" || command == "ham" || command == "check"))
        {
            await HandleAdminCommandAsync(message, command, cancellationToken);
        }
    }

    private async Task HandleAdminCommandAsync(Message message, string command, CancellationToken cancellationToken)
    {
        var replyToMessage = message.ReplyToMessage!;
        
        // Проверяем, что реплай не на сообщение бота (кроме форвардов)
        if (replyToMessage.From?.Id == _bot.BotId && replyToMessage.ForwardDate == null)
        {
            await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning, 
                new SimpleNotificationData(message.From!, message.Chat, "Реплай на сообщение бота"), 
                cancellationToken);
            return;
        }

        var text = replyToMessage.Text ?? replyToMessage.Caption;
        _logger.LogDebug("Админская команда /{Command}: извлечен текст='{Text}' (длина={Length})", 
            command, string.IsNullOrWhiteSpace(text) ? "[ПУСТОЙ]" : text.Length > 100 ? text.Substring(0, 100) + "..." : text, 
            text?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("❌ Команда /{Command} не выполнена: текст сообщения пустой или отсутствует", command);
            await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Warning, 
                new SimpleNotificationData(message.From!, message.Chat, "Сообщение не содержит текста"), 
                cancellationToken);
            return;
        }

        switch (command)
        {
            case "check":
                await HandleCheckCommandAsync(message, text, replyToMessage, cancellationToken);
                break;
            case "spam":
                await HandleSpamCommandAsync(message, text, replyToMessage, cancellationToken);
                break;
            case "ham":
                await HandleHamCommandAsync(message, text, replyToMessage, cancellationToken);
                break;
        }
    }

    private async Task HandleCheckCommandAsync(Message message, string text, Message replyToMessage, CancellationToken cancellationToken)
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
        await _bot.SendMessage(message.Chat.Id, msg, parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
    }

    private async Task HandleSpamCommandAsync(Message message, string text, Message replyToMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔥 Обрабатываем команду /spam для текста: '{Text}'", text);
        await _classifier.AddSpam(text);
        await _badMessageManager.MarkAsBad(text);
        await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Success, 
            new SimpleNotificationData(message.From!, message.Chat, "Сообщение добавлено как пример спама"), 
            cancellationToken);
        _logger.LogInformation("✅ Команда /spam успешно выполнена");
    }

    private async Task HandleHamCommandAsync(Message message, string text, Message replyToMessage, CancellationToken cancellationToken)
    {
        _logger.LogInformation("✅ Обрабатываем команду /ham для текста: '{Text}'", text);
        await _classifier.AddHam(text);
        await _messageService.SendUserNotificationAsync(message.From!, message.Chat, UserNotificationType.Success, 
            new SimpleNotificationData(message.From!, message.Chat, "Сообщение добавлено как пример НЕ-спама"), 
            cancellationToken);
        _logger.LogInformation("✅ Команда /ham успешно выполнена");
    }

    private async Task HandleNewMembersAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.NewChatMembers == null)
        {
            _logger.LogDebug("Сообщение о новых участниках не содержит данных о пользователях");
            return;
        }

        foreach (var newUser in message.NewChatMembers.Where(x => x != null && !x.IsBot))
        {
            var joinKey = $"joined_{message.Chat.Id}_{newUser.Id}";
            if (!_joinedUserFlags.ContainsKey(joinKey))
            {
                _logger.LogInformation("==================== НОВЫЙ УЧАСТНИК ====================\n" +
                    "Пользователь {User} (id={UserId}, username={Username}) зашел в группу '{ChatTitle}' (id={ChatId})\n" +
                    "========================================================", 
                    Utils.FullName(newUser), newUser.Id, newUser.Username ?? "-", message.Chat.Title ?? "-", message.Chat.Id);

                _joinedUserFlags.TryAdd(joinKey, 1);
                _ = Task.Run(async () => { 
                    await Task.Delay(15000); 
                    _joinedUserFlags.TryRemove(joinKey, out _); 
                });
            }

            await ProcessNewUserAsync(message, newUser, cancellationToken);
        }
    }

    private async Task ProcessNewUserAsync(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            _logger.LogWarning("ProcessNewUserAsync вызван с null пользователем");
            return;
        }

        if (userJoinMessage?.Chat == null)
        {
            _logger.LogWarning("ProcessNewUserAsync вызван с null сообщением или чатом");
            return;
        }

        var chat = userJoinMessage.Chat;

        // Проверка имени пользователя
        if (_moderationService == null)
        {
            _logger.LogError("_moderationService равен null в ProcessNewUserAsync");
            return;
        }

        var nameResult = await _moderationService.CheckUserNameAsync(user);
        if (nameResult.Action == ModerationAction.Ban)
        {
            await BanUserForLongName(userJoinMessage, user, nameResult.Reason, null, cancellationToken);
            return;
        }
        if (nameResult.Action == ModerationAction.Report)
        {
            await BanUserForLongName(userJoinMessage, user, nameResult.Reason, TimeSpan.FromMinutes(10), cancellationToken);
            return;
        }

        // Проверка клубного пользователя
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (clubUser != null)
        {
            _logger.LogDebug("User is {Name} from club", clubUser);
            return;
        }

        // Проверка блэклиста
        if (await _userManager.InBanlist(user.Id))
        {
            await BanBlacklistedUser(userJoinMessage, user, cancellationToken);
            return;
        }

        // Проверяем, не находится ли пользователь уже в процессе прохождения капчи
        var captchaKey = _captchaService.GenerateKey(chat.Id, user.Id);
        if (_captchaService.GetCaptchaInfo(captchaKey) != null)
        {
            _logger.LogDebug("Пользователь уже проходит капчу");
            return;
        }

        // Создаем капчу
        await _captchaService.CreateCaptchaAsync(chat, user, userJoinMessage);
    }

    private async Task HandleChannelMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var chat = message.Chat;
        var senderChat = message.SenderChat!;

        // Разрешаем сообщения от самого чата
        if (senderChat.Id == chat.Id)
            return;

        // Разрешаем в announcement чатах
        if (ChatSettingsManager.GetChatType(chat.Id) == "announcement")
            return;

        // Проверяем связанный чат
        try
        {
            var chatFull = await _bot.GetChat(chat, cancellationToken);
            // Проверяем, является ли это обсуждением канала
            if (chat.Type == ChatType.Supergroup && message.IsAutomaticForward)
                return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить информацию о чате {ChatId}", chat.Id);
        }

        // Автобан каналов если включен
        if (Config.ChannelAutoBan)
        {
            await AutoBanChannel(message, cancellationToken);
        }
        else
        {
            // Просто репортим
            _logger.LogInformation("Сообщение от канала {ChannelTitle} в чате {ChatTitle} - репорт в админ-чат", 
                senderChat.Title, chat.Title);
        }
    }

    private async Task HandleUserMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var user = message.From;
        var chat = message.Chat;

        // Игнорируем сообщения без пользователя (системные сообщения)
        if (user == null)
        {
            _logger.LogDebug("Игнорируем системное сообщение без пользователя");
            return;
        }

        // Игнорируем сообщения от ботов
        if (user.IsBot)
        {
            _logger.LogDebug("Игнорируем сообщение от бота {BotId}", user.Id);
            return;
        }

        // Игнорируем системные сообщения (выход пользователей и т.д.)
        if (message.LeftChatMember != null)
        {
            _logger.LogDebug("Игнорируем системное сообщение о выходе пользователя");
            return;
        }

        // Проверяем, не находится ли пользователь в процессе прохождения капчи
        var captchaKey = _captchaService.GenerateKey(chat.Id, user.Id);
        if (_captchaService.GetCaptchaInfo(captchaKey) != null)
        {
            // Удаляем сообщение от пользователя, который должен пройти капчу
            try
            {
                await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить сообщение от пользователя проходящего капчу");
            }
            return;
        }

        // ПРИОРИТЕТНАЯ проверка блэклиста lols.bot (выполняется даже для одобренных)
        _logger.LogDebug("🔍 Проверяем пользователя {UserId} по блэклисту lols.bot", user.Id);
        if (await _userManager.InBanlist(user.Id))
        {
            await HandleBlacklistBan(message, user, chat, cancellationToken);
            return;
        }
        _logger.LogDebug("✅ Пользователь {UserId} не найден в блэклисте", user.Id);

        // Проверяем, одобрен ли пользователь
        if (_moderationService.IsUserApproved(user.Id, chat.Id))
        {
            _logger.LogDebug("✅ Пользователь {UserId} уже одобрен в чате {ChatId}, пропускаем модерацию", user.Id, chat.Id);
            return;
        }

        // Логируем сообщения от неодобренных пользователей для анализа
        var messageText = message.Text ?? message.Caption ?? "[медиа/стикер/файл]";
        _userFlowLogger.LogFirstMessage(user, chat, messageText);

        // Определяем тип пользователя
        var isChannelDiscussion = await IsChannelDiscussion(chat, message);
        var userType = isChannelDiscussion ? "из обсуждения канала" : "новый участник";
        
        _logger.LogInformation("==================== СООБЩЕНИЕ ОТ НЕОДОБРЕННОГО ====================\n" +
            "{UserType}: {User} (id={UserId}, username={Username}) в '{ChatTitle}' (id={ChatId})\n" +
            "Сообщение: {Text}\n" +
            "================================================================", 
            userType, Utils.FullName(user), user.Id, user.Username ?? "-", chat.Title ?? "-", chat.Id, 
            (message.Text ?? message.Caption)?.Substring(0, Math.Min((message.Text ?? message.Caption)?.Length ?? 0, 100)) ?? "[медиа]");

        // Проверка на клубного пользователя
        var clubName = await _userManager.GetClubUsername(user.Id);
        if (!string.IsNullOrEmpty(clubName))
        {
            _logger.LogDebug("User is {Name} from club", clubName);
            return;
        }

        // AI анализ профиля при первом сообщении
        var profileAnalysisResult = await PerformAiProfileAnalysis(message, user, chat, cancellationToken);
        if (profileAnalysisResult)
        {
            // Пользователь получил ограничения за подозрительный профиль, не продолжаем модерацию
            return;
        }

        // Модерация сообщения
        _userFlowLogger.LogModerationStarted(user, chat, messageText);
        var moderationResult = await _moderationService.CheckMessageAsync(message);
        _userFlowLogger.LogModerationResult(user, chat, moderationResult.Action.ToString(), moderationResult.Reason, moderationResult.Confidence);
        
        switch (moderationResult.Action)
        {
            case ModerationAction.Allow:
                _logger.LogDebug("Сообщение разрешено: {Reason}", moderationResult.Reason);
                var allowedMessageText = message.Text ?? message.Caption ?? "";
                
                // Проверяем AI детект для подозрительных пользователей
                var aiDetectBlocked = await _moderationService.CheckAiDetectAndNotifyAdminsAsync(user, chat, message);
                
                // Засчитываем хорошее сообщение только если пользователь не был заблокирован AI детектом
                if (!aiDetectBlocked)
                {
                    await _moderationService.IncrementGoodMessageCountAsync(user, chat, allowedMessageText);
                }
                break;
            
            case ModerationAction.Ban:
                _userFlowLogger.LogUserBanned(user, chat, moderationResult.Reason);
                await AutoBan(message, moderationResult.Reason, cancellationToken);
                break;
            
            case ModerationAction.Delete:
                _logger.LogInformation("Удаление сообщения: {Reason}", moderationResult.Reason);
                try
                {
                    await DeleteAndReportMessage(message, moderationResult.Reason, cancellationToken);
                    _logger.LogInformation("Сообщение успешно обработано для удаления");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при удалении сообщения: {Reason}", moderationResult.Reason);
                }
                break;
            
            case ModerationAction.Report:
                _logger.LogInformation("Отправка в админ-чат: {Reason}", moderationResult.Reason);
                await DontDeleteButReportMessage(message, user, cancellationToken);
                break;
            
            case ModerationAction.RequireManualReview:
                _logger.LogInformation("Требует ручной проверки: {Reason}", moderationResult.Reason);
                await DontDeleteButReportMessage(message, user, cancellationToken);
                break;
        }
    }

    private async Task<bool> IsChannelDiscussion(Chat chat, Message message)
    {
        try
        {
            if (chat.Type != ChatType.Supergroup)
                return false;

            // Проверяем, является ли это автоматическим пересыланием из канала
            var isAutoForward = message.IsAutomaticForward;
            
            if (isAutoForward)
            {
                _logger.LogDebug("Обнаружено обсуждение канала: chat={ChatId}, autoForward={AutoForward}", 
                    chat.Id, message.IsAutomaticForward);
            }
            
            return isAutoForward;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось определить тип чата {ChatId}", chat.Id);
            return false;
        }
    }

    private async Task BanUserForLongName(Message? userJoinMessage, User user, string reason, TimeSpan? banDuration, CancellationToken cancellationToken)
    {
        try
        {
            var chat = userJoinMessage?.Chat!;
            
            // Проверяем, что чат не приватный - в приватных чатах нельзя банить пользователей
            if (chat.Type == ChatType.Private)
            {
                _logger.LogWarning("Попытка бана за длинное имя в приватном чате {ChatId} - операция невозможна", chat.Id);
                var errorData = new ErrorNotificationData(
                    new InvalidOperationException("Попытка бана в приватном чате"),
                    "Бан за длинное имя",
                    user,
                    chat
                );
                await _messageService.SendAdminNotificationAsync(AdminNotificationType.PrivateChatBanAttempt, errorData, cancellationToken);
                return;
            }
            
            await _bot.BanChatMember(
                chat.Id, 
                user.Id,
                banDuration.HasValue ? DateTime.UtcNow + banDuration.Value : null,
                revokeMessages: true
            );
            
            if (userJoinMessage != null)
            {
                await _bot.DeleteMessage(userJoinMessage.Chat.Id, userJoinMessage.MessageId, cancellationToken);
            }

            var banType = banDuration.HasValue ? "Автобан на 10 минут" : "🚫 Перманентный бан";
            var banData = new AutoBanNotificationData(user, chat, banType, reason, userJoinMessage?.MessageId);
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.BanForLongName, banData, cancellationToken);
            
            _userFlowLogger.LogUserBanned(user, chat, reason);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя за длинное имя");
        }
    }

    private async Task BanBlacklistedUser(Message userJoinMessage, User user, CancellationToken cancellationToken)
    {
        try
        {
            var chat = userJoinMessage.Chat;
            
            // Проверяем, что чат не приватный - в приватных чатах нельзя банить пользователей
            if (chat.Type == ChatType.Private)
            {
                _logger.LogWarning("Попытка бана из блэклиста в приватном чате {ChatId} - операция невозможна", chat.Id);
                var errorData = new ErrorNotificationData(
                    new InvalidOperationException("Попытка бана в приватном чате"),
                    "Бан из блэклиста",
                    user,
                    chat
                );
                await _messageService.SendAdminNotificationAsync(AdminNotificationType.PrivateChatBanAttempt, errorData, cancellationToken);
                return;
            }
            
            var banUntil = DateTime.UtcNow + TimeSpan.FromMinutes(240);
            await _bot.BanChatMember(chat.Id, user.Id, banUntil, revokeMessages: true, cancellationToken: cancellationToken);
            
            await _bot.DeleteMessage(chat.Id, userJoinMessage.MessageId, cancellationToken);
            
            _userFlowLogger.LogUserBanned(user, chat, "Пользователь в блэклисте");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя из блэклиста");
        }
    }

    private async Task AutoBanChannel(Message message, CancellationToken cancellationToken)
    {
        try
        {
            var chat = message.Chat;
            var senderChat = message.SenderChat!;
            
            var fwd = await _bot.ForwardMessage(Config.AdminChatId, chat, message.MessageId, cancellationToken: cancellationToken);
            await _bot.DeleteMessage(chat, message.MessageId, cancellationToken);
            await _bot.BanChatSenderChat(chat, senderChat.Id, cancellationToken);
            
            var channelData = new ChannelMessageNotificationData(senderChat, chat, message.Text ?? "[медиа]");
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.ChannelMessage, channelData, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить канал");
            var errorData = new ErrorNotificationData(
                new InvalidOperationException("Не удалось забанить канал"),
                "Не хватает могущества",
                null,
                message.Chat
            );
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.ChannelError, errorData, cancellationToken);
        }
    }

    private async Task AutoBan(Message message, string reason, CancellationToken cancellationToken)
    {
        var user = message.From;
        var chat = message.Chat;
        
        // Проверяем, что чат не приватный - в приватных чатах нельзя банить пользователей
        if (chat.Type == ChatType.Private)
        {
            _logger.LogWarning("Попытка бана в приватном чате {ChatId} - операция невозможна", chat.Id);
            var errorData = new ErrorNotificationData(
                new InvalidOperationException("Попытка бана в приватном чате"),
                reason,
                user,
                chat
            );
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.PrivateChatBanAttempt, errorData, cancellationToken);
            return;
        }
        
        var forward = await _bot.ForwardMessage(
            new ChatId(Config.AdminChatId),
            message.Chat.Id,
            message.MessageId,
            cancellationToken: cancellationToken
        );
        var autoBanData = new AutoBanNotificationData(
            user, 
            message.Chat, 
            "Авто-бан", 
            reason, 
            message.MessageId, 
            LinkToMessage(message.Chat, message.MessageId)
        );
        await _messageService.SendAdminNotificationAsync(AdminNotificationType.AutoBan, autoBanData, cancellationToken);
        await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: cancellationToken);
        await _bot.BanChatMember(message.Chat, user.Id, revokeMessages: false, cancellationToken: cancellationToken);
        
        // Полностью очищаем пользователя из всех списков
        _moderationService.CleanupUserFromAllLists(user.Id, message.Chat.Id);
        
        var cleanupData = new UserCleanupNotificationData(user, message.Chat, "после автобана");
        await _messageService.SendAdminNotificationAsync(AdminNotificationType.UserCleanup, cleanupData, cancellationToken);
    }

    private async Task DeleteAndReportMessage(Message message, string reason, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Начинаем DeleteAndReportMessage для сообщения {MessageId} в чате {ChatId}", message.MessageId, message.Chat.Id);
        
        var user = message.From;
        
        Message? forward = null;
        var deletionMessagePart = $"{reason}";
        
        try 
        {
            _logger.LogDebug("Пытаемся переслать сообщение в админ-чат {AdminChatId}", Config.AdminChatId);
            // Пытаемся переслать сообщение
            forward = await _bot.ForwardMessage(
                new ChatId(Config.AdminChatId),
                message.Chat.Id,
                message.MessageId,
                cancellationToken: cancellationToken
            );
            _logger.LogDebug("Сообщение успешно переслано в админ-чат");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось переслать сообщение");
        }
        
        try
        {
            _logger.LogDebug("Пытаемся удалить сообщение {MessageId} из чата {ChatId}", message.MessageId, message.Chat.Id);
            await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
            deletionMessagePart += ", сообщение удалено.";
            _logger.LogDebug("Сообщение успешно удалено");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to delete message {MessageId} from chat {ChatId}", message.MessageId, message.Chat.Id);
            deletionMessagePart += ", сообщение НЕ удалено (не хватило могущества?).";
        }

        try
        {
            // Создаем кнопки реакции для админ-чата
            var callbackDataBan = $"ban_{message.Chat.Id}_{user.Id}";
            MemoryCache.Default.Add(callbackDataBan, message, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(12) });
            
            var row = new List<InlineKeyboardButton>
            {
                new InlineKeyboardButton("🤖 бан") { CallbackData = callbackDataBan },
                new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" },
                new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" }
            };

            var postLink = LinkToMessage(message.Chat, message.MessageId);
            _logger.LogDebug("Отправляем уведомление в админ-чат {AdminChatId}", Config.AdminChatId);
            await _bot.SendMessage(
                Config.AdminChatId,
                $"⚠️ *{deletionMessagePart}*\nПользователь [{Markdown.Escape(FullName(user.FirstName, user.LastName))}](tg://user?id={user.Id}) в чате *{message.Chat.Title}*\n{postLink}",
                parseMode: ParseMode.Markdown,
                replyParameters: forward,
                replyMarkup: new InlineKeyboardMarkup(row),
                cancellationToken: cancellationToken
            );
            _logger.LogDebug("Уведомление успешно отправлено в админ-чат");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Не удалось отправить уведомление в админ-чат");
        }

        // Отправляем предупреждение пользователю (только если не было отправлено недавно)
        var warningKey = $"warning_{message.Chat.Id}_{user.Id}";
        var existingWarning = MemoryCache.Default.Get(warningKey);
        
        if (existingWarning == null)
        {
            try
            {
                var mention = $"[{user.FirstName}](tg://user?id={user.Id})";
                var warnMsg = $"👋 {mention}, вы пока *новичок* в этом чате\\.\n\n*Первые 3 сообщения* проходят антиспам\\-проверку:\n• нельзя эмодзи, рекламу и *стоп\\-слова*\n• работает ML\\-анализ";
                
                var sentWarn = await _bot.SendMessage(
                    message.Chat.Id, 
                    warnMsg, 
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken
                );
                
                // Сохраняем ID предупреждающего сообщения в кэше (на 10 минут, чтобы не спамить)
                MemoryCache.Default.Add(warningKey, sentWarn.MessageId, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10) });
                
                DeleteMessageLater(sentWarn, TimeSpan.FromSeconds(40), cancellationToken);
                _logger.LogDebug("Предупреждение отправлено пользователю и будет удалено через 40 секунд");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось отправить предупреждение пользователю");
            }
        }
        else
        {
            _logger.LogDebug("Предупреждение пользователю {UserId} в чате {ChatId} уже было отправлено недавно, пропускаем", user.Id, message.Chat.Id);
        }
    }

    private async Task DontDeleteButReportMessage(Message message, User user, CancellationToken cancellationToken)
    {
        try
        {
            var forward = await _bot.ForwardMessage(
                new ChatId(Config.AdminChatId),
                message.Chat.Id,
                message.MessageId,
                cancellationToken: cancellationToken
            );
            
            var suspiciousData = new SuspiciousMessageNotificationData(
                user, 
                message.Chat, 
                message.Text ?? message.Caption ?? "[медиа]", 
                message.MessageId
            );
            await _messageService.SendAdminNotificationAsync(AdminNotificationType.SuspiciousMessage, suspiciousData, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при пересылке сообщения");
            await _bot.SendMessage(
                Config.AdminChatId,
                $"⚠️ Не удалось переслать подозрительное сообщение из чата *{message.Chat.Title}* от пользователя [{Markdown.Escape(FullName(user.FirstName, user.LastName))}](tg://user?id={user.Id})",
                parseMode: ParseMode.Markdown
            );
        }
    }

    /// <summary>
    /// Обработка бана пользователя по блэклисту lols.bot при первом сообщении
    /// </summary>
    private async Task HandleBlacklistBan(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        var messageText = message.Text ?? message.Caption ?? "[медиа/стикер/файл]";
        _logger.LogWarning("🚫 БЛЭКЛИСТ LOLS.BOT: {UserName} (id={UserId}) в чате '{ChatTitle}' (id={ChatId}) написал: {MessageText}", 
            FullName(user.FirstName, user.LastName), user.Id, chat.Title, chat.Id, 
            messageText.Length > 100 ? messageText.Substring(0, 100) + "..." : messageText);
        
        _userFlowLogger.LogUserBanned(user, chat, "Пользователь в блэклисте lols.bot");
        
        // Пересылаем сообщение в лог-чат перед удалением
        try
        {
            var forward = await _bot.ForwardMessage(
                new ChatId(Config.LogAdminChatId),
                message.Chat.Id,
                message.MessageId,
                cancellationToken: cancellationToken
            );
            
            await _bot.SendMessage(
                Config.LogAdminChatId,
                $"🚫 Автобан по блэклисту lols.bot (первое сообщение){Environment.NewLine}" +
                $"Юзер {FullName(user.FirstName, user.LastName)} из чата {message.Chat.Title}{Environment.NewLine}" +
                $"{LinkToMessage(message.Chat, message.MessageId)}",
                replyParameters: forward,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось переслать сообщение в лог-чат");
        }
        
        // Удаляем сообщение
        try
        {
            await _bot.DeleteMessage(message.Chat, message.MessageId, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось удалить сообщение пользователя из блэклиста");
        }
        
        // Баним пользователя на 4 часа (как в IntroFlowService)
        try
        {
            var banUntil = DateTime.UtcNow + TimeSpan.FromMinutes(240);
            await _bot.BanChatMember(chat.Id, user.Id, untilDate: banUntil, revokeMessages: true, cancellationToken: cancellationToken);
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
            try
            {
                var removedData = new SimpleNotificationData(user, message.Chat, "удален из списка одобренных после автобана по блэклисту");
                await _messageService.SendAdminNotificationAsync(AdminNotificationType.RemovedFromApproved, removedData, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось отправить уведомление об удалении из одобренных");
            }
        }
        
        _logger.LogInformation("✅ АВТОБАН ЗАВЕРШЕН: пользователь {User} (id={UserId}) забанен на 4 часа в чате '{ChatTitle}' (id={ChatId}) по блэклисту lols.bot", 
            FullName(user.FirstName, user.LastName), user.Id, message.Chat.Title, message.Chat.Id);
    }

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";

    private static string LinkToMessage(Chat chat, long messageId) =>
        chat.Type == ChatType.Supergroup ? LinkToSuperGroupMessage(chat, messageId)
        : chat.Username == null ? ""
        : LinkToGroupWithNameMessage(chat, messageId);

    private static string LinkToSuperGroupMessage(Chat chat, long messageId) => $"https://t.me/c/{chat.Id.ToString()[4..]}/{messageId}";

    private static string LinkToGroupWithNameMessage(Chat chat, long messageId) => $"https://t.me/{chat.Username}/{messageId}";

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
                    await _bot.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "DeleteMessage");
                }
            },
            cancellationToken
        );
    }

    /// <summary>
    /// Выполняет AI анализ профиля пользователя при первом сообщении
    /// </summary>
    /// <returns>true если пользователь получил ограничения за подозрительный профиль</returns>
    private async Task<bool> PerformAiProfileAnalysis(Message message, User user, Chat chat, CancellationToken cancellationToken)
    {
        _logger.LogDebug("🤖 Запускаем AI анализ профиля пользователя {UserId} ({UserName})", 
            user.Id, FullName(user.FirstName, user.LastName));
        
        try
        {
            var result = await _aiChecks.GetAttentionBaitProbability(user);
            _logger.LogInformation("🤖 AI анализ профиля: пользователь {UserId}, вероятность={Probability}, причина={Reason}", 
                user.Id, result.SpamProbability.Probability, result.SpamProbability.Reason);

            // Если высокая вероятность спама в профиле - даем ридонли
            if (result.SpamProbability.Probability > 0.7) // Порог можно настроить
            {
                _logger.LogWarning("🚫 AI определил подозрительный профиль: пользователь {UserId}, вероятность={Probability}", 
                    user.Id, result.SpamProbability.Probability);

                // Удаляем сообщение пользователя
                try
                {
                    await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить сообщение при AI анализе");
                }

                // Даем ридонли на 10 минут
                try
                {
                    var untilDate = DateTime.UtcNow.AddMinutes(10);
                    await _bot.RestrictChatMember(
                        chat.Id, 
                        user.Id, 
                        new ChatPermissions
                        {
                            CanSendMessages = false,
                            CanSendAudios = false,
                            CanSendDocuments = false,
                            CanSendPhotos = false,
                            CanSendVideos = false,
                            CanSendVideoNotes = false,
                            CanSendVoiceNotes = false,
                            CanSendPolls = false,
                            CanSendOtherMessages = false,
                            CanAddWebPagePreviews = false,
                            CanChangeInfo = false,
                            CanInviteUsers = false,
                            CanPinMessages = false,
                            CanManageTopics = false
                        },
                        untilDate: (DateTime?)untilDate,
                        cancellationToken: cancellationToken
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось дать ридонли пользователю");
                }

                // Отправляем красивое уведомление в админ-чат
                var aiProfileData = new AiProfileAnalysisData(
            user, 
            chat, 
            result.SpamProbability.Probability, 
            result.SpamProbability.Reason, 
            result.NameBio, 
            message.Text ?? message.Caption ?? "[медиа]", 
            result.Photo, 
            message.MessageId
        );
        await _messageService.SendAiProfileAnalysisAsync(aiProfileData, cancellationToken);

                _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");
                _userFlowLogger.LogUserRestricted(user, chat, $"AI анализ профиля: {result.SpamProbability.Reason}", TimeSpan.FromMinutes(10));
                return true; // Возвращаем true - пользователь получил ограничения
            }
            else
            {
                _logger.LogDebug("✅ AI анализ: профиль пользователя {UserId} выглядит безопасно (вероятность={Probability})", 
                    user.Id, result.SpamProbability.Probability);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Ошибка при AI анализе профиля пользователя {UserId}", user.Id);
            // Продолжаем выполнение даже при ошибке AI анализа
        }

        return false; // Возвращаем false - профиль безопасен, продолжаем модерацию
    }


} 