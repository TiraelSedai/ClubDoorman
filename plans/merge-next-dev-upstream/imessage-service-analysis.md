# Анализ конфликта в IMessageService.cs

## Конфликт в методе SendWelcomeMessageAsync

### Ваша версия (HEAD):
```csharp
/// <summary>
/// Отправляет приветственное сообщение используя Request объект
/// </summary>
Task<Message> SendWelcomeMessageAsync(SendWelcomeMessageRequest request);
```

### Версия Мамая (upstream/next-dev):
```csharp
/// <summary>
/// Отправляет приветственное сообщение и автоматически удаляет его через 20 секунд
/// Возвращает null если приветствия отключены
/// </summary>
Task<Message?> SendWelcomeMessageAsync(User user, Chat chat, string reason = "приветствие", CancellationToken cancellationToken = default);
```

## Анализ различий

### Ваша архитектура:
- Использует Request Objects (SendWelcomeMessageRequest)
- Возвращает Message (не nullable)
- Более объектно-ориентированный подход

### Поведение Мамая:
- Прямые параметры (User, Chat, string)
- Возвращает Message? (nullable)
- Автоматическое удаление через 20 секунд
- Возвращает null если приветствия отключены

## Рекомендуемое решение

### Сохранить оба метода для обратной совместимости:

```csharp
/// <summary>
/// Отправляет приветственное сообщение используя Request объект
/// </summary>
Task<Message> SendWelcomeMessageAsync(SendWelcomeMessageRequest request);

/// <summary>
/// Отправляет приветственное сообщение и автоматически удаляет его через 20 секунд
/// Возвращает null если приветствия отключены
/// </summary>
Task<Message?> SendWelcomeMessageAsync(User user, Chat chat, string reason = "приветствие", CancellationToken cancellationToken = default);
```

## Преимущества решения:
1. ✅ Сохраняется ваша архитектура Request Objects
2. ✅ Принимается поведение Мамая (nullable return, auto-delete)
3. ✅ Обратная совместимость
4. ✅ Возможность постепенной миграции 