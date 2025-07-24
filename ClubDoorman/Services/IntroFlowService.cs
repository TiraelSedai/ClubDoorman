using System.Diagnostics;
using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Models.Requests;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для обработки приветствия новых участников
/// </summary>
public class IntroFlowService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<IntroFlowService> _logger;
    private readonly ICaptchaService _captchaService;
    private readonly IUserManager _userManager;
    private readonly IAiChecks _aiChecks;
    private readonly IStatisticsService _statisticsService;
    private readonly GlobalStatsManager _globalStatsManager;
    private readonly IModerationService _moderationService;
    private readonly IMessageService _messageService;

    public IntroFlowService(
        ITelegramBotClientWrapper bot,
        ILogger<IntroFlowService> logger,
        ICaptchaService captchaService,
        IUserManager userManager,
        IAiChecks aiChecks,
        IStatisticsService statisticsService,
        GlobalStatsManager globalStatsManager,
        IModerationService moderationService,
        IMessageService messageService)
    {
        _bot = bot;
        _logger = logger;
        _captchaService = captchaService;
        _userManager = userManager;
        _aiChecks = aiChecks;
        _statisticsService = statisticsService;
        _globalStatsManager = globalStatsManager;
        _moderationService = moderationService;
        _messageService = messageService;
    }

    public async Task ProcessNewUserAsync(Message? userJoinMessage, User user, Chat? chat = default, CancellationToken cancellationToken = default)
    {
        chat = userJoinMessage?.Chat ?? chat;
        Debug.Assert(chat != null);
        
        // Проверка whitelist - если активен, работаем только в разрешённых чатах
        if (!Config.IsChatAllowed(chat.Id))
        {
            _logger.LogDebug("Чат {ChatId} ({ChatTitle}) не в whitelist - игнорируем IntroFlow", chat.Id, chat.Title);
            return;
        }
        
        // Проверяем длину имени
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        
        // Проверяем длину имени для обоих случаев
        if (fullName.Length > 40)
        {
            var isPermanent = fullName.Length > 75;
            await BanUserForLongName(
                userJoinMessage,
                user,
                fullName,
                isPermanent ? null : TimeSpan.FromMinutes(10),
                isPermanent ? "🚫 Перманентный бан" : "Автобан на 10 минут",
                isPermanent ? "экстремально" : "подозрительно",
                chat
            );
            return;
        }

        _logger.LogDebug("Intro flow {@User}", user);
        
        // Проверяем, является ли пользователь участником клуба
        var clubUser = await _userManager.GetClubUsername(user.Id);
        if (clubUser != null)
        {
            _logger.LogDebug("User is {Name} from club", clubUser);
            return;
        }

        var chatId = chat.Id;

        if (await BanIfBlacklisted(user, chat, userJoinMessage))
            return;

        // AI анализ профиля убран - теперь выполняется при первом сообщении в MessageHandler

        var key = _captchaService.GenerateKey(chatId, user.Id);
        if (_captchaService.GetCaptchaInfo(key) != null)
        {
            _logger.LogDebug("This user is already awaiting captcha challenge");
            return;
        }

        // Создаем капчу через сервис
        var captchaRequest = new CreateCaptchaRequest(chat, user, userJoinMessage);
        var captchaInfo = await _captchaService.CreateCaptchaAsync(captchaRequest);
        
        // Если капча отключена для этой группы, отправляем приветствие сразу
        if (captchaInfo == null)
        {
            _logger.LogInformation("[NO_CAPTCHA] Капча отключена для чата {ChatId} - отправляем приветствие сразу после проверок", chat.Id);
            var welcomeRequest = new SendWelcomeMessageRequest(user, chat, "приветствие без капчи", cancellationToken);
            await _messageService.SendWelcomeMessageAsync(welcomeRequest);
        }
        else
        {
            _globalStatsManager.IncCaptcha(chatId, chat.Title ?? "");
        }
    }

    private async Task BanUserForLongName(
        Message? userJoinMessage,
        User user,
        string fullName,
        TimeSpan? banDuration,
        string banType,
        string nameDescription,
        Chat? chat = default)
    {
        try
        {
            chat = userJoinMessage?.Chat ?? chat;
            Debug.Assert(chat != null);
            
            // Баним пользователя (если banDuration null - бан навсегда)
            await _bot.BanChatMember(
                chat.Id, 
                user.Id,
                banDuration.HasValue ? DateTime.UtcNow + banDuration.Value : null,
                revokeMessages: true  // Удаляем все сообщения пользователя
            );
            
            // Полная очистка из всех списков при перманентном бане
            if (!banDuration.HasValue)
            {
                _moderationService.CleanupUserFromAllLists(user.Id, chat.Id);
                _logger.LogInformation("🧹 Пользователь {UserId} очищен из всех списков после бана в IntroFlow", user.Id);
            }
            
            // Удаляем сообщение о входе
            if (userJoinMessage != null)
            {
                await _bot.DeleteMessage(userJoinMessage.Chat.Id, (int)userJoinMessage.MessageId);
            }

            // Логируем для статистики
            _statisticsService.IncrementLongNameBan(chat.Id);

            // Уведомляем админов
            await _messageService.SendAdminNotificationAsync(
                AdminNotificationType.AutoBan,
                new AutoBanNotificationData(user, chat, banType, $"{nameDescription} длинное имя пользователя ({fullName.Length} символов): {fullName}")
            );
            _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban user with long username");
        }
    }

    private async Task<bool> BanIfBlacklisted(User user, Chat chat, Message? userJoinMessage = null)
    {
        if (!await _userManager.InBanlist(user.Id))
            return false;

        try
        {
            _statisticsService.IncrementBlacklistBan(chat.Id);
            
            // Баним пользователя на 4 часа с параметром revokeMessages: true чтобы удалить все сообщения
            var banUntil = DateTime.UtcNow + TimeSpan.FromMinutes(240);
            await _bot.BanChatMember(chat.Id, user.Id, banUntil, revokeMessages: true);
            
            // Явно удаляем сообщение о входе в чат, если оно есть
            if (userJoinMessage != null)
            {
                try
                {
                    await _bot.DeleteMessage(chat.Id, userJoinMessage.MessageId);
                    _logger.LogDebug("Удалено сообщение о входе пользователя из блэклиста");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить сообщение о входе пользователя из блэклиста");
                }
            }
            
            // Удаляем из списка одобренных
            if (_userManager.RemoveApproval(user.Id, chat.Id, removeAll: true))
            {
                await _messageService.SendAdminNotificationAsync(
                    AdminNotificationType.UserCleanup,
                    new UserCleanupNotificationData(user, chat, $"Пользователь {FullName(user.FirstName, user.LastName)} удален из списка одобренных после бана по блеклисту")
                );
            }
            
            _logger.LogInformation("Пользователь {User} (id={UserId}) из блэклиста забанен на 4 часа в чате {ChatTitle} (id={ChatId})", FullName(user.FirstName, user.LastName), user.Id, chat.Title, chat.Id);
            _globalStatsManager.IncBan(chat.Id, chat.Title ?? "");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Unable to ban");
            await _messageService.SendAdminNotificationAsync(
                AdminNotificationType.Warning,
                new SimpleNotificationData(new User { Id = 0, FirstName = "System" }, chat, $"Не могу забанить юзера из блеклиста в чате {chat.Title}. Не хватает могущества? Сходите забаньте руками.")
            );
        }

        return false;
    }

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";
} 