using System.Runtime.Caching;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Handlers;

/// <summary>
/// Обработчик callback запросов
/// </summary>
public class CallbackQueryHandler : IUpdateHandler
{
    private readonly TelegramBotClient _bot;
    private readonly ICaptchaService _captchaService;
    private readonly IUserManager _userManager;
    private readonly BadMessageManager _badMessageManager;
    private readonly IStatisticsService _statisticsService;
    private readonly AiChecks _aiChecks;
    private readonly ILogger<CallbackQueryHandler> _logger;

    public CallbackQueryHandler(
        TelegramBotClient bot,
        ICaptchaService captchaService,
        IUserManager userManager,
        BadMessageManager badMessageManager,
        IStatisticsService statisticsService,
        AiChecks aiChecks,
        ILogger<CallbackQueryHandler> logger)
    {
        _bot = bot;
        _captchaService = captchaService;
        _userManager = userManager;
        _badMessageManager = badMessageManager;
        _statisticsService = statisticsService;
        _aiChecks = aiChecks;
        _logger = logger;
    }

    public bool CanHandle(Update update)
    {
        return update.CallbackQuery != null;
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        var callbackQuery = update.CallbackQuery!;
        var cbData = callbackQuery.Data;
        
        if (string.IsNullOrEmpty(cbData))
            return;

        var message = callbackQuery.Message;
        if (message == null)
            return;

        try
        {
            if (message.Chat.Id == Config.AdminChatId)
            {
                await HandleAdminCallback(callbackQuery, cancellationToken);
            }
            else
            {
                await HandleCaptchaCallback(callbackQuery, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке callback {Data}", cbData);
            await _bot.AnswerCallbackQuery(callbackQuery.Id, "Произошла ошибка", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleCaptchaCallback(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var cbData = callbackQuery.Data!;
        var message = callbackQuery.Message!;
        var chat = message.Chat;

        // Парсим данные капчи: cap_{user.Id}_{x}
        var split = cbData.Split('_');
        if (split.Length < 3 || split[0] != "cap")
            return;

        if (!long.TryParse(split[1], out var userId) || !int.TryParse(split[2], out var chosen))
            return;

        // Проверяем, что callback от того же пользователя
        if (callbackQuery.From.Id != userId)
        {
            await _bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
            return;
        }

        var key = _captchaService.GenerateKey(chat.Id, userId);
        var captchaInfo = _captchaService.GetCaptchaInfo(key);
        
        if (captchaInfo == null)
        {
            _logger.LogWarning("Капча {Key} не найдена в словаре", key);
            await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);
            return;
        }

        // Удаляем сообщение с капчей
        await _bot.DeleteMessage(chat.Id, message.MessageId, cancellationToken);

        // Проверяем правильность ответа
        var isCorrect = await _captchaService.ValidateCaptchaAsync(key, chosen);
        
        if (!isCorrect)
        {
            await HandleFailedCaptcha(captchaInfo, cancellationToken);
        }
        else
        {
            await HandleSuccessfulCaptcha(callbackQuery.From, chat, captchaInfo, cancellationToken);
        }
    }

    private async Task HandleFailedCaptcha(Models.CaptchaInfo captchaInfo, CancellationToken cancellationToken)
    {
        _logger.LogInformation("==================== КАПЧА НЕ ПРОЙДЕНА ====================\n" +
            "Пользователь {User} (id={UserId}) не прошёл капчу в группе '{ChatTitle}' (id={ChatId})\n" +
            "===========================================================", 
            Utils.FullName(captchaInfo.User), captchaInfo.User.Id, captchaInfo.ChatTitle ?? "-", captchaInfo.ChatId);

        _statisticsService.IncrementCaptcha(captchaInfo.ChatId);

        try
        {
            // Банируем на 20 минут
            await _bot.BanChatMember(
                captchaInfo.ChatId, 
                captchaInfo.User.Id, 
                DateTime.UtcNow + TimeSpan.FromMinutes(20), 
                revokeMessages: false,
                cancellationToken: cancellationToken
            );

            // Удаляем сообщение о входе
            if (captchaInfo.UserJoinedMessage != null)
            {
                await _bot.DeleteMessage(captchaInfo.ChatId, captchaInfo.UserJoinedMessage.MessageId, cancellationToken);
            }

            // Планируем разбан через 20 минут
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(20), cancellationToken);
                    await _bot.UnbanChatMember(captchaInfo.ChatId, captchaInfo.User.Id, cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при разбане пользователя {UserId}", captchaInfo.User.Id);
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось забанить пользователя за неправильную капчу");
        }
    }

    private async Task HandleSuccessfulCaptcha(User user, Chat chat, Models.CaptchaInfo captchaInfo, CancellationToken cancellationToken)
    {
        _logger.LogInformation("==================== КАПЧА ПРОЙДЕНА ====================\n" +
            "Пользователь {User} (id={UserId}) успешно прошёл капчу в группе '{ChatTitle}' (id={ChatId}) — показываем приветствие\n" +
            "========================================================", 
            Utils.FullName(user), user.Id, chat.Title ?? "-", chat.Id);

        // Создаем приветственное сообщение
        var displayName = !string.IsNullOrEmpty(user.FirstName)
            ? System.Net.WebUtility.HtmlEncode(Utils.FullName(user))
            : (!string.IsNullOrEmpty(user.Username) ? "@" + user.Username : "гость");
        
        var mention = $"<a href=\"tg://user?id={user.Id}\">{displayName}</a>";
        
        // Заглушка для рекламы (если группа не в исключениях)
        var isNoAdGroup = IsNoAdGroup(chat.Id);
        var vpnAd = isNoAdGroup ? "" : "\n\n\n📍 <b>Место для рекламы</b> \n <i>...</i>";
        
        string greetMsg;
        if (ChatSettingsManager.GetChatType(chat.Id) == "announcement")
        {
            greetMsg = $"👋 {mention}\n\n<b>Внимание:</b> первые три сообщения проходят антиспам-проверку, сообщения со стоп-словами и спамом будут удалены. Не просите писать в ЛС!{vpnAd}";
        }
        else
        {
            var mediaWarning = Config.IsMediaFilteringDisabledForChat(chat.Id) ? ", стикеры, документы" : ", изображения, стикеры, документы";
            greetMsg = $"👋 {mention}\n\n<b>Внимание!</b> первые три сообщения проходят антиспам-проверку, эмодзи{mediaWarning} и реклама запрещены — они могут удаляться автоматически. Не просите писать в ЛС!{vpnAd}";
        }

        var sent = await _bot.SendMessage(chat.Id, greetMsg, parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        
        // Удаляем приветствие через 20 секунд
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);
                await _bot.DeleteMessage(chat.Id, sent.MessageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить приветственное сообщение");
            }
        }, cancellationToken);
    }

    private async Task HandleAdminCallback(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var cbData = callbackQuery.Data!;
        var split = cbData.Split('_').ToList();

        try
        {
            if (split.Count > 1 && split[0] == "approve" && long.TryParse(split[1], out var approveUserId))
            {
                await HandleApproveUser(callbackQuery, approveUserId, cancellationToken);
            }
            else if (split.Count > 2 && split[0] == "ban" && long.TryParse(split[1], out var chatId) && long.TryParse(split[2], out var userId))
            {
                await HandleBanUser(callbackQuery, chatId, userId, cancellationToken);
            }
                    else if (split.Count > 1 && split[0] == "aiOk")
        {
            if (split.Count == 2 && long.TryParse(split[1], out var aiOkUserIdOld))
            {
                // Старый формат aiOk_{userId} - только кеширование
                await HandleAiOkUser(callbackQuery, null, aiOkUserIdOld, cancellationToken);
            }
            else if (split.Count == 3 && long.TryParse(split[1], out var aiOkChatId) && long.TryParse(split[2], out var aiOkUserId))
            {
                // Новый формат aiOk_{chatId}_{userId} - снятие ограничений + кеширование
                await HandleAiOkUser(callbackQuery, aiOkChatId, aiOkUserId, cancellationToken);
            }
        }
            else if (cbData == "noop")
            {
                // Ничего не делаем, просто убираем кнопки
                await _bot.EditMessageReplyMarkup(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, cancellationToken: cancellationToken);
            }

            await _bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке админского callback {Data}", cbData);
            await _bot.AnswerCallbackQuery(callbackQuery.Id, "Ошибка при выполнении действия", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleApproveUser(CallbackQuery callbackQuery, long userId, CancellationToken cancellationToken)
    {
        // Админ одобряет пользователя - всегда глобально
        await _userManager.Approve(userId);
        
        var adminName = GetAdminDisplayName(callbackQuery.From);
        await _bot.SendMessage(
            Config.AdminChatId,
            $"✅ {adminName} добавил пользователя в список доверенных",
            replyParameters: callbackQuery.Message?.MessageId,
            cancellationToken: cancellationToken
        );

        // Убираем кнопки
        await _bot.EditMessageReplyMarkup(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, cancellationToken: cancellationToken);
    }

    private async Task HandleBanUser(CallbackQuery callbackQuery, long chatId, long userId, CancellationToken cancellationToken)
    {
        var callbackDataBan = $"ban_{chatId}_{userId}";
        var userMessage = MemoryCache.Default.Remove(callbackDataBan) as Message;
        
        // Добавляем текст в список плохих сообщений
        var text = userMessage?.Caption ?? userMessage?.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            await _badMessageManager.MarkAsBad(text);
        }

        try
        {
            // Банируем пользователя
            await _bot.BanChatMember(new ChatId(chatId), userId, cancellationToken: cancellationToken);
            
            // Удаляем из одобренных
            if (_userManager.RemoveApproval(userId, chatId, removeAll: true))
            {
                await _bot.SendMessage(
                    Config.AdminChatId,
                    $"⚠️ Пользователь удален из списка одобренных после ручного бана администратором {GetAdminDisplayName(callbackQuery.From)}",
                    replyParameters: callbackQuery.Message?.MessageId,
                    cancellationToken: cancellationToken
                );
            }

            var adminName = GetAdminDisplayName(callbackQuery.From);
            await _bot.SendMessage(
                Config.AdminChatId,
                $"🚫 {adminName} забанил, сообщение добавлено в список авто-бана",
                replyParameters: callbackQuery.Message?.MessageId,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось забанить пользователя через админский callback");
            await _bot.SendMessage(
                Config.AdminChatId,
                "⚠️ Не могу забанить. Не хватает могущества? Сходите забаньте руками",
                replyParameters: callbackQuery.Message?.MessageId,
                cancellationToken: cancellationToken
            );
        }

        // Удаляем оригинальное сообщение пользователя
        try
        {
            if (userMessage != null)
                await _bot.DeleteMessage(userMessage.Chat, userMessage.MessageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось удалить оригинальное сообщение пользователя");
        }

        // Убираем кнопки
        await _bot.EditMessageReplyMarkup(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, cancellationToken: cancellationToken);
    }

    private async Task HandleAiOkUser(CallbackQuery callbackQuery, long? chatId, long userId, CancellationToken cancellationToken)
    {
        // Помечаем AI проверку как безопасную
        _aiChecks.MarkUserOkay(userId);
        
        var adminName = GetAdminDisplayName(callbackQuery.From);
        var message = $"✅ {adminName} отметил профиль как безопасный - AI проверки отключены для этого пользователя";
        
        // Если передан chatId - пытаемся снять ограничения с пользователя
        if (chatId.HasValue)
        {
            try
            {
                // Снимаем все ограничения (возвращаем полные права)
                await _bot.RestrictChatMember(
                    chatId.Value,
                    userId,
                    new ChatPermissions
                    {
                        CanSendMessages = true,
                        CanSendAudios = true,
                        CanSendDocuments = true,
                        CanSendPhotos = true,
                        CanSendVideos = true,
                        CanSendVideoNotes = true,
                        CanSendVoiceNotes = true,
                        CanSendPolls = true,
                        CanSendOtherMessages = true,
                        CanAddWebPagePreviews = true,
                        CanChangeInfo = false, // Эти права обычно не даются обычным пользователям
                        CanInviteUsers = false,
                        CanPinMessages = false,
                        CanManageTopics = false
                    },
                    cancellationToken: cancellationToken
                );
                
                message += " + ограничения сняты";
                _logger.LogInformation("Ограничения сняты с пользователя {UserId} в чате {ChatId} администратором {AdminName}", 
                    userId, chatId.Value, adminName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось снять ограничения с пользователя {UserId} в чате {ChatId}", userId, chatId.Value);
                message += " (не удалось снять ограничения - возможно, недостаточно прав)";
            }
        }
        
        await _bot.SendMessage(
            Config.AdminChatId,
            message,
            replyParameters: callbackQuery.Message?.MessageId,
            cancellationToken: cancellationToken
        );

        // Убираем кнопки
        await _bot.EditMessageReplyMarkup(callbackQuery.Message!.Chat.Id, callbackQuery.Message.MessageId, cancellationToken: cancellationToken);
    }

    private static string GetAdminDisplayName(User user)
    {
        return !string.IsNullOrEmpty(user.Username)
            ? user.Username
            : Utils.FullName(user);
    }

    private static bool IsNoAdGroup(long chatId)
    {
        return Config.NoVpnAdGroups.Contains(chatId);
    }
} 