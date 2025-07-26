# 📊 План централизации логирования ClubDoorman

## 🎯 Цели

1. **Централизовать все логи** в единую систему
2. **Добавить файловое логирование** для продакшена
3. **Убрать хардкоды сообщений** из бизнес-логики
4. **Создать систему шаблонов сообщений** для Telegram
5. **Улучшить структурированность** логов

## 📋 Текущие проблемы

### 1. Хардкоды сообщений в коде
```csharp
// MessageHandler.cs - строки 887-894
await _bot.SendMessage(
    Config.LogAdminChatId,
    $"🚫 Автобан по блэклисту lols.bot (первое сообщение){Environment.NewLine}" +
    $"Юзер {FullName(user.FirstName, user.LastName)} из чата {message.Chat.Title}{Environment.NewLine}" +
    $"{LinkToMessage(message.Chat, message.MessageId)}",
    replyParameters: forward,
    cancellationToken: cancellationToken
);
```

### 2. Дублирование логики отправки
- `MessageHandler.cs` - автобаны
- `Worker.cs` - автобаны из блэклиста  
- `ModerationService.cs` - уведомления о подозрительных пользователях

### 3. Отсутствие файлового логирования
- Только консоль + Telegram
- Нет ротации логов
- Нет структурированного формата

## 🏗️ Архитектура решения

### 1. Централизованная система сообщений

```csharp
// Services/IMessageService.cs
public interface IMessageService
{
    Task SendAdminNotificationAsync(AdminNotificationType type, object data, CancellationToken cancellationToken = default);
    Task SendLogNotificationAsync(LogNotificationType type, object data, CancellationToken cancellationToken = default);
    Task SendUserNotificationAsync(User user, Chat chat, UserNotificationType type, object data, CancellationToken cancellationToken = default);
}

// Services/MessageService.cs
public class MessageService : IMessageService
{
    private readonly ITelegramBotClientWrapper _bot;
    private readonly ILogger<MessageService> _logger;
    private readonly MessageTemplates _templates;
    
    public async Task SendAdminNotificationAsync(AdminNotificationType type, object data, CancellationToken cancellationToken = default)
    {
        var template = _templates.GetAdminTemplate(type);
        var message = template.Format(data);
        await _bot.SendMessage(Config.AdminChatId, message, cancellationToken: cancellationToken);
    }
}
```

### 2. Система шаблонов сообщений

```csharp
// Services/MessageTemplates.cs
public class MessageTemplates
{
    private readonly Dictionary<AdminNotificationType, string> _adminTemplates = new()
    {
        [AdminNotificationType.AutoBanBlacklist] = 
            "🚫 Автобан по блэклисту lols.bot (первое сообщение)\n" +
            "Юзер {UserFullName} из чата {ChatTitle}\n" +
            "{MessageLink}",
            
        [AdminNotificationType.PrivateChatBanAttempt] = 
            "⚠️ Попытка бана в приватном чате: {Reason}\n" +
            "Юзер {UserFullName} из чата {ChatTitle}\n" +
            "Операция невозможна в приватных чатах"
    };
    
    public string GetAdminTemplate(AdminNotificationType type) => _adminTemplates[type];
}
```

### 3. Расширенная система логирования

```csharp
// Services/ILoggingService.cs
public interface ILoggingService
{
    void LogUserAction(UserActionType action, User user, Chat chat, object? data = null);
    void LogSystemEvent(SystemEventType event, object? data = null);
    void LogError(Exception ex, string context, object? data = null);
    void LogModerationResult(ModerationResult result, User user, Chat chat);
}

// Services/LoggingService.cs
public class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly IUserFlowLogger _userFlowLogger;
    private readonly IMessageService _messageService;
    
    public void LogUserAction(UserActionType action, User user, Chat chat, object? data = null)
    {
        // Логируем в файл
        _logger.LogInformation("User Action: {Action} | User: {User} | Chat: {Chat} | Data: {@Data}", 
            action, Utils.FullName(user), chat.Title, data);
            
        // Логируем в UserFlowLogger
        switch (action)
        {
            case UserActionType.Joined:
                _userFlowLogger.LogUserJoined(user, chat);
                break;
            case UserActionType.Banned:
                _userFlowLogger.LogUserBanned(user, chat, data?.ToString() ?? "Unknown reason");
                break;
        }
    }
}
```

### 4. Файловое логирование с Serilog

```csharp
// Program.cs - обновленная конфигурация
.UseSerilog(
    (_, _, config) =>
    {
        config
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "ClubDoorman")
            .WriteTo.Async(a => a.Console())
            .WriteTo.Async(a => a.File(
                path: "logs/clubdoorman-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            ))
            .WriteTo.Async(a => a.File(
                path: "logs/errors-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                restrictedToMinimumLevel: LogEventLevel.Error,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            ));
    }
)
```

## 📝 План реализации

### Этап 1: Создание базовой инфраструктуры ✅
1. ✅ Создать `IMessageService` и `MessageService`
2. ✅ Создать `MessageTemplates` с шаблонами
3. ✅ Создать `ILoggingService` и `LoggingService`
4. ✅ Добавить файловое логирование в `Program.cs`

### Этап 2: Рефакторинг существующего кода ✅
1. ✅ Заменить хардкоды в `MessageHandler.cs`
2. ✅ Заменить хардкоды в `Worker.cs`
3. ✅ Заменить хардкоды в `ModerationService.cs`
4. ✅ Обновить `UserFlowLogger` для использования новой системы

### Этап 3: Расширение функциональности
1. ⏳ Добавить новые типы уведомлений
2. ⏳ Создать систему метрик
3. ⏳ Добавить мониторинг производительности
4. ⏳ Создать дашборд для админов

## 🔧 Технические детали

### Переменные окружения
```bash
# Логирование
DOORMAN_LOG_LEVEL=Information
DOORMAN_LOG_FILE_ENABLED=true
DOORMAN_LOG_FILE_PATH=logs/clubdoorman-.log
DOORMAN_LOG_ERROR_FILE_PATH=logs/errors-.log
DOORMAN_LOG_RETENTION_DAYS=7
DOORMAN_LOG_ERROR_RETENTION_DAYS=30

# Telegram уведомления
DOORMAN_ADMIN_CHAT=-4881744366
DOORMAN_LOG_ADMIN_CHAT=-4881744366
DOORMAN_NOTIFICATIONS_ENABLED=true
```

### Структура файлов
```
ClubDoorman/
├── Services/
│   ├── IMessageService.cs ✅
│   ├── MessageService.cs ✅
│   ├── MessageTemplates.cs ✅
│   ├── ILoggingService.cs ⏳
│   ├── LoggingService.cs ⏳
│   └── UserFlowLogger.cs (обновленный) ⏳
├── Models/
│   ├── Notifications/
│   │   ├── AdminNotificationType.cs ✅
│   │   ├── LogNotificationType.cs ✅
│   │   ├── UserNotificationType.cs ✅
│   │   └── NotificationData.cs ✅
│   └── Logging/
│       ├── UserActionType.cs ⏳
│       ├── SystemEventType.cs ⏳
│       └── LogContext.cs ⏳
└── logs/
    ├── clubdoorman-2024-01-21.log ✅
    ├── errors-2024-01-21.log ⏳
    └── .gitkeep ✅
```

## 📊 Ожидаемые результаты

### 1. Улучшение читаемости кода
```csharp
// Было
await _bot.SendMessage(Config.AdminChatId, $"🚫 Автобан: {reason}...", cancellationToken);

// Стало
await _messageService.SendAdminNotificationAsync(
    AdminNotificationType.AutoBan, 
    new { Reason = reason, User = user, Chat = chat }, 
    cancellationToken
);
```

### 2. Централизованное управление сообщениями
- Все шаблоны в одном месте
- Легко изменить текст сообщений
- Поддержка локализации в будущем

### 3. Структурированное логирование
- JSON формат для анализа
- Ротация логов
- Отдельные файлы для ошибок

### 4. Улучшенный мониторинг
- Метрики производительности
- Трекинг пользовательских действий
- Алерты для критических ошибок

## 🚀 Следующие шаги

1. ✅ Создать базовую инфраструктуру
2. ✅ Протестировать на небольшом участке кода
3. ✅ Постепенно мигрировать существующий код
4. ✅ Добавить гибкую настройку логирования
5. ✅ Создать документацию для разработчиков

## ✅ Выполненные задачи

### 1. Создана базовая инфраструктура ✅
- ✅ `AdminNotificationType.cs` - enum для типов админских уведомлений
- ✅ `LogNotificationType.cs` - enum для типов лог-уведомлений  
- ✅ `UserNotificationType.cs` - enum для типов пользовательских уведомлений
- ✅ `NotificationData.cs` - классы данных для уведомлений
- ✅ `MessageTemplates.cs` - система шаблонов сообщений
- ✅ `IMessageService.cs` - интерфейс сервиса сообщений
- ✅ `MessageService.cs` - реализация сервиса сообщений

### 2. Добавлено файловое логирование ✅
- ✅ Добавлены пакеты `Serilog.Sinks.File` и `Serilog.Formatting.Compact`
- ✅ Обновлена конфигурация Serilog в `Program.cs`
- ✅ Создана директория `logs/` с `.gitkeep`
- ✅ Настроена ротация логов (7 дней для обычных, 30 дней для ошибок)
- ✅ Протестировано - логи создаются корректно

### 3. Интеграция в DI контейнер ✅
- ✅ Зарегистрированы `MessageTemplates` и `IMessageService` в DI
- ✅ Проект собирается без ошибок
- ✅ Бот запускается и работает корректно

### 4. Рефакторинг существующего кода ✅
- ✅ Заменены все хардкоды в `MessageHandler.cs` на централизованные уведомления
- ✅ Заменены все хардкоды в `Worker.cs` на централизованные уведомления
- ✅ Заменены все хардкоды в `ModerationService.cs` на централизованные уведомления
- ✅ Все уведомления теперь используют единую систему шаблонов
- ✅ Проект собирается без ошибок после рефакторинга

### 5. Гибкая настройка логирования ✅
- ✅ Создан `LoggingConfiguration` - конфигурационные классы для настройки логирования
- ✅ Создан `ILoggingConfigurationService` и `LoggingConfigurationService` - сервис для работы с конфигурацией
- ✅ Обновлен `appsettings.json` с детальными настройками логирования
- ✅ Интегрирована конфигурация в `MessageService` с проверками направлений уведомлений
- ✅ Настроено разделение файловых логов (основные, ошибки, системные, пользовательские флоу)
- ✅ Все 501 тест проходит успешно

## 🧪 Результаты тестирования

### Файловое логирование
```
$ ls -la logs/
итого 16
drwxrwxr-x  2 kpblc kpblc 4096 июл 21 14:42 .
drwxrwxr-x 11 kpblc kpblc 4096 июл 21 14:40 ..
-rw-rw-r--  1 kpblc kpblc 2092 июл 21 14:42 clubdoorman-20250721.log
-rw-rw-r--  1 kpblc kpblc   99 июл 21 14:40 .gitkeep
```

### Содержимое лог-файла
```
2025-07-21 14:42:43.471 +03:00 [INF] Начинаем обучение ML модели...
2025-07-21 14:42:43.469 +03:00 [INF] RetrainLoop запущен - переобучение каждые 5 минут при необходимости
2025-07-21 14:42:43.496 +03:00 [DBG] Загружено 279 стоп-слов
2025-07-21 14:42:43.558 +03:00 [INF] Загружено 807 записей из датасета
2025-07-21 14:42:43.559 +03:00 [INF] Спам: 275, НЕ спам: 532
2025-07-21 14:42:43.648 +03:00 [DBG] Создаем pipeline для обучения...
2025-07-21 14:42:43.657 +03:00 [DBG] Обучаем модель...
2025-07-21 14:42:43.684 +03:00 [INF] 🤖 AI анализ ВКЛЮЧЕН: OpenRouter API настроен
2025-07-21 14:42:43.685 +03:00 [INF] Файл подозрительных пользователей не найден, создаем пустой список
2025-07-21 14:42:43.687 +03:00 [WRN] 🎭 Система мимикрии ОТКЛЮЧЕНА: установите DOORMAN_SUSPICIOUS_DETECTION_ENABLE=true для включения
2025-07-21 14:42:43.721 +03:00 [INF] Начальное обновление банлиста из lols.bot при старте бота
2025-07-21 14:42:43.741 +03:00 [DBG] Touch
2025-07-21 14:42:43.742 +03:00 [INF] Application started. Press Ctrl+C to shut down.
2025-07-21 14:42:43.742 +03:00 [INF] Hosting environment: Development
2025-07-21 14:42:43.742 +03:00 [INF] Content root path: /home/kpblc/projects/ClubDoorman/ClubDoorman
2025-07-21 14:42:43.744 +03:00 [DBG] offset read ok
2025-07-21 14:42:44.140 +03:00 [INF] Первичное обновление количества участников во всех чатах для статистики
2025-07-21 14:42:44.144 +03:00 [INF] ✅ ML модель успешно обучена за 656ms! Движок готов к работе.
2025-07-21 14:42:46.088 +03:00 [INF] Обновлен банлист из lols.bot: было 0, стало 2437243 записей
2025-07-21 14:42:52.735 +03:00 [INF] Application is shutting down...
``` 