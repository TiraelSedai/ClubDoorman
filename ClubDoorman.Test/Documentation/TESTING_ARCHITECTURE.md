# Архитектура тестирования в ClubDoorman

## 🏗️ Структура тестового проекта

### Организация файлов

```
ClubDoorman.Test/
├── TestInfrastructure/           # Централизованная инфраструктура
│   ├── MessageHandlerTestFactory.cs
│   ├── FakeTelegramClient.cs
│   ├── TestDataFactory.cs
│   └── FakeServicesFactory.cs
├── Services/                     # Тесты сервисов
│   ├── ModerationServiceSimpleTests.cs
│   └── ModerationServiceTests.cs
├── Handlers/                     # Тесты обработчиков
│   ├── MessageHandlerTests.cs
│   └── MessageHandlerExtendedTests.cs
├── Unit/                         # Модульные тесты
│   ├── Services/
│   ├── Handlers/
│   └── Infrastructure/
├── Integration/                  # Интеграционные тесты
│   ├── MessageHandlerIntegrationTests.cs
│   └── InfrastructureE2ETests.cs
└── LESSONS_LEARNED.md           # Документация
```

## 🔧 Ключевые компоненты

### 1. MessageHandlerTestFactory

**Назначение:** Централизованное создание MessageHandler с моками.

**Принципы:**
- Все моки создаются как свойства с инициализацией
- Методы для создания разных конфигураций
- Переиспользование между тестами

```csharp
public class MessageHandlerTestFactory
{
    // Моки как свойства с инициализацией
    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock { get; } = new();
    public Mock<IMessageService> MessageServiceMock { get; } = new();
    public Mock<IModerationService> ModerationServiceMock { get; } = new();
    // ... другие моки
    
    // Методы для создания разных конфигураций
    public MessageHandler CreateMessageHandlerWithFake(FakeTelegramClient fakeClient)
    public MessageHandler CreateMessageHandlerWithMocks()
    public MessageHandler CreateMessageHandlerForIntegration()
}
```

### 2. FakeTelegramClient

**Назначение:** Эмуляция Telegram Bot API для тестов.

**Ключевые возможности:**
- Регистрация MessageEnvelope
- Создание сообщений из envelope
- Отслеживание удаленных сообщений
- Эмуляция отправки сообщений

```csharp
public class FakeTelegramClient : ITelegramBotClientWrapper
{
    private readonly Dictionary<int, MessageEnvelope> _messageEnvelopes = new();
    private readonly List<DeletedMessage> _deletedMessages = new();
    private readonly List<SentMessage> _sentMessages = new();
    
    public void RegisterMessageEnvelope(MessageEnvelope envelope)
    public Message CreateMessageFromEnvelope(MessageEnvelope envelope)
    public bool WasMessageDeleted(MessageEnvelope envelope)
    public Task<bool> DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
}
```

### 3. MessageEnvelope

**Назначение:** Централизованное представление сообщения для тестов.

**Преимущества:**
- Обход ограничений Telegram.Bot API
- Единообразное создание тестовых данных
- Простое отслеживание состояния

```csharp
public record MessageEnvelope
{
    public int MessageId { get; init; }
    public long ChatId { get; init; }
    public long UserId { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

## 📋 Паттерны тестирования

### 1. Arrange-Act-Assert (AAA)

```csharp
[Test]
public async Task TestExample()
{
    // Arrange - подготовка
    var factory = new MessageHandlerTestFactory();
    var handler = factory.CreateMessageHandlerWithFake(fakeClient);
    var envelope = CreateTestEnvelope();
    
    // Act - выполнение
    await handler.HandleAsync(message);
    
    // Assert - проверка
    Assert.That(result, Is.True);
}
```

### 2. Builder Pattern для тестовых данных

```csharp
public class MessageEnvelopeBuilder
{
    private int _messageId = 1;
    private long _chatId = 123;
    private long _userId = 456;
    private string _text = "Test message";
    
    public MessageEnvelopeBuilder WithMessageId(int messageId)
    {
        _messageId = messageId;
        return this;
    }
    
    public MessageEnvelopeBuilder WithChatId(long chatId)
    {
        _chatId = chatId;
        return this;
    }
    
    public MessageEnvelope Build()
    {
        return new MessageEnvelope
        {
            MessageId = _messageId,
            ChatId = _chatId,
            UserId = _userId,
            Text = _text
        };
    }
}

// Использование
var envelope = new MessageEnvelopeBuilder()
    .WithMessageId(789)
    .WithChatId(456)
    .Build();
```

### 3. Test Data Factories

```csharp
public static class TestDataFactory
{
    public static MessageEnvelope CreateSpamMessage(long userId = 789, long chatId = 123)
    {
        return new MessageEnvelope
        {
            MessageId = 456,
            ChatId = chatId,
            UserId = userId,
            Text = "SPAM MESSAGE"
        };
    }
    
    public static MessageEnvelope CreateValidMessage(long userId = 789, long chatId = 123)
    {
        return new MessageEnvelope
        {
            MessageId = 456,
            ChatId = chatId,
            UserId = userId,
            Text = "Hello, world!"
        };
    }
}
```

## 🎯 Типы тестов

### 1. Unit Tests (Модульные)

**Цель:** Тестирование отдельных компонентов в изоляции.

**Характеристики:**
- Быстрые (миллисекунды)
- Изолированные
- Используют только моки
- Тестируют бизнес-логику

```csharp
[Test]
public async Task ModerationService_SpamMessage_ReturnsDelete()
{
    // Arrange
    var service = new ModerationService(mocks...);
    var message = TestDataFactory.CreateSpamMessage();
    
    // Act
    var result = await service.CheckMessageAsync(message);
    
    // Assert
    Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
}
```

### 2. Integration Tests (Интеграционные)

**Цель:** Тестирование взаимодействия между компонентами.

**Характеристики:**
- Средняя скорость (секунды)
- Используют FakeTelegramClient
- Тестируют реальные взаимодействия
- Проверяют полные сценарии

```csharp
[Test]
public async Task MessageHandler_SpamMessage_DeletesAndSendsWarning()
{
    // Arrange
    var factory = new MessageHandlerTestFactory();
    var handler = factory.CreateMessageHandlerWithFake(fakeClient);
    var envelope = TestDataFactory.CreateSpamMessage();
    
    // Act
    await handler.HandleAsync(message);
    
    // Assert
    Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True);
    Assert.That(fakeClient.SentMessages.Count, Is.EqualTo(1));
}
```

### 3. End-to-End Tests (E2E)

**Цель:** Тестирование полного пользовательского сценария.

**Характеристики:**
- Медленные (секунды-минуты)
- Используют реальные сервисы
- Тестируют полные потоки
- Проверяют пользовательский опыт

## 🔍 Отладка тестов

### 1. Отладочные логи

```csharp
// В сложных тестах добавлять логи
Console.WriteLine($"DEBUG: Processing message {envelope.MessageId}");
Console.WriteLine($"DEBUG: Moderation result: {result.Action}");
Console.WriteLine($"DEBUG: Message deleted: {fakeClient.WasMessageDeleted(envelope)}");
```

### 2. Проверка состояния моков

```csharp
// Проверка вызовов методов
MessageServiceMock.Verify(x => x.SendUserNotificationWithReplyAsync(
    It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<UserNotificationType>(),
    It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);

// Проверка параметров вызовов
MessageServiceMock.Verify(x => x.SendUserNotificationWithReplyAsync(
    It.Is<User>(u => u.Id == expectedUserId),
    It.Is<Chat>(c => c.Id == expectedChatId),
    It.Is<UserNotificationType>(t => t == expectedType),
    It.IsAny<object>(),
    It.IsAny<CancellationToken>()), Times.Once);
```

### 3. Проверка состояния FakeTelegramClient

```csharp
// Проверка отправленных сообщений
Assert.That(fakeClient.SentMessages.Count, Is.EqualTo(1));
Assert.That(fakeClient.SentMessages[0].Text, Contains.Substring("новичок"));

// Проверка удаленных сообщений
Assert.That(fakeClient.DeletedMessages.Count, Is.EqualTo(1));
Assert.That(fakeClient.DeletedMessages[0].MessageId, Is.EqualTo(envelope.MessageId));
```

## 🚀 Рекомендации по производительности

### 1. Переиспользование объектов

```csharp
// ✅ Хорошо: Переиспользование фабрики
public class TestBase
{
    protected MessageHandlerTestFactory Factory { get; } = new();
    
    [SetUp]
    public void Setup()
    {
        // Общая настройка
    }
}

// ❌ Плохо: Создание фабрики в каждом тесте
[Test]
public void Test1()
{
    var factory = new MessageHandlerTestFactory(); // Дублирование
}
```

### 2. Параллельное выполнение

```csharp
// Использовать атрибуты для контроля параллельности
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class MessageHandlerTests
{
    // Тесты могут выполняться параллельно
}
```

### 3. Очистка ресурсов

```csharp
[TestFixture]
public class TestBase : IDisposable
{
    public void Dispose()
    {
        // Очистка ресурсов
    }
}
```

## 📊 Метрики качества тестов

### 1. Покрытие кода

```bash
# Запуск с покрытием
dotnet test --collect:"XPlat Code Coverage"
```

### 2. Время выполнения

```bash
# Запуск с измерением времени
dotnet test --logger "console;verbosity=detailed"
```

### 3. Количество тестов

```bash
# Подсчет тестов
dotnet test --list-tests
```

## 🎯 Лучшие практики

### 1. Именование тестов

```csharp
// ✅ Хорошо: Описательные имена
[Test]
public async Task MessageHandler_SpamMessage_DeletesMessageAndSendsWarning()

[Test]
public async Task ModerationService_SpamText_ReturnsDeleteAction()

// ❌ Плохо: Неинформативные имена
[Test]
public async Task Test1()

[Test]
public async Task HandleMessage()
```

### 2. Организация тестов

```csharp
[TestFixture]
public class MessageHandlerTests
{
    [Test]
    public async Task SpamMessage_DeletesAndWarns()
    {
        // Тест удаления спама
    }
    
    [Test]
    public async Task ValidMessage_AllowsAndLogs()
    {
        // Тест валидного сообщения
    }
    
    [Test]
    public async Task NewUser_SendsWelcomeMessage()
    {
        // Тест нового пользователя
    }
}
```

### 3. Документирование тестов

```csharp
/// <summary>
/// Тестирует сценарий обработки спам-сообщения:
/// 1. Сообщение отправляется в чат
/// 2. Модерация определяет его как спам
/// 3. Сообщение удаляется
/// 4. Пользователю отправляется предупреждение
/// </summary>
[Test]
public async Task MessageHandler_SpamMessage_DeletesMessageAndSendsWarning()
{
    // Реализация теста
}
```

## 🔄 Непрерывная интеграция

### 1. Автоматические тесты

```yaml
# .github/workflows/tests.yml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
      - name: Run tests
        run: dotnet test --verbosity normal
```

### 2. Качество кода

```yaml
# Проверка покрытия
- name: Check coverage
  run: dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Анализ кода
- name: Run code analysis
  run: dotnet build --verbosity normal
```

## 📚 Дополнительные ресурсы

### Полезные ссылки:
- [NUnit Documentation](https://docs.nunit.org/)
- [Moq Documentation](https://github.com/moq/moq4/wiki/Quickstart)
- [Testing Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/)

### Инструменты:
- **NUnit** - фреймворк тестирования
- **Moq** - библиотека для создания моков
- **FluentAssertions** - улучшенные ассерты
- **Coverlet** - измерение покрытия кода 