using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Handlers.Commands;

/// <summary>
/// Обработчик команд для управления подозрительными пользователями
/// </summary>
public class SuspiciousCommandHandler : ICommandHandler
{
    private readonly TelegramBotClient _bot;
    private readonly IModerationService _moderationService;
    private readonly ILogger<SuspiciousCommandHandler> _logger;

    public string CommandName => "suspicious";

    public SuspiciousCommandHandler(
        TelegramBotClient bot, 
        IModerationService moderationService,
        ILogger<SuspiciousCommandHandler> logger)
    {
        _bot = bot;
        _moderationService = moderationService;
        _logger = logger;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        // Проверяем, что команда пришла из админ-чата
        if (message.Chat.Id != Config.AdminChatId && message.Chat.Id != Config.LogAdminChatId)
            return;

        var commandParts = message.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        if (commandParts.Length < 2)
        {
            await ShowHelp(message, cancellationToken);
            return;
        }

        var subCommand = commandParts[1].ToLower();
        
        try
        {
            switch (subCommand)
            {
                case "stats":
                    await HandleStatsCommand(message, cancellationToken);
                    break;
                    
                case "list":
                    await HandleListCommand(message, cancellationToken);
                    break;
                    
                case "help":
                default:
                    await ShowHelp(message, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке команды /suspicious {SubCommand}", subCommand);
            await _bot.SendMessage(
                message.Chat.Id,
                "❌ Произошла ошибка при выполнении команды",
                cancellationToken: cancellationToken
            );
        }
    }

    private async Task HandleStatsCommand(Message message, CancellationToken cancellationToken)
    {
        var (totalSuspicious, withAiDetect, groupsCount) = _moderationService.GetSuspiciousUsersStats();

        var statsText = $"📊 *Статистика подозрительных пользователей*\n\n" +
                       $"👥 Всего подозрительных: *{totalSuspicious}*\n" +
                       $"🔍 С включенным AI детектом: *{withAiDetect}*\n" +
                       $"🏠 Затронуто групп: *{groupsCount}*\n\n" +
                       $"⚙️ Настройки:\n" +
                       $"• Система включена: {(Config.SuspiciousDetectionEnabled ? "✅" : "❌")}\n" +
                       $"• Порог мимикрии: *{Config.MimicryThreshold:F1}*\n" +
                       $"• Сообщений для одобрения: *{Config.SuspiciousToApprovedMessageCount}*";

        await _bot.SendMessage(
            message.Chat.Id,
            statsText,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("Отправлена статистика подозрительных пользователей в админ-чат");
    }

    private async Task HandleListCommand(Message message, CancellationToken cancellationToken)
    {
        var aiDetectUsers = _moderationService.GetAiDetectUsers();

        if (aiDetectUsers.Count == 0)
        {
            await _bot.SendMessage(
                message.Chat.Id,
                "📝 *Список пользователей с AI детектом*\n\n" +
                "Нет пользователей с включенным AI детектом.",
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken
            );
            return;
        }

        var listText = $"📝 *Пользователи с включенным AI детектом* ({aiDetectUsers.Count})\n\n";

        for (int i = 0; i < Math.Min(aiDetectUsers.Count, 10); i++) // Показываем максимум 10
        {
            var (userId, chatId) = aiDetectUsers[i];
            listText += $"{i + 1}. ID: `{userId}` в чате `{chatId}`\n";
        }

        if (aiDetectUsers.Count > 10)
        {
            listText += $"\n... и ещё {aiDetectUsers.Count - 10} пользователей";
        }

        await _bot.SendMessage(
            message.Chat.Id,
            listText,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation("Отправлен список пользователей с AI детектом в админ-чат");
    }

    private async Task ShowHelp(Message message, CancellationToken cancellationToken)
    {
        var helpText = """
🔍 *Команды управления подозрительными пользователями*

/suspicious stats - показать статистику
/suspicious list - список пользователей с AI детектом  
/suspicious help - эта справка

*Описание системы:*
Система автоматически анализирует первые 3 сообщения новых пользователей на предмет шаблонности и мимикрии. Подозрительные пользователи переводятся в промежуточный статус и требуют дополнительных хороших сообщений для одобрения.

Для особо подозрительных пользователей администраторы могут включить AI детект, который будет пересылать все их сообщения в админ-чат для ручного анализа.
""";

        await _bot.SendMessage(
            message.Chat.Id,
            helpText,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken
        );
    }
} 