using System.Runtime.Caching;
using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services;

/// <summary>
/// Реализация сервиса для проверки прав бота в чатах
/// </summary>
public class BotPermissionsService : IBotPermissionsService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<BotPermissionsService> _logger;
    private readonly MemoryCache _cache = MemoryCache.Default;
    private const string CachePrefix = "bot_permissions_";
    private const int CacheMinutes = 30; // Кэшируем на 30 минут

    public BotPermissionsService(
        ITelegramBotClientWrapper bot,
        ILogger<BotPermissionsService> logger)
    {
        _bot = bot ?? throw new ArgumentNullException(nameof(bot));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Проверяет, имеет ли бот права администратора в чате
    /// </summary>
    public async Task<bool> IsBotAdminAsync(long chatId, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatMember = await GetBotChatMemberAsync(chatId, cancellationToken);
            return chatMember?.Status == ChatMemberStatus.Administrator;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось проверить права администратора бота в чате {ChatId}", chatId);
            return false;
        }
    }

    /// <summary>
    /// Проверяет, работает ли бот в тихом режиме в данном чате
    /// </summary>
    public async Task<bool> IsSilentModeAsync(long chatId, CancellationToken cancellationToken = default)
    {
        // Админ-чаты всегда работают в обычном режиме
        if (chatId == Config.AdminChatId || chatId == Config.LogAdminChatId)
            return false;

        // Приватные чаты не поддерживают тихий режим
        try
        {
            var chat = await _bot.GetChat(chatId, cancellationToken);
            if (chat.Type == ChatType.Private)
                return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить информацию о чате {ChatId}", chatId);
            return false;
        }

        // Проверяем права администратора
        return !await IsBotAdminAsync(chatId, cancellationToken);
    }

    /// <summary>
    /// Получает информацию о правах бота в чате
    /// </summary>
    public async Task<ChatMember?> GetBotChatMemberAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CachePrefix}{chatId}";
        
        // Проверяем кэш
        if (_cache.Get(cacheKey) is ChatMember cachedMember)
        {
            return cachedMember;
        }

        try
        {
            var botId = _bot.BotId;
            var chatMember = await _bot.GetChatMember(chatId, botId, cancellationToken);
            
            // Кэшируем результат
            var cachePolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(CacheMinutes)
            };
            _cache.Set(cacheKey, chatMember, cachePolicy);
            
            _logger.LogDebug("Получена информация о правах бота в чате {ChatId}: {Status}", chatId, chatMember.Status);
            return chatMember;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось получить информацию о правах бота в чате {ChatId}", chatId);
            
            // Кэшируем ошибку на короткое время, чтобы не спамить API
            var cachePolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(5)
            };
            _cache.Set(cacheKey, (ChatMember?)null, cachePolicy);
            
            return null;
        }
    }
} 