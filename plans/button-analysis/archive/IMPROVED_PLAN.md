# Улучшенный план рефакторинга кнопок

## Анализ ChatGPT: ✅ Все предложения правильные

### Превосходные решения (оставить как есть):
- ✅ **Типизация `CallbackActionType` + `BanContext`** — очень мощно
- ✅ **`ParsedCallbackData.Parse()` с поддержкой старых форматов** — ключ к плавному переходу
- ✅ **Инкапсуляция логики кнопок в `ServiceChatDispatcher`** — правильно, минимизирует churn
- ✅ **SRP через `ICallbackActionHandler`** — масштабируемость без боли
- ✅ **Миграция через `_serviceChatDispatcher.Create...`** — DX-удобно и читаемо
- ✅ **Тест-план с проверкой неработающих кнопок** — без этого рефакторинг опасен

### Рекомендации ChatGPT (принимаем):

#### 1. ✳ `ParsedCallbackData.Parse()` — сделать fail-tolerant
```csharp
public static bool TryParse(string data, out ParsedCallbackData result, out string error)
```

#### 2. 📁 Структура файлов — завести `Callback/` поддиректорию
```
ClubDoorman/
├── Callback/
│   ├── ParsedCallbackData.cs
│   ├── CallbackActionType.cs
│   ├── BanContext.cs
│   ├── ICallbackActionHandler.cs
│   └── Handlers/
│       ├── BanActionHandler.cs
│       └── ...
```

#### 3. 🧪 Тестирование `ServiceChatDispatcher`
- Правильность `CallbackData` (snapshot-тесты)
- `InlineKeyboardMarkup`-структуру (2D-массивы)
- Интеграцию в `GetAdminChatReplyMarkup`

#### 4. 📘 Документация: `.md` с примерами callbackData
Создать `docs/buttons.md` с форматами

## Анализ ServiceChatDispatcher: 397 строк - нужна реструктуризация

### Текущая структура:
```
ServiceChatDispatcher.cs (397 строк)
├── SendToAdminChatAsync()
├── SendToLogChatAsync()
├── ShouldSendToAdminChat()
├── GetAdminChatReplyMarkup() ← логика кнопок
├── FormatSuspiciousMessage()
├── FormatSuspiciousUser()
├── FormatAiDetect()
├── FormatAiProfileAnalysis()
├── SendAiProfileAnalysisWithPhoto()
├── FormatPrivateChatBanAttempt()
├── FormatChannelMessage()
├── FormatUserRestricted()
├── FormatUserRemovedFromApproved()
├── FormatError()
├── FormatAutoBanLog()
├── FormatAiDetectLog()
├── FormatGenericLogNotification()
├── FormatGenericNotification()
├── FormatUser()
├── FormatChat()
└── FormatMessageLink()
```

### Проблемы:
- ❌ **397 строк** - слишком много для одного файла
- ❌ **Смешанная ответственность** - диспетчеризация + форматирование + кнопки
- ❌ **Дублирование логики** - кнопки создаются в разных местах
- ❌ **Сложность тестирования** - много методов в одном классе

## Улучшенный план рефакторинга

### Этап 1: Критическое исправление (ПРИОРИТЕТ #1)

#### 1.1 Диагностика неработающих кнопок
- [ ] Найти конкретные места, где кнопки не работают
- [ ] Создать минимальный тест для воспроизведения
- [ ] Определить причину (неправильный формат, ошибка парсинга)

#### 1.2 Быстрое исправление
- [ ] Исправить обработку в `HandleAdminCallback`
- [ ] Убедиться, что все 515 тестов проходят
- [ ] Протестировать в реальных условиях

### Этап 2: Типизация и структура (без изменения архитектуры)

#### 2.1 Создать структуру Callback/
```
ClubDoorman/
├── Callback/
│   ├── Models/
│   │   ├── ParsedCallbackData.cs
│   │   ├── CallbackActionType.cs
│   │   └── BanContext.cs
│   ├── Interfaces/
│   │   └── ICallbackActionHandler.cs
│   └── Handlers/
│       ├── BanActionHandler.cs
│       ├── ApproveActionHandler.cs
│       ├── SuspiciousActionHandler.cs
│       └── CaptchaActionHandler.cs
```

#### 2.2 Добавить enum'ы для callback actions
```csharp
// ClubDoorman/Callback/Models/CallbackActionType.cs
public enum CallbackActionType
{
    Ban,
    BanProfile, 
    Approve,
    AiOk,
    SuspiciousApprove,
    SuspiciousBan,
    SuspiciousAi,
    Noop,
    Captcha
}

// ClubDoorman/Callback/Models/BanContext.cs
public enum BanContext
{
    Default,    // обычный бан сообщения
    Profile,    // бан за профиль
    Mimicry     // бан за мимикрию
}
```

#### 2.3 Создать ParsedCallbackData с fail-tolerant парсингом
```csharp
// ClubDoorman/Callback/Models/ParsedCallbackData.cs
public class ParsedCallbackData
{
    public CallbackActionType Action { get; set; }
    public BanContext? BanContext { get; set; }
    public long ChatId { get; set; }
    public long UserId { get; set; }
    public int? MessageId { get; set; }
    public int? ChosenIndex { get; set; } // для капчи
    
    public static ParsedCallbackData Parse(string callbackData)
    {
        if (TryParse(callbackData, out var result, out var error))
            return result;
        
        throw new ArgumentException($"Invalid callback data: {error}", nameof(callbackData));
    }
    
    public static bool TryParse(string data, out ParsedCallbackData result, out string error)
    {
        result = null!;
        error = string.Empty;
        
        if (string.IsNullOrEmpty(data))
        {
            error = "Callback data is null or empty";
            return false;
        }
        
        try
        {
            // Парсинг с валидацией и поддержкой старых форматов
            var parts = data.Split('_');
            
            // Поддержка старых форматов
            if (parts.Length >= 2 && parts[0] == "ban" && long.TryParse(parts[1], out var chatId))
            {
                if (parts.Length >= 3 && long.TryParse(parts[2], out var userId))
                {
                    result = new ParsedCallbackData
                    {
                        Action = CallbackActionType.Ban,
                        ChatId = chatId,
                        UserId = userId,
                        BanContext = parts.Length > 3 && parts[3] == "mimicry" ? BanContext.Mimicry : BanContext.Default
                    };
                    return true;
                }
            }
            
            // Новые форматы
            if (parts.Length >= 2 && Enum.TryParse<CallbackActionType>(parts[0], true, out var action))
            {
                result = new ParsedCallbackData { Action = action };
                
                switch (action)
                {
                    case CallbackActionType.Ban:
                        if (parts.Length >= 4 && long.TryParse(parts[1], out var banChatId) && long.TryParse(parts[2], out var banUserId))
                        {
                            result.ChatId = banChatId;
                            result.UserId = banUserId;
                            result.BanContext = parts.Length > 3 ? ParseBanContext(parts[3]) : BanContext.Default;
                            return true;
                        }
                        break;
                        
                    case CallbackActionType.Approve:
                        if (parts.Length >= 2 && long.TryParse(parts[1], out var approveUserId))
                        {
                            result.UserId = approveUserId;
                            return true;
                        }
                        break;
                        
                    // ... другие случаи
                }
            }
            
            error = $"Unknown callback data format: {data}";
            return false;
        }
        catch (Exception ex)
        {
            error = $"Exception during parsing: {ex.Message}";
            return false;
        }
    }
    
    private static BanContext ParseBanContext(string context)
    {
        return context.ToLower() switch
        {
            "profile" => BanContext.Profile,
            "mimicry" => BanContext.Mimicry,
            _ => BanContext.Default
        };
    }
}
```

### Этап 3: Реструктуризация ServiceChatDispatcher

#### 3.1 Разбить на компоненты
```
ClubDoorman/Services/
├── ServiceChatDispatcher.cs (основной диспетчер)
├── NotificationFormatters/
│   ├── INotificationFormatter.cs
│   ├── SuspiciousMessageFormatter.cs
│   ├── SuspiciousUserFormatter.cs
│   ├── AiDetectFormatter.cs
│   └── AiProfileFormatter.cs
├── ButtonFactory/
│   ├── IButtonFactory.cs
│   └── ButtonFactory.cs
└── NotificationRouting/
    ├── INotificationRouter.cs
    └── NotificationRouter.cs
```

#### 3.2 Создать ButtonFactory
```csharp
// ClubDoorman/Services/ButtonFactory/IButtonFactory.cs
public interface IButtonFactory
{
    InlineKeyboardButton CreateBanButton(long userId, long chatId, BanContext context);
    InlineKeyboardButton CreateApproveButton(long userId);
    InlineKeyboardButton CreateSuspiciousButton(CallbackActionType action, long userId, long chatId, long messageId);
    InlineKeyboardButton CreateNoopButton();
    InlineKeyboardButton CreateCaptchaButton(long userId, int chosenIndex);
    
    // Методы для создания наборов кнопок
    InlineKeyboardMarkup CreateAdminActionButtons(long userId, long chatId, BanContext banContext);
    InlineKeyboardMarkup CreateSuspiciousUserButtons(long userId, long chatId, long messageId);
    InlineKeyboardMarkup CreateAiProfileButtons(long userId, long chatId);
}

// ClubDoorman/Services/ButtonFactory/ButtonFactory.cs
public class ButtonFactory : IButtonFactory
{
    public InlineKeyboardButton CreateBanButton(long userId, long chatId, BanContext context)
    {
        var callbackData = context switch
        {
            BanContext.Default => $"ban_{chatId}_{userId}",
            BanContext.Profile => $"banprofile_{chatId}_{userId}",
            BanContext.Mimicry => $"ban_{chatId}_{userId}_mimicry",
            _ => throw new ArgumentException($"Unknown ban context: {context}")
        };
        
        return new InlineKeyboardButton("🚫 Забанить") { CallbackData = callbackData };
    }
    
    public InlineKeyboardButton CreateApproveButton(long userId)
    {
        return new InlineKeyboardButton("✅ Одобрить") { CallbackData = $"approve_{userId}" };
    }
    
    public InlineKeyboardButton CreateSuspiciousButton(CallbackActionType action, long userId, long chatId, long messageId)
    {
        var callbackData = $"suspicious_{action.ToString().ToLower()}_{userId}_{chatId}_{messageId}";
        var text = action switch
        {
            CallbackActionType.SuspiciousApprove => "✅ Одобрить",
            CallbackActionType.SuspiciousBan => "🚫 Забанить",
            CallbackActionType.SuspiciousAi => "🔍 AI анализ вкл/выкл",
            _ => throw new ArgumentException($"Unknown suspicious action: {action}")
        };
        
        return new InlineKeyboardButton(text) { CallbackData = callbackData };
    }
    
    public InlineKeyboardButton CreateNoopButton()
    {
        return new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" };
    }
    
    public InlineKeyboardButton CreateCaptchaButton(long userId, int chosenIndex)
    {
        return new InlineKeyboardButton(Captcha.CaptchaList[chosenIndex].Emoji) 
        { 
            CallbackData = $"cap_{userId}_{chosenIndex}" 
        };
    }
    
    public InlineKeyboardMarkup CreateAdminActionButtons(long userId, long chatId, BanContext banContext)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                CreateBanButton(userId, chatId, banContext),
                CreateNoopButton(),
                CreateApproveButton(userId)
            }
        });
    }
    
    public InlineKeyboardMarkup CreateSuspiciousUserButtons(long userId, long chatId, long messageId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                CreateSuspiciousButton(CallbackActionType.SuspiciousApprove, userId, chatId, messageId),
                CreateSuspiciousButton(CallbackActionType.SuspiciousBan, userId, chatId, messageId)
            },
            new[]
            {
                CreateSuspiciousButton(CallbackActionType.SuspiciousAi, userId, chatId, messageId)
            }
        });
    }
    
    public InlineKeyboardMarkup CreateAiProfileButtons(long userId, long chatId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { CreateBanButton(userId, chatId, BanContext.Profile) },
            new[] { new InlineKeyboardButton("✅✅✅ ok") { CallbackData = $"aiOk_{chatId}_{userId}" } }
        });
    }
}
```

#### 3.3 Создать NotificationFormatters
```csharp
// ClubDoorman/Services/NotificationFormatters/INotificationFormatter.cs
public interface INotificationFormatter
{
    string Format(NotificationData notification);
    InlineKeyboardMarkup? GetReplyMarkup(NotificationData notification);
}

// ClubDoorman/Services/NotificationFormatters/SuspiciousUserFormatter.cs
public class SuspiciousUserFormatter : INotificationFormatter
{
    private readonly IButtonFactory _buttonFactory;
    
    public SuspiciousUserFormatter(IButtonFactory buttonFactory)
    {
        _buttonFactory = buttonFactory;
    }
    
    public string Format(NotificationData notification)
    {
        if (notification is not SuspiciousUserNotificationData suspicious)
            throw new ArgumentException("Expected SuspiciousUserNotificationData");
            
        return $"🤔 <b>Подозрительный пользователь</b>\n\n" +
               $"👤 Пользователь: {FormatUser(suspicious.User)}\n" +
               $"💬 Чат: {FormatChat(suspicious.Chat)}\n" +
               $"🎭 Оценка мимикрии: {suspicious.MimicryScore:F2}\n" +
               $"📝 Первые сообщения:\n{string.Join("\n", suspicious.FirstMessages.Take(3))}";
    }
    
    public InlineKeyboardMarkup? GetReplyMarkup(NotificationData notification)
    {
        if (notification is not SuspiciousUserNotificationData suspicious)
            return null;
            
        return _buttonFactory.CreateSuspiciousUserButtons(
            suspicious.User.Id, suspicious.Chat.Id, suspicious.MessageId);
    }
    
    private string FormatUser(User user) => $"{user.FirstName} {user.LastName}".Trim();
    private string FormatChat(Chat chat) => chat.Title ?? chat.Username ?? chat.Id.ToString();
}
```

#### 3.4 Упростить ServiceChatDispatcher
```csharp
// ClubDoorman/Services/ServiceChatDispatcher.cs (упрощенная версия)
public class ServiceChatDispatcher : IServiceChatDispatcher
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<ServiceChatDispatcher> _logger;
    private readonly IButtonFactory _buttonFactory;
    private readonly IEnumerable<INotificationFormatter> _formatters;
    
    public ServiceChatDispatcher(
        ITelegramBotClientWrapper bot,
        ILogger<ServiceChatDispatcher> logger,
        IButtonFactory buttonFactory,
        IEnumerable<INotificationFormatter> formatters)
    {
        _bot = bot;
        _logger = logger;
        _buttonFactory = buttonFactory;
        _formatters = formatters;
    }
    
    public async Task SendToAdminChatAsync(NotificationData notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var formatter = _formatters.FirstOrDefault(f => f.GetType().Name.Contains(notification.GetType().Name.Replace("NotificationData", "")));
            
            if (formatter != null)
            {
                var message = formatter.Format(notification);
                var replyMarkup = formatter.GetReplyMarkup(notification);
                
                await _bot.SendMessageAsync(
                    Config.AdminChatId,
                    message,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                    replyMarkup: replyMarkup,
                    cancellationToken: cancellationToken);
            }
            else
            {
                // Fallback для неизвестных типов
                await SendGenericNotification(notification, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомления в админ-чат");
            throw;
        }
    }
    
    // ... остальные методы упрощены
}
```

### Этап 4: Постепенная миграция

#### 4.1 Замена создания кнопок
```csharp
// В MessageHandler.cs заменить:
var keyboard = _buttonFactory.CreateAdminActionButtons(user.Id, message.Chat.Id, BanContext.Default);

// В ModerationService.cs заменить:
var keyboard = _buttonFactory.CreateSuspiciousUserButtons(user.Id, chat.Id, messageId);

// В CaptchaService.cs заменить:
var keyboard = new InlineKeyboardMarkup(challenge.Select(x => _buttonFactory.CreateCaptchaButton(user.Id, x)).ToList());
```

### Этап 5: Расширение обработчиков (опционально)

#### 5.1 Создать callback action handlers
```csharp
// ClubDoorman/Callback/Interfaces/ICallbackActionHandler.cs
public interface ICallbackActionHandler
{
    bool CanHandle(ParsedCallbackData data);
    Task HandleAsync(ParsedCallbackData data, CallbackQuery callbackQuery, CancellationToken cancellationToken);
}

// ClubDoorman/Callback/Handlers/BanActionHandler.cs
public class BanActionHandler : ICallbackActionHandler
{
    private readonly IUserManager _userManager;
    private readonly IBadMessageManager _badMessageManager;
    private readonly ILogger<BanActionHandler> _logger;
    
    public BanActionHandler(IUserManager userManager, IBadMessageManager badMessageManager, ILogger<BanActionHandler> logger)
    {
        _userManager = userManager;
        _badMessageManager = badMessageManager;
        _logger = logger;
    }
    
    public bool CanHandle(ParsedCallbackData data)
    {
        return data.Action == CallbackActionType.Ban || data.Action == CallbackActionType.BanProfile;
    }
    
    public async Task HandleAsync(ParsedCallbackData data, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        // Логика бана в зависимости от контекста
        if (data.BanContext == BanContext.Profile)
        {
            // Бан за профиль - не добавляем сообщение в badMessageManager
            await _userManager.BanUser(data.UserId);
        }
        else
        {
            // Обычный бан - добавляем сообщение в badMessageManager
            var message = MemoryCache.Default.Get($"{data.ChatId}_{data.UserId}") as string;
            if (!string.IsNullOrEmpty(message))
            {
                _badMessageManager.AddBadMessage(message);
            }
            await _userManager.BanUser(data.UserId);
        }
    }
}
```

## Документация

### docs/buttons.md
```markdown
# CallbackData formats (новая система)

## Бан
- `ban_{chatId}_{userId}` — обычный бан сообщения
- `banprofile_{chatId}_{userId}` — бан за профиль (не добавляет в badMessageManager)
- `ban_{chatId}_{userId}_mimicry` — бан за мимикрию

## Одобрение
- `approve_{userId}` — одобрить пользователя

## Подозрительные пользователи
- `suspicious_approve_{userId}_{chatId}_{messageId}` — одобрить подозрительного
- `suspicious_ban_{userId}_{chatId}_{messageId}` — забанить подозрительного
- `suspicious_ai_{userId}_{chatId}_{messageId}` — переключить AI детект

## AI
- `aiOk_{chatId}_{userId}` — AI анализ профиля OK

## Капча
- `cap_{userId}_{chosenIndex}` — ответ на капчу

## Системные
- `noop` — ничего не делать (пропуск)
```

## План тестирования

### 1. ParsedCallbackData
- [ ] Тесты для всех форматов callback data
- [ ] Тесты для TryParse с ошибками
- [ ] Тесты для обратной совместимости

### 2. ButtonFactory
- [ ] Тесты для каждого метода создания кнопок
- [ ] Snapshot-тесты для InlineKeyboardMarkup
- [ ] Тесты для правильности callback data

### 3. NotificationFormatters
- [ ] Тесты для каждого форматтера
- [ ] Тесты для интеграции с ButtonFactory

### 4. ServiceChatDispatcher
- [ ] Тесты для упрощенной версии
- [ ] Интеграционные тесты с реальными уведомлениями

## Принципы реализации

### 1. Использование существующей архитектуры
- **ServiceChatDispatcher** как основа (но упрощенный)
- **UpdateDispatcher** как центральная точка
- **TestFactory Pattern** для тестирования

### 2. Реструктуризация без революции
- Разбиваем большой файл на компоненты
- Сохраняем существующие интерфейсы
- Постепенная миграция

### 3. Fail-tolerant подход
- TryParse для безопасного парсинга
- Graceful degradation для ошибок
- Подробное логирование

### 4. DX-ориентированность
- Четкая структура файлов
- Документация с примерами
- Snapshot-тесты для UI 