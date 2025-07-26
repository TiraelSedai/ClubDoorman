# Разделение сообщений по сервис-чатам

## Обзор

Реализована система разделения сообщений по сервис-чатам согласно [issue #22](https://github.com/momai/ClubDoorman/issues/22).

## Архитектура

### Компоненты

1. **IServiceChatDispatcher** - интерфейс диспетчера сервис-чатов
2. **ServiceChatDispatcher** - реализация диспетчера с логикой разделения
3. **MessageService** - обновлен для использования диспетчера

### Логика разделения

#### Админ-чат (требует реакции через кнопки)
- `SuspiciousMessageNotificationData` - подозрительные сообщения
- `SuspiciousUserNotificationData` - подозрительные пользователи
- `AiDetectNotificationData` (когда `IsAutoDelete = false`) - AI детект, требующий проверки
- `PrivateChatBanAttemptData` - попытки бана в приватном чате
- `ChannelMessageNotificationData` - сообщения от каналов
- `UserRestrictedNotificationData` - пользователи с ограничениями
- `UserRemovedFromApprovedNotificationData` - удаление из списка одобренных
- `ErrorNotificationData` - ошибки, требующие внимания

#### Лог-чат (для анализа и корректировки фильтров)
- `AutoBanNotificationData` - автобаны
- `AiDetectNotificationData` (когда `IsAutoDelete = true`) - AI автоудаление
- Все остальные типы уведомлений

## Конфигурация

### Переменные окружения
- `DOORMAN_ADMIN_CHAT` - ID админ-чата
- `DOORMAN_LOG_ADMIN_CHAT` - ID лог-чата (если не указан, используется админ-чат)

### Регистрация в DI
```csharp
services.AddSingleton<IServiceChatDispatcher, ServiceChatDispatcher>();
```

## Использование

### В MessageService
```csharp
// Автоматическое разделение по типам уведомлений
if (_serviceChatDispatcher.ShouldSendToAdminChat(data))
{
    await _serviceChatDispatcher.SendToAdminChatAsync(data, cancellationToken);
}
else
{
    await _serviceChatDispatcher.SendToLogChatAsync(data, cancellationToken);
}
```

### Прямое использование
```csharp
// Принудительная отправка в админ-чат
await _serviceChatDispatcher.SendToAdminChatAsync(notification, cancellationToken);

// Принудительная отправка в лог-чат
await _serviceChatDispatcher.SendToLogChatAsync(notification, cancellationToken);
```

## Форматирование сообщений

### Админ-чат
- HTML разметка
- Кнопки для взаимодействия
- Подробная информация для принятия решений

### Лог-чат
- HTML разметка
- Без кнопок
- Краткая информация для анализа

## Кнопки в админ-чате

### Подозрительные сообщения
- ✅ Одобрить
- ❌ Спам
- 🚫 Бан

### Подозрительные пользователи
- ✅ Одобрить
- 🚫 Бан

### AI детект
- ✅ OK
- ❌ Спам

## Тестирование

Созданы unit-тесты для проверки логики разделения:
- `ServiceChatDispatcherTests.cs` - 8 тестов
- Покрыты все основные сценарии
- Все тесты проходят успешно

## Миграция

### Что изменилось
1. Добавлен `IServiceChatDispatcher` в конструктор `MessageService`
2. Обновлены методы `SendAdminNotificationAsync` и `SendLogNotificationAsync`
3. Зарегистрирован диспетчер в DI контейнере

### Обратная совместимость
- Весь существующий функционал сохранен
- API `MessageService` не изменился
- Конфигурация осталась прежней

## Преимущества

1. **Автоматическое разделение** - не нужно вручную определять тип чата
2. **Централизованная логика** - все правила в одном месте
3. **Гибкость** - легко добавлять новые типы уведомлений
4. **Тестируемость** - покрыто unit-тестами
5. **Обратная совместимость** - не ломает существующий код 