using System.Collections.Concurrent;
using ClubDoorman.Models;
using ClubDoorman.Infrastructure;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис для работы с капчей
/// </summary>
public class CaptchaService : ICaptchaService
{
    private readonly ConcurrentDictionary<string, CaptchaInfo> _captchaNeededUsers = new();
    private readonly TelegramBotClient _bot;
    private readonly ILogger<CaptchaService> _logger;

    // Черный список имен для отображения
    private readonly List<string> _namesBlacklist = ["p0rn", "porn", "порн", "п0рн", "pоrn", "пoрн", "bot"];

    public CaptchaService(TelegramBotClient bot, ILogger<CaptchaService> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public async Task<CaptchaInfo> CreateCaptchaAsync(Chat chat, User user, Message? userJoinMessage = null)
    {
        const int challengeLength = 8;
        var correctAnswerIndex = Random.Shared.Next(challengeLength);
        var challenge = new List<int>(challengeLength);
        
        while (challenge.Count < challengeLength)
        {
            var rand = Random.Shared.Next(Captcha.CaptchaList.Count);
            if (!challenge.Contains(rand))
                challenge.Add(rand);
        }
        
        var correctAnswer = challenge[correctAnswerIndex];
        var keyboard = challenge
            .Select(x => new InlineKeyboardButton(Captcha.CaptchaList[x].Emoji) 
            { 
                CallbackData = $"cap_{user.Id}_{x}" 
            })
            .ToList();

        ReplyParameters? replyParams = null;
        if (userJoinMessage != null)
            replyParams = userJoinMessage;

        var fullNameForDisplay = Utils.FullName(user);
        var fullNameLower = fullNameForDisplay.ToLowerInvariant();
        var username = user.Username?.ToLower();
        
        if (_namesBlacklist.Any(fullNameLower.Contains) || 
            username?.Contains("porn") == true || 
            username?.Contains("p0rn") == true)
        {
            fullNameForDisplay = "новый участник чата";
        }

        var welcomeMessage = $"Привет, <a href=\"tg://user?id={user.Id}\">{System.Net.WebUtility.HtmlEncode(fullNameForDisplay)}</a>! " +
                            $"Антиспам: на какой кнопке {Captcha.CaptchaList[correctAnswer].Description}?";

        // Добавляем заглушку для рекламы если нужно
        var isNoAdGroup = IsNoAdGroup(chat.Id);
        var vpnAdHtml = isNoAdGroup ? "" : "\n\n 📍 Место для рекламы\n<i>...</i>";
        welcomeMessage += vpnAdHtml;

        var captchaMessage = await _bot.SendMessage(
            chat.Id,
            welcomeMessage,
            parseMode: ParseMode.Html,
            replyParameters: replyParams,
            replyMarkup: new InlineKeyboardMarkup(keyboard)
        );

        var cts = new CancellationTokenSource();
        var captchaInfo = new CaptchaInfo(chat.Id, chat.Title, DateTime.UtcNow, user, correctAnswer, cts, userJoinMessage);
        
        var key = GenerateKey(chat.Id, user.Id);
        _captchaNeededUsers.TryAdd(key, captchaInfo);

        // Автоматическое удаление капчи и бан через 1.2 минуты
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1.2), cts.Token);
                
                // Удаляем капчу из коллекции
                if (_captchaNeededUsers.TryRemove(key, out var expiredCaptcha))
                {
                    _logger.LogInformation("Пользователь {User} (id={UserId}) не прошёл капчу (таймаут) в группе '{ChatTitle}' (id={ChatId})", 
                        Utils.FullName(expiredCaptcha.User), expiredCaptcha.User.Id, expiredCaptcha.ChatTitle ?? "-", expiredCaptcha.ChatId);
                    
                    // Баним пользователя на 20 минут
                    await _bot.BanChatMember(expiredCaptcha.ChatId, expiredCaptcha.User.Id, 
                        DateTime.UtcNow + TimeSpan.FromMinutes(20), revokeMessages: false);
                    
                    // Удаляем сообщения
                    await _bot.DeleteMessage(chat.Id, captchaMessage.MessageId);
                    if (userJoinMessage != null)
                        await _bot.DeleteMessage(chat.Id, userJoinMessage.MessageId);
                    
                    // Разбан через 20 минут
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(20));
                            await _bot.UnbanChatMember(expiredCaptcha.ChatId, expiredCaptcha.User.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка при разбане пользователя {UserId}", expiredCaptcha.User.Id);
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальная отмена
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при автоудалении капчи");
            }
        });

        return captchaInfo;
    }

    public async Task<bool> ValidateCaptchaAsync(string key, int answer)
    {
        if (!_captchaNeededUsers.TryRemove(key, out var info))
        {
            _logger.LogWarning("Капча {Key} не найдена", key);
            return false;
        }

        await info.Cts.CancelAsync();
        return info.CorrectAnswer == answer;
    }

    public CaptchaInfo? GetCaptchaInfo(string key)
    {
        return _captchaNeededUsers.TryGetValue(key, out var info) ? info : null;
    }

    public bool RemoveCaptcha(string key)
    {
        return _captchaNeededUsers.TryRemove(key, out _);
    }

    public string GenerateKey(long chatId, long userId)
    {
        return $"{chatId}_{userId}";
    }

    public async Task BanExpiredCaptchaUsersAsync()
    {
        if (_captchaNeededUsers.IsEmpty)
            return;

        var now = DateTime.UtcNow;
        var users = _captchaNeededUsers.ToArray();
        
        foreach (var (key, captchaInfo) in users)
        {
            var minutes = (now - captchaInfo.Timestamp).TotalMinutes;
            if (minutes > 1.3)
            {
                _logger.LogInformation("Пользователь {User} (id={UserId}) не прошёл капчу (таймаут) в группе '{ChatTitle}' (id={ChatId})", 
                    Utils.FullName(captchaInfo.User), captchaInfo.User.Id, captchaInfo.ChatTitle ?? "-", captchaInfo.ChatId);

                _captchaNeededUsers.TryRemove(key, out _);
                
                try
                {
                    await _bot.BanChatMember(captchaInfo.ChatId, captchaInfo.User.Id, 
                        now + TimeSpan.FromMinutes(20), revokeMessages: false);
                    
                    if (captchaInfo.UserJoinedMessage != null)
                        await _bot.DeleteMessage(captchaInfo.ChatId, captchaInfo.UserJoinedMessage.MessageId);

                    // Разбан через некоторое время
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(20));
                            await _bot.UnbanChatMember(captchaInfo.ChatId, captchaInfo.User.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Ошибка при разбане пользователя {UserId}", captchaInfo.User.Id);
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при бане пользователя за просроченную капчу");
                }
            }
        }
    }

    private static bool IsNoAdGroup(long chatId)
    {
        return Config.NoVpnAdGroups.Contains(chatId);
    }
} 