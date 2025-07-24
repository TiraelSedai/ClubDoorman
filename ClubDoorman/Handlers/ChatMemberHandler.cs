using System.Diagnostics;
using System.Runtime.Caching;
using ClubDoorman.Infrastructure;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Extensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Handlers;

/// <summary>
/// Обработчик изменений участников чата
/// </summary>
public class ChatMemberHandler : IUpdateHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly IUserManager _userManager;
    private readonly ILogger<ChatMemberHandler> _logger;
    private readonly IntroFlowService _introFlowService;
    private readonly IMessageService _messageService;
    private readonly IAppConfig _appConfig;

    public ChatMemberHandler(
        ITelegramBotClientWrapper bot,
        IUserManager userManager,
        ILogger<ChatMemberHandler> logger,
        IntroFlowService introFlowService,
        IMessageService messageService,
        IAppConfig appConfig)
    {
        _bot = bot;
        _userManager = userManager;
        _logger = logger;
        _introFlowService = introFlowService;
        _messageService = messageService;
        _appConfig = appConfig;
    }

    public bool CanHandle(Update update) => update.Type == UpdateType.ChatMember;

    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        var chatMember = update.ChatMember;
        Debug.Assert(chatMember != null);
        var newChatMember = chatMember.NewChatMember;
        ChatSettingsManager.EnsureChatInConfig(chatMember.Chat.Id, chatMember.Chat.Title);
        
        // Проверка whitelist - если активен, работаем только в разрешённых чатах
        if (!_appConfig.IsChatAllowed(chatMember.Chat.Id))
        {
            _logger.LogDebug("Чат {ChatId} ({ChatTitle}) не в whitelist - игнорируем изменение участника", chatMember.Chat.Id, chatMember.Chat.Title);
            return;
        }
        
        // Игнорируем изменения, сделанные самим ботом
        if (chatMember.From?.Id == _bot.BotId)
        {
            _logger.LogDebug("Игнорируем изменение статуса участника, сделанное самим ботом");
            return;
        }
        
        switch (newChatMember.Status)
        {
            case ChatMemberStatus.Member:
            {
                _logger.LogDebug("New chat member new {@New} old {@Old}", newChatMember, chatMember.OldChatMember);
                if (chatMember.OldChatMember.Status == ChatMemberStatus.Left)
                {
                    var u = newChatMember.User;
                    _logger.LogInformation("==================== НОВЫЙ УЧАСТНИК ====================\nПользователь {User} (id={UserId}, username={Username}) зашел в группу '{ChatTitle}' (id={ChatId})\n========================================================", 
                        (u.FirstName + (string.IsNullOrEmpty(u.LastName) ? "" : " " + u.LastName)), u.Id, u.Username ?? "-", chatMember.Chat.Title ?? "-", chatMember.Chat.Id);
                    
                    // Запускаем IntroFlow через сервис
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        await _introFlowService.ProcessNewUserAsync(null, newChatMember.User, chatMember.Chat);
                    });
                }
                break;
            }
            case ChatMemberStatus.Kicked
            or ChatMemberStatus.Restricted:
                var user = newChatMember.User;
                var key = $"{chatMember.Chat.Id}_{user.Id}";
                var lastMessage = MemoryCache.Default.Get(key) as string;
                var tailMessage = string.IsNullOrWhiteSpace(lastMessage)
                    ? ""
                    : $" Его/её последним сообщением было:\n```\n{lastMessage}\n```";
                
                // Удаляем из списка доверенных
                if (_userManager.RemoveApproval(user.Id, chatMember.Chat.Id, removeAll: true))
                {
                    var removedData = new UserRemovedFromApprovedNotificationData(
                        user, chatMember.Chat, "удален из списка одобренных после получения ограничений", 0, chatMember.Chat.Title ?? "");
                    await _messageService.SendAdminNotificationAsync(
                        AdminNotificationType.UserRemovedFromApproved,
                        removedData,
                        cancellationToken
                    );
                }
                
                var restrictedData = new UserRestrictedNotificationData(
                    user, chatMember.Chat, "пользователь получил ограничения", 0, lastMessage, chatMember.Chat.Title ?? "");
                await _messageService.SendAdminNotificationAsync(
                    AdminNotificationType.UserRestricted,
                    restrictedData,
                    cancellationToken
                );
                break;
        }
    }

    private static string FullName(string firstName, string? lastName) =>
        string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";
} 