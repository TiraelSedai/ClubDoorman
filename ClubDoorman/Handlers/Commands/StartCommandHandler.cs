using ClubDoorman.Infrastructure;
using ClubDoorman.Services;
using ClubDoorman.Models.Notifications;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Handlers.Commands;

/// <summary>
/// Обработчик команды /start
/// </summary>
public class StartCommandHandler : ICommandHandler
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<StartCommandHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IAppConfig _appConfig;

    public string CommandName => "start";

    public StartCommandHandler(ITelegramBotClientWrapper bot, ILogger<StartCommandHandler> logger, IMessageService messageService, IAppConfig appConfig)
    {
        _bot = bot;
        _logger = logger;
        _messageService = messageService;
        _appConfig = appConfig;
    }

    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        if (message.Chat.Type != ChatType.Private)
            return;

        // Если whitelist активен - не отвечаем в личке
        if (!_appConfig.IsPrivateStartAllowed())
        {
            _logger.LogDebug("Команда /start в личке отключена - активен whitelist");
            return;
        }

        var about = GetStartMessage();
        await _messageService.SendUserNotificationAsync(
            message.From!, 
            message.Chat, 
            UserNotificationType.Welcome, 
            new SimpleNotificationData(message.From!, message.Chat, about), 
            cancellationToken
        );
    }

    private static string GetStartMessage()
    {
        return """
<b>👋 Привет! Я современный антиспам-бот для Telegram</b>

Защищаю <b>группы</b> и <b>каналы с обсуждениями</b> от спама, флуда и нежелательных участников.

━━━━━━━━━━━━━━━

<b>🛡️ МНОГОУРОВНЕВАЯ ЗАЩИТА</b>

Во всех чатах работают одинаковые фильтры:
• Проверка в базах спамеров
• Фильтрация первых 3 сообщений
• ML-анализ текста на спам
• Блокировка стоп-слов и подозрительных ссылок

<b>Отличие только в первичной проверке:</b>

<b>📺 Каналы с обсуждениями:</b>
AI-анализ профилей (фото + описание) — никаких капч!

<b>👥 Обычные группы:</b>
Капча для новых участников (60 секунд)

━━━━━━━━━━━━━━━

<b>🔄 Как это работает:</b>

<b>1.</b> Новый участник → проверка в базах спамеров
<b>2.</b> <b>Капча</b> (группы) или <b>AI-проверка профиля</b> (каналы)
<b>3.</b> Модерация первых сообщений (ML + фильтры)
<b>4.</b> После 3 хороших сообщений — полная свобода

<a href="https://telegra.ph/GateTroitsBot-04-19">📖 Подробная документация</a>

━━━━━━━━━━━━━━━

<b>⚡ Быстрое подключение:</b>

1️⃣ Добавьте бота в чат как админа
2️⃣ Дайте права на удаление сообщений и бан
3️⃣ Готово! Защита уже работает

━━━━━━━━━━━━━━━

<b>💰 ТАРИФЫ</b>

🆓 <b>Бесплатно:</b> все функции + ограниченный LLM анализ + реклама

🔥 <b>Без рекламы</b> — <b>$5 навсегда</b>
   Отключение рекламы в одной группе

📺 <b>Канал PRO</b> — <b>$5/год*</b>
   Расширенный AI-анализ профилей + без рекламы

💎 <b>Премиум</b> — <b>от $12/год</b>
   Персональная копия бота с ML-датасетом
   AI для всех каналов + админ-чат

<i>* Для больших каналов (5К+ участников, 50+ новых в день) — индивидуальные тарифы</i>

💳 <b>Заказ:</b> @momai

━━━━━━━━━━━━━━━

<b>💡 Важно знать:</b>

<b>👤 Новичкам:</b>
   • Первые 3 сообщения — только текст
   • Без ссылок, медиа, эмодзи
   
<b>👑 Админам:</b>
   • Можно дать права не сразу — бот изучит активных участников
   • Рекомендуется подождать 3-4 дня для сбора статистики

━━━━━━━━━━━━━━━

<b>🔗 Полезные ссылки:</b>

📖 <a href="https://telegra.ph/GateTroitsBot-04-19">Документация</a>
💻 <a href="https://github.com/momai/ClubDoorman">Исходный код</a>
💬 Поддержка: @momai

━━━━━━━━━━━━━━━

<b>📢 О рекламе:</b>

Показываю только качественную рекламу. Никакого шлака, бурмалды и серых схем не будет, обещаю 🤝

<b>🧼 Пусть ваш чат будет чистым и уютным!</b>
""";
    }
} 