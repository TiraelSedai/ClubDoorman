# Test Infrastructure Documentation

## Обзор

Этот документ описывает инфраструктуру тестирования проекта ClubDoorman, включая инструменты, паттерны и лучшие практики.

## 🚀 Новые возможности (Этап 3)

### TestKitTelegram - Улучшенная работа с Telegram объектами

`TestKitTelegram` решает вечную проблему с `MessageId` в Telegram.Bot API и упрощает создание тестовых сценариев.

#### Основные возможности:

```csharp
// Создание MessageEnvelope с автоматическим MessageId
var envelope = TestKit.CreateEnvelope(userId: 12345, chatId: 67890, text: "Test message");

// Создание полного сценария с FakeTelegramClient
var (fakeClient, envelope, message, update) = TestKit.CreateFullScenario(
    userId: 12345, 
    chatId: 67890, 
    text: "Test message"
);

// Создание спам-сценария
var (fakeClient, envelope, message, update) = TestKit.CreateSpamScenario(userId: 12345, chatId: 67890);

// Создание сценария нового участника
var (fakeClient, envelope, message, update) = TestKit.CreateNewUserScenario(userId: 12345, chatId: 67890);
```

#### Решение проблемы MessageId:

```csharp
// MessageEnvelope содержит правильный MessageId
var envelope = TestKit.CreateEnvelope(userId: 12345, chatId: 67890, text: "Test");
Assert.That(envelope.MessageId, Is.GreaterThan(0)); // ✅ Работает

// FakeTelegramClient правильно отслеживает сообщения
var (fakeClient, envelope, message, update) = TestKit.CreateFullScenario();
Assert.That(fakeClient.WasMessageDeleted(envelope), Is.False); // ✅ Работает
```

### Обновленные категории тестов

Категории основаны на существующих в проекте и оптимизированы для CI/CD:

```csharp
// Существующие категории
[Category(TestCategories.Fast)]        // Быстрые тесты (< 1 сек)
[Category(TestCategories.Critical)]    // Критически важные
[Category(TestCategories.Unit)]        // Unit тесты
[Category(TestCategories.Integration)] // Интеграционные тесты
[Category(TestCategories.E2E)]         // End-to-end тесты
[Category(TestCategories.BDD)]         // BDD тесты с SpecFlow
[Category(TestCategories.RealApi)]     // Тесты с реальными API

// Доменные категории
[Category(TestCategories.Handlers)]    // Тесты обработчиков
[Category(TestCategories.Services)]    // Тесты сервисов
[Category(TestCategories.Moderation)]  // Тесты модерации
[Category(TestCategories.Ai)]          // Тесты AI компонентов
[Category(TestCategories.Captcha)]     // Тесты капчи
[Category(TestCategories.Ban)]         // Тесты бана пользователей

// Новые категории для оптимизации
[Category(TestCategories.Slow)]        // Медленные тесты (1-5 сек)
[Category(TestCategories.Flaky)]       // Нестабильные тесты
[Category(TestCategories.AutoFixture)] // AutoFixture демо-тесты
```

## 📦 Основные компоненты

### 1. GlobalUsings.cs

Централизует общие `using` директивы:

```csharp
global using Moq;
global using FluentAssertions;
global using System.Threading;
global using System.Threading.Tasks;
global using Telegram.Bot.Types;
// ... и другие
```

### 2. TestKit.cs

Единая точка входа для создания тестовых объектов:

```csharp
// Создание фабрик
var factory = TestKit.CreateMessageHandlerFactory();
var handler = TestKit.CreateMessageHandlerWithFake();

// AutoFixture
var service = TestKit.Create<IModerationService>();
var services = TestKit.CreateMany<IModerationService>(5);

// Bogus данные
var user = TestKit.CreateUser(userId: 12345);
var message = TestKit.CreateMessage();

// Telegram интеграция
var (fakeClient, envelope, message, update) = TestKit.CreateFullScenario();
```

### 3. TestKit.Bogus.cs

Генерация реалистичных тестовых данных:

```csharp
var user = TestKitBogus.CreateRealisticUser(userId: 12345);
var message = TestKitBogus.CreateRealisticMessage();
var group = TestKitBogus.CreateRealisticGroup();
var spamMessage = TestKitBogus.CreateRealisticSpamMessage();
```

### 4. TestKit.AutoFixture.cs

Автоматическое создание объектов и моков:

```csharp
// Простое создание
var service = TestKitAutoFixture.Create<IModerationService>();

// Создание с настройкой
var (sut, fixture) = TestKitAutoFixture.CreateWithFixture<MessageHandler>();
var mock = fixture.Freeze<Mock<ITelegramBotClientWrapper>>();
mock.Setup(x => x.SendMessageAsync(...)).ReturnsAsync(...);
```

### 5. TestKit.Builders.cs

Fluent API для создания тестовых данных:

```csharp
var message = TestKitBuilders.CreateMessage()
    .WithText("Hello, world!")
    .FromUser(12345)
    .InChat(67890)
    .Build();

var user = TestKitBuilders.CreateUser()
    .WithId(12345)
    .WithUsername("testuser")
    .AsRegularUser()
    .Build();

var result = TestKitBuilders.CreateModerationResult()
    .WithAction(ModerationAction.Delete)
    .WithReason("Spam detected")
    .Build();
```

## 🎯 Паттерны использования

### 1. Простой тест с AutoFixture

```csharp
[Test, Category(TestCategories.Fast)]
public void ModerationService_ValidMessage_ReturnsAllow()
{
    // Arrange
    var service = TestKit.Create<IModerationService>();
    var message = TestKit.CreateMessage();

    // Act
    var result = service.CheckMessageAsync(message).Result;

    // Assert
    Assert.That(result.Action, Is.EqualTo(ModerationAction.Allow));
}
```

### 2. Тест с FakeTelegramClient

```csharp
[Test, Category(TestCategories.Integration)]
public async Task MessageHandler_SpamMessage_DeletesMessage()
{
    // Arrange
    var (fakeClient, envelope, message, update) = TestKit.CreateSpamScenario();
    var handler = TestKit.CreateMessageHandlerWithFake(fakeClient);

    // Act
    await handler.HandleAsync(update);

    // Assert
    Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True);
}
```

### 3. Тест с Builders

```csharp
[Test, Category(TestCategories.Unit)]
public void ModerationService_SpamMessage_ReturnsDelete()
{
    // Arrange
    var service = TestKit.Create<IModerationService>();
    var message = TestKitBuilders.CreateMessage()
        .AsSpam()
        .FromUser(12345)
        .Build();

    // Act
    var result = service.CheckMessageAsync(message).Result;

    // Assert
    Assert.That(result.Action, Is.EqualTo(ModerationAction.Delete));
}
```

### 4. Тест с существующей фабрикой

```csharp
[Test, Category(TestCategories.Unit)]
public void MessageHandler_WithFactory_CreatesCorrectly()
{
    // Arrange
    var factory = TestKit.CreateMessageHandlerFactory();
    factory.WithModerationServiceSetup(mock =>
    {
        mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
            .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid"));
    });

    // Act
    var handler = factory.CreateMessageHandler();

    // Assert
    Assert.That(handler, Is.Not.Null);
}
```

## 🔧 Настройка и конфигурация

### Добавление новых категорий

```csharp
// В TestCategories.cs
public static class TestCategories
{
    public const string NewCategory = "new-category";
    // ...
}
```

### Расширение TestKit

```csharp
// В TestKit.cs
public static class TestKit
{
    public static NewService CreateNewService() => new NewService();
    // ...
}
```

### Создание новых Builders

```csharp
// В TestKit.Builders.cs
public class NewObjectBuilder
{
    private NewObject _obj = new();
    
    public NewObjectBuilder WithProperty(string value)
    {
        _obj.Property = value;
        return this;
    }
    
    public NewObject Build() => _obj;
}
```

## 📊 Метрики и производительность

### Время выполнения тестов

- **Fast**: < 1 секунды
- **Slow**: 1-5 секунд
- **Integration**: 5-30 секунд
- **E2E**: 30+ секунд

### Покрытие кода

```bash
# Запуск тестов с покрытием
dotnet test --collect:"XPlat Code Coverage"

# Генерация отчета
reportgenerator -reports:TestResults/*/coverage.cobertura.xml -targetdir:TestResults/Coverage
```

## 🚨 Известные ограничения

### MessageId в Telegram.Bot

```csharp
// ❌ Не работает - MessageId readonly
message.MessageId = 123;

// ✅ Работает - используем MessageEnvelope
var envelope = TestKit.CreateEnvelope(messageId: 123);
var message = fakeClient.CreateMessageFromEnvelope(envelope);
```

### AutoFixture с readonly свойствами

```csharp
// ❌ Не работает с readonly свойствами
fixture.Customize<Message>(c => c.With(m => m.MessageId, 123));

// ✅ Работает - используем TestDataFactory
fixture.Customize<Message>(c => c.FromFactory(() => TestDataFactory.CreateValidMessageWithId(123)));
```

## 📚 Дополнительные ресурсы

- [LESSONS_LEARNED.md](LESSONS_LEARNED.md) - Извлеченные уроки
- [TESTING_ARCHITECTURE.md](TESTING_ARCHITECTURE.md) - Архитектура тестирования
- [TIMEOUTS.md](TIMEOUTS.md) - Настройка таймаутов

## 🤝 Миграция существующих тестов

### Пошаговый план

1. **Добавить категории** к существующим тестам
2. **Заменить создание моков** на `TestKit.Create<T>()`
3. **Использовать Builders** вместо ручного создания объектов
4. **Интегрировать FakeTelegramClient** для Telegram-тестов
5. **Добавить MessageEnvelope** для тестов с MessageId

### Пример миграции

```csharp
// До
[Test]
public void OldTest()
{
    var mock = new Mock<IService>();
    var message = new Message { Text = "test" };
    // ...
}

// После
[Test, Category(TestCategories.Fast)]
public void NewTest()
{
    var service = TestKit.Create<IService>();
    var message = TestKitBuilders.CreateMessage()
        .WithText("test")
        .Build();
    // ...
}
```

---

**Примечание**: Эта инфраструктура постоянно развивается. Следите за обновлениями в планах и worklog файлах. 