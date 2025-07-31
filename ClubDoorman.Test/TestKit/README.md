# TestKit - Тестовая инфраструктура ClubDoorman

## 📚 Документация
- **[INDEX.md](INDEX.md)** - Тегированный индекс всех генераторов и моков
- **[TestCategories.cs](TestCategories.cs)** - Категории тестов для CI/CD

## 📦 Компоненты TestKit

### Core
- **TestKit.cs** - Основной класс с единым интерфейсом (674 строки)
- **TestCategories.cs** - Категории тестов для оптимизации CI/CD

### Data Generation
- **TestKit.Bogus.cs** - Генерация реалистичных тестовых данных с помощью Bogus
- **TestKit.Builders.cs** - Fluent API для создания тестовых объектов (Test Data Builders pattern)

### Automation
- **TestKit.AutoFixture.cs** - Автоматическое создание объектов и моков с помощью AutoFixture
- **TestKit.Telegram.cs** - Улучшенная работа с Telegram объектами и решение проблемы MessageId
- **TestKit.Mocks.cs** - Централизованные моки для всех сервисов и компонентов

## Быстрый старт

```csharp
// Базовые объекты
var user = TK.CreateValidUser();
var chat = TK.CreateGroupChat();
var message = TK.CreateValidMessage();

// Специализированные сценарии
var captcha = TK.Specialized.Captcha.Bait();
var result = TK.Specialized.Moderation.Ban();
var callback = TK.Specialized.Admin.ApproveCallback();
```

## Основные методы

### Пользователи и чаты
```csharp
var user = TK.CreateValidUser();
var botUser = TK.CreateBotUser();
var groupChat = TK.CreateGroupChat();
var channel = TK.CreateChannel();
```

### Сообщения
```csharp
var validMessage = TK.CreateValidMessage();
var spamMessage = TK.CreateSpamMessage();
var textMessage = TK.CreateTextMessage(userId, chatId, "Hello");
var channelMessage = TK.CreateChannelMessage(senderChatId, chatId, "Channel post");
```

### Специализированные генераторы

#### Капча
```csharp
var captcha = TK.Specialized.Captcha.Bait();
var validCaptcha = TK.Specialized.Captcha.Valid();
var expiredCaptcha = TK.Specialized.Captcha.Expired();
var correctResult = TK.Specialized.Captcha.CorrectResult();
```

#### Модерация
```csharp
var allowResult = TK.Specialized.Moderation.Allow();
var deleteResult = TK.Specialized.Moderation.Delete();
var banResult = TK.Specialized.Moderation.Ban();
```

#### Админские действия
```csharp
var approveCallback = TK.Specialized.Admin.ApproveCallback();
var banCallback = TK.Specialized.Admin.BanCallback();
var notification = TK.Specialized.Admin.Notification();
```

#### Обновления чата
```csharp
var memberJoined = TK.Specialized.Updates.MemberJoined();
var memberBanned = TK.Specialized.Updates.MemberBanned();
var memberLeft = TK.Specialized.Updates.MemberLeft();
```

## Продвинутые возможности

### AutoFixture - Автоматическое создание объектов
```csharp
// Создание с автоматическими зависимостями
var service = TK.Create<IModerationService>();
var handler = TK.Create<MessageHandler>();

// Создание коллекций
var users = TK.CreateMany<User>(5);

// Создание с фикстурой для кастомизации
var (sut, fixture) = TK.CreateWithFixture<MessageHandler>();
```

### Builders - Fluent API
```csharp
// Создание объектов через builder pattern
var message = TestKitBuilders.CreateMessage()
    .WithText("Hello, world!")
    .FromUser(12345)
    .InChat(67890)
    .Build();

var user = TestKitBuilders.CreateUser()
    .WithId(12345)
    .WithUsername("testuser")
    .IsBot(false)
    .Build();
```

### Bogus - Реалистичные данные
```csharp
// Создание реалистичных данных
var realisticUser = TK.CreateUser(userId: 12345);
var realisticMessage = TK.CreateMessage();
var realisticChannel = TK.CreateRealisticChannel();
```

### Telegram - Специализированные сценарии
```csharp
// Полные сценарии
var (fakeClient, envelope, message, update) = TK.CreateFullScenario();
var (fakeClient, envelope, message, update) = TK.CreateSpamScenario();
var (fakeClient, envelope, message, update) = TK.CreateNewUserScenario();

// Создание envelope
var envelope = TK.CreateEnvelope(message, update);
```

### Mocks - Централизованные моки
```csharp
// Базовые моки
var mockService = TK.CreateMock<IMyService>();
var loggerMock = TK.CreateLoggerMock<MyClass>();
var nullLogger = TK.CreateNullLogger<MyClass>();

// Telegram моки
var botClient = TK.CreateMockBotClient();
var botWrapper = TK.CreateMockBotClientWrapper();
var testMessage = TK.CreateTestMessage("Hello");
var testUser = TK.CreateTestUser("testuser");
var testChat = TK.CreateTestChat("Test Group");
var testUpdate = TK.CreateTestUpdate();

// Сервисные моки
var moderationService = TK.CreateMockModerationService();
var captchaService = TK.CreateMockCaptchaService();
var userManager = TK.CreateMockUserManager();
var userBanService = TK.CreateMockUserBanService();
var violationTracker = TK.CreateMockViolationTracker();
var messageService = TK.CreateMockMessageService();
var statisticsService = TK.CreateMockStatisticsService();
var botPermissionsService = TK.CreateMockBotPermissionsService();

// AI моки
var aiChecks = TK.CreateMockAiChecks();
var spamAiChecks = TK.CreateSpamAiChecks();
var normalAiChecks = TK.CreateNormalAiChecks();
var errorAiChecks = TK.CreateErrorAiChecks();

// Специализированные моки
var (moderation, captcha, userBan, message, aiChecks) = TK.CreateMessageHandlerMocks();
var (captcha, statistics, moderation, message) = TK.CreateCallbackQueryHandlerMocks();
var banModeration = TK.CreateBanModerationService("Spam detected");
var deleteModeration = TK.CreateDeleteModerationService("Inappropriate content");
var successfulCaptcha = TK.CreateSuccessfulCaptchaService();
var failedCaptcha = TK.CreateFailedCaptchaService();
var approvedUserManager = TK.CreateApprovedUserManager();
var unapprovedUserManager = TK.CreateUnapprovedUserManager();
var banTriggeringTracker = TK.CreateBanTriggeringViolationTracker();
```

## Интеграционные тесты

### MessageHandler
```csharp
var factory = TK.CreateMessageHandlerFactory();
var handler = factory.CreateWithDefaults();

// Настройка сценария
factory.SetupModerationBanScenario("Спам сообщение");
var result = await handler.HandleUserMessageAsync(message);
```

### ModerationService
```csharp
var factory = TK.CreateModerationServiceFactory();
var service = factory.CreateWithDefaults();

var result = await service.CheckMessageAsync(message, user, chat);
```

## Лучшие практики

1. **Используйте `TK.CreateX()`** для всех тестовых объектов
2. **Смотрите в `TK.Specialized.*`** для специализированных сценариев
3. **Пишите тесты парами**: happy path + fail path
4. **Используйте фабрики** для интеграционных тестов
5. **Если метода нет** - добавьте прокси в `TestKit.cs`
6. **Для сложных объектов** используйте Builders или AutoFixture
7. **Для реалистичных данных** используйте Bogus
8. **Для моков используйте `TK.CreateMock*()`** - единый интерфейс для всех моков
9. **Используйте специализированные моки** для типичных сценариев (баны, капча, etc.)
10. **Все моки возвращают `Mock<T>`** - используйте `.Object` для получения объекта

## Решение проблемы размера TestKit.cs

TestKit.cs уже содержит 674 строки. Для предотвращения "раздувания":

### Стратегия разделения:
1. **Основные методы** остаются в `TestKit.cs`
2. **Специализированные** группируются в `TestKit.Specialized.*`
3. **Сложная логика** выносится в отдельные файлы:
   - `TestKit.AutoFixture.cs` - автоматическое создание
   - `TestKit.Builders.cs` - fluent API
   - `TestKit.Bogus.cs` - реалистичные данные
   - `TestKit.Telegram.cs` - Telegram-специфика

### Принципы добавления новых методов:
1. **Простые прокси** → `TestKit.cs`
2. **Специализированные** → `TestKit.Specialized.*`
3. **Сложная логика** → отдельный файл
4. **Доменно-специфичные** → соответствующий файл

## Добавление новых методов

Если нужного метода нет в TestKit:

1. **Простые прокси** в `TestKit.cs`:
```csharp
public static NewType CreateNewObject() => TestDataFactory.CreateNewObject();
```

2. **Специализированные** в `TestKit.Specialized.*`:
```csharp
public static class Specialized
{
    public static class NewCategory
    {
        public static NewType NewMethod() => TestDataFactory.CreateNewObject();
    }
}
```

3. **Сложная логика** в отдельном файле:
```csharp
// TestKit.NewFeature.cs
public static class TestKitNewFeature
{
    public static NewType CreateComplexObject() => // сложная логика
}
``` 