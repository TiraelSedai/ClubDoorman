# Стратегия миграции: от Legacy к эффективной реализации

## 🎯 Цель: Одна эффективная реализация БЕЗ legacy

### ❌ Что НЕ планируем:
- Оставлять старые форматы callback data навсегда
- Поддерживать дублирующую логику
- Сохранять разбросанное создание кнопок
- Оставлять монолитный CallbackQueryHandler

### ✅ Что планируем:
- **Полная миграция** на новую систему
- **Единая точка создания кнопок** - ButtonFactory
- **Типизированный парсинг** - ParsedCallbackData
- **Разделенные обработчики** - ICallbackActionHandler
- **Чистая архитектура** без legacy

## 📋 Поэтапный план избавления от legacy

### Этап 1: Критическое исправление (ПРИОРИТЕТ #1)
**Цель:** Исправить неработающие кнопки
**Legacy:** Оставляем как есть, только исправляем баги
**Время:** 1-2 дня

### Этап 2: Создание новой инфраструктуры
**Цель:** Создать новую систему параллельно со старой
**Legacy:** Продолжает работать
**Новое:** 
- `Callback/` поддиректория
- `ParsedCallbackData` с `TryParse()`
- `ButtonFactory`
- `NotificationFormatters`
**Время:** 3-5 дней

### Этап 3: Постепенная миграция создания кнопок
**Цель:** Заменить создание кнопок на ButtonFactory
**Legacy:** Постепенно заменяется
**Новое:** Все новые кнопки через ButtonFactory
**Время:** 2-3 дня

### Этап 4: Миграция обработки callback
**Цель:** Заменить парсинг в CallbackQueryHandler
**Legacy:** Старый парсинг заменяется на ParsedCallbackData
**Новое:** Типизированная обработка
**Время:** 2-3 дня

### Этап 5: Разделение обработчиков (опционально)
**Цель:** Разбить CallbackQueryHandler на отдельные обработчики
**Legacy:** CallbackQueryHandler упрощается или удаляется
**Новое:** ICallbackActionHandler implementations
**Время:** 3-5 дней

### Этап 6: Очистка legacy кода
**Цель:** Удалить все старые форматы и методы
**Legacy:** Полностью удаляется
**Новое:** Только новая система
**Время:** 1-2 дня

## 🔄 Детальная стратегия миграции

### Шаг 1: Параллельная работа (Этапы 2-4)
```csharp
// Старый код продолжает работать
private async Task HandleAdminCallback(CallbackQuery callbackQuery, CancellationToken cancellationToken)
{
    var cbData = callbackQuery.Data!;
    var split = cbData.Split('_').ToList();
    
    // Старый парсинг
    if (split.Count > 2 && split[0] == "ban" && long.TryParse(split[1], out var chatId))
    {
        await HandleBanUser(callbackQuery, chatId, userId, cancellationToken);
    }
    
    // НОВОЕ: Пробуем новый парсинг
    if (ParsedCallbackData.TryParse(cbData, out var parsedData, out var error))
    {
        await HandleParsedCallback(parsedData, callbackQuery, cancellationToken);
    }
    else
    {
        // Fallback на старый парсинг
        _logger.LogWarning("Не удалось распарсить callback data: {Error}, используем старый парсинг", error);
    }
}
```

### Шаг 2: Постепенная замена создания кнопок
```csharp
// Старый код в MessageHandler.cs
var keyboard = new InlineKeyboardMarkup(new[]
{
    new[]
    {
        new InlineKeyboardButton("🤖 бан") { CallbackData = callbackDataBan },
        new InlineKeyboardButton("😶 пропуск") { CallbackData = "noop" },
        new InlineKeyboardButton("🥰 свой") { CallbackData = $"approve_{user.Id}" }
    }
});

// НОВЫЙ код
var keyboard = _buttonFactory.CreateAdminActionButtons(user.Id, message.Chat.Id, BanContext.Default);
```

### Шаг 3: Миграция форматов callback data
```csharp
// Старые форматы (постепенно заменяются)
"ban_123_456"
"approve_123"
"suspicious_ban_123_456_789"

// Новые форматы (типизированные)
"ban_123_456_default"
"approve_123"
"suspicious_ban_123_456_789"
```

### Шаг 4: Удаление legacy кода
```csharp
// УДАЛЯЕМ старый парсинг
private async Task HandleAdminCallback(CallbackQuery callbackQuery, CancellationToken cancellationToken)
{
    var cbData = callbackQuery.Data!;
    
    // ТОЛЬКО новый парсинг
    if (ParsedCallbackData.TryParse(cbData, out var parsedData, out var error))
    {
        await HandleParsedCallback(parsedData, callbackQuery, cancellationToken);
    }
    else
    {
        _logger.LogError("Неизвестный формат callback data: {Data}, ошибка: {Error}", cbData, error);
        await _bot.AnswerCallbackQuery(callbackQuery.Id, "Ошибка обработки", cancellationToken: cancellationToken);
    }
}
```

## 🧪 Стратегия тестирования миграции

### 1. Тесты совместимости
```csharp
[Test]
public async Task LegacyCallbackData_ShouldStillWork()
{
    // Arrange
    var legacyCallbackData = "ban_123_456";
    var callbackQuery = CreateCallbackQuery(legacyCallbackData);
    
    // Act
    await _handler.HandleAsync(callbackQuery);
    
    // Assert
    // Проверяем, что действие выполнилось
}

[Test]
public async Task NewCallbackData_ShouldWork()
{
    // Arrange
    var newCallbackData = "ban_123_456_default";
    var callbackQuery = CreateCallbackQuery(newCallbackData);
    
    // Act
    await _handler.HandleAsync(callbackQuery);
    
    // Assert
    // Проверяем, что действие выполнилось
}
```

### 2. Snapshot-тесты для кнопок
```csharp
[Test]
public void ButtonFactory_CreateAdminActionButtons_ShouldMatchSnapshot()
{
    // Arrange
    var userId = 123L;
    var chatId = 456L;
    var context = BanContext.Default;
    
    // Act
    var keyboard = _buttonFactory.CreateAdminActionButtons(userId, chatId, context);
    
    // Assert
    Assert.That(keyboard.InlineKeyboard, Is.EqualTo(ExpectedKeyboardSnapshot));
}
```

### 3. Интеграционные тесты
```csharp
[Test]
public async Task EndToEnd_ButtonCreationAndHandling_ShouldWork()
{
    // Arrange
    var user = CreateTestUser();
    var chat = CreateTestChat();
    
    // Act - создаем кнопку
    var keyboard = _buttonFactory.CreateAdminActionButtons(user.Id, chat.Id, BanContext.Default);
    var callbackData = keyboard.InlineKeyboard[0][0].CallbackData;
    
    // Act - обрабатываем callback
    var callbackQuery = CreateCallbackQuery(callbackData);
    await _handler.HandleAsync(callbackQuery);
    
    // Assert
    // Проверяем, что действие выполнилось корректно
}
```

## 📊 Метрики успешной миграции

### Количественные метрики:
- [ ] **100% кнопок** создаются через ButtonFactory
- [ ] **100% callback data** парсится через ParsedCallbackData
- [ ] **0 legacy форматов** остаются в коде
- [ ] **515 тестов** проходят успешно
- [ ] **0 регрессий** в функциональности

### Качественные метрики:
- [ ] **Читаемость кода** улучшилась
- [ ] **Тестируемость** повысилась
- [ ] **Расширяемость** стала проще
- [ ] **Поддержка** упростилась

## 🚨 Риски и митигация

### Риск 1: Регрессии при миграции
**Митигация:** 
- Поэтапная миграция
- Полное покрытие тестами
- Возможность отката на каждом этапе

### Риск 2: Неработающие кнопки после миграции
**Митигация:**
- Параллельная работа старой и новой системы
- Fallback на старый парсинг
- Подробное логирование

### Риск 3: Сложность понимания для команды
**Митигация:**
- Документация с примерами
- Постепенное внедрение
- Обучение команды

## 📅 Временные рамки

### Общее время: 12-20 дней
- **Этап 1:** 1-2 дня (критическое исправление)
- **Этап 2:** 3-5 дней (новая инфраструктура)
- **Этап 3:** 2-3 дня (миграция создания кнопок)
- **Этап 4:** 2-3 дня (миграция обработки)
- **Этап 5:** 3-5 дней (разделение обработчиков)
- **Этап 6:** 1-2 дня (очистка legacy)

## 🎯 Финальное состояние

### После миграции у нас будет:
```
ClubDoorman/
├── Callback/
│   ├── Models/
│   │   ├── ParsedCallbackData.cs ✅
│   │   ├── CallbackActionType.cs ✅
│   │   └── BanContext.cs ✅
│   ├── Interfaces/
│   │   └── ICallbackActionHandler.cs ✅
│   └── Handlers/
│       ├── BanActionHandler.cs ✅
│       ├── ApproveActionHandler.cs ✅
│       └── SuspiciousActionHandler.cs ✅
├── Services/
│   ├── ButtonFactory/
│   │   ├── IButtonFactory.cs ✅
│   │   └── ButtonFactory.cs ✅
│   ├── NotificationFormatters/
│   │   ├── INotificationFormatter.cs ✅
│   │   └── ... ✅
│   └── ServiceChatDispatcher.cs ✅ (упрощенный)
└── docs/
    └── buttons.md ✅
```

### ❌ Что будет удалено:
- Старые форматы callback data
- Ручной парсинг в CallbackQueryHandler
- Разбросанное создание кнопок
- Монолитная логика обработки

## 💡 Вывод

**Да, мы НЕ планируем оставлять legacy!** 

Стратегия: **поэтапная миграция с полной очисткой** в конце.

**Результат:** Одна эффективная, типизированная, тестируемая система без legacy кода. 