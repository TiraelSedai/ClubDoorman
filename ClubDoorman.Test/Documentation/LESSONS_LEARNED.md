# Lessons Learned: Тестирование в ClubDoorman

## 🎯 Основные принципы

### 1. Централизация тестовой инфраструктуры

**Проблема:** Разрозненные подходы к созданию моков и тестовых данных в разных тестах.

**Решение:** Использовать централизованные фабрики и инфраструктуру.

#### ✅ Рекомендации:

```csharp
// ✅ Хорошо: Централизованная фабрика
public class MessageHandlerTestFactory
{
    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock { get; } = new();
    public Mock<IMessageService> MessageServiceMock { get; } = new();
    // ... другие моки
    
    public MessageHandler CreateMessageHandlerWithFake(FakeTelegramClient fakeClient)
    {
        // Настройка моков
        // Создание MessageHandler с моками
    }
}
```

```csharp
// ❌ Плохо: Создание моков в каждом тесте
[Test]
public void SomeTest()
{
    var mock1 = new Mock<IService1>();
    var mock2 = new Mock<IService2>();
    // Дублирование кода
}
```

### 2. Единообразие в создании моков

**Проблема:** Разные подходы к созданию моков (свойства vs поля vs локальные переменные).

**Решение:** Использовать единый подход - свойства с инициализацией.

#### ✅ Рекомендации:

```csharp
// ✅ Хорошо: Свойства с инициализацией
public class TestFactory
{
    public Mock<IService> ServiceMock { get; } = new();
    public Mock<ILogger> LoggerMock { get; } = new();
}

// ❌ Плохо: Свойства, создающие новые моки каждый раз
public class TestFactory
{
    public Mock<IService> ServiceMock => new Mock<IService>(); // Создает новый мок каждый раз!
}
```

### 3. Использование MessageEnvelope для тестирования

**Проблема:** Ограничения Telegram.Bot API (MessageId всегда 0 в тестах).

**Решение:** Использовать MessageEnvelope для централизованного создания и отслеживания сообщений.

#### ✅ Рекомендации:

```csharp
// ✅ Хорошо: Использование MessageEnvelope
[Test]
public async Task MessageHandlerDeletionTest()
{
    var envelope = new MessageEnvelope
    {
        MessageId = 456,
        ChatId = 123,
        UserId = 789,
        Text = "SPAM MESSAGE"
    };
    
    fakeClient.RegisterMessageEnvelope(envelope);
    var message = fakeClient.CreateMessageFromEnvelope(envelope);
    
    await handler.HandleAsync(message);
    
    Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True);
}
```

### 4. Интерфейсы для тестирования

**Проблема:** Сложность тестирования классов с множественными зависимостями.

**Решение:** Добавлять интерфейсы для упрощения тестирования.

#### ✅ Рекомендации:

```csharp
// ✅ Хорошо: Интерфейс для тестирования
public interface IMessageHandler
{
    bool CanHandle(Message message);
    Task HandleAsync(Message message, CancellationToken cancellationToken = default);
}

public class MessageHandler : IUpdateHandler, IMessageHandler
{
    // Реализация методов интерфейса
    public bool CanHandle(Message message)
    {
        var update = new Update { Message = message };
        return CanHandle(update);
    }
    
    public async Task HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        var update = new Update { Message = message };
        await HandleAsync(update, cancellationToken);
    }
}
```

### 5. Вынесение повторяющейся настройки в инфраструктуру

**Проблема:** Дублирование кода настройки моков в тестах (3+ повторения).

**Решение:** Создавать методы для стандартных сценариев в тестовых фабриках.

#### ✅ Рекомендации:

```csharp
// ✅ Хорошо: Метод для стандартной настройки
public class MessageHandlerTestFactory
{
    public MessageHandlerTestFactory SetupStandardBanTestScenario()
    {
        WithAppConfigSetup(mock => 
        {
            mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
            mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
            mock.Setup(x => x.AdminChatId).Returns(123456789);
            mock.Setup(x => x.LogAdminChatId).Returns(987654321);
        });
        
        WithBotPermissionsServiceSetup(mock =>
        {
            mock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        });
        
        // ... другие стандартные настройки
        
        return this;
    }
}

// ✅ Использование в тестах
[Test]
public async Task BanUserForLongName_PrivateChat_LogsWarningAndSendsAdminNotification()
{
    var factory = new MessageHandlerTestFactory();
    
    // Используем стандартную настройку + специфичные для теста моки
    factory.SetupStandardBanTestScenario()
        .WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(user.Id, null)).Returns(false);
            // ... специфичные настройки
        });
    
    var handler = factory.CreateMessageHandler();
    // ... тест
}
```

## 🚨 Критические ошибки и их решения

### 1. Моки создаются заново каждый раз

**Проблема:**
```csharp
public Mock<IService> ServiceMock => new Mock<IService>(); // ❌ Новый мок каждый раз!
```

**Решение:**
```csharp
public Mock<IService> ServiceMock { get; } = new(); // ✅ Один мок на все время жизни
```

### 2. Смешивание реальных сервисов и моков

**Проблема:**
```csharp
// В фабрике используется реальный сервис
return new MessageHandler(..., realMessageService, ...); // ❌
// В тесте проверяется мок
MessageServiceMock.Verify(...); // ❌ Не сработает!
```

**Решение:**
```csharp
// В фабрике используется мок
return new MessageHandler(..., MessageServiceMock.Object, ...); // ✅
// В тесте проверяется тот же мок
MessageServiceMock.Verify(...); // ✅ Работает!
```

### 3. Неправильная настройка FakeTelegramClient

**Проблема:** MessageId = 0 из-за ограничений Telegram.Bot API.

**Решение:** Использовать логику поиска envelope по ChatId.

```csharp
public async Task<bool> DeleteMessageAsync(ChatId chatId, int messageId, CancellationToken cancellationToken = default)
{
    var actualMessageId = messageId;
    if (messageId == 0)
    {
        // Ищем envelope по ChatId
        var envelope = _messageEnvelopes.Values.FirstOrDefault(e => e.ChatId == chatId.Identifier);
        if (envelope != null)
        {
            actualMessageId = envelope.MessageId;
        }
    }
    
    // Удаляем сообщение с правильным ID
    _deletedMessages.Add(new DeletedMessage { ChatId = chatId.Identifier, MessageId = actualMessageId });
    return true;
}
```

### 4. Неправильные типы в Moq настройках

**Проблема:** Несоответствие типов параметров в моках и реальных методах.

**Решение:** Использовать правильные типы из Telegram.Bot API.

```csharp
// ❌ Плохо: Неправильные типы
_botMock.Setup(x => x.BanChatMember(It.IsAny<long>(), It.IsAny<long>(), ...))
    .Returns(Task.CompletedTask);

// ✅ Хорошо: Правильные типы
_botMock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), ...))
    .Returns(Task.CompletedTask);

// ❌ Плохо: Неправильные типы для DeleteMessage
_botMock.Setup(x => x.DeleteMessage(It.IsAny<Chat>(), It.IsAny<int>(), ...))
    .Returns(Task.CompletedTask);

// ✅ Хорошо: Правильные типы для DeleteMessage
_botMock.Setup(x => x.DeleteMessage(It.IsAny<long>(), It.IsAny<int>(), ...))
    .Returns(Task.CompletedTask);
```

### 5. Несоответствие методов в тестах и реализации

**Проблема:** Тесты ожидают один метод, а реализация использует другой.

**Решение:** Приводить реализацию в соответствие с ожиданиями тестов или обновлять тесты.

```csharp
// ❌ Проблема: Тест ожидает ForwardToLogWithNotificationAsync
_messageServiceMock.Verify(x => x.ForwardToLogWithNotificationAsync(...), Times.Once);

// Но реализация использует SendLogNotificationAsync
await _messageService.SendLogNotificationAsync(...);

// ✅ Решение: Использовать консистентный метод
await _messageService.ForwardToLogWithNotificationAsync(message, logNotificationType, autoBanData, cancellationToken);
```

### 6. Конфликты с конфигурацией чатов в тестах

**Проблема:** `ChatSettingsManager.GetChatType()` возвращает неожиданные значения для тестовых чатов.

**Решение:** Использовать уникальные ID чатов в тестах.

```csharp
// ❌ Плохо: Использование стандартных ID
var chat = TestDataFactory.CreateGroupChat(); // ID может конфликтовать с настройками

// ✅ Хорошо: Уникальные ID для тестов
var chat = new Chat
{
    Id = -1001999999999, // Уникальный ID, который точно не настроен
    Type = ChatType.Group,
    Title = "Test Group for Channel Ban",
    Username = "testgroupchannel"
};
```

### 7. Недостаточная настройка моков для Telegram API

**Проблема:** Методы Telegram API не настроены в моках, что приводит к исключениям.

**Решение:** Настраивать все необходимые методы Telegram API.

```csharp
// ✅ Хорошо: Полная настройка моков
factory.WithBotSetup(mock => 
{
    // Настраиваем GetChat для корректной работы HandleChannelMessageAsync
    mock.Setup(x => x.GetChat(It.IsAny<ChatId>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(chat);
    
    // Настраиваем другие методы по необходимости
    mock.Setup(x => x.DeleteMessage(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);
});
```

## 📋 Чек-лист для новых тестов

### Перед написанием теста:

- [ ] Используется ли централизованная фабрика?
- [ ] Все ли моки созданы как свойства с инициализацией?
- [ ] Используется ли MessageEnvelope для создания сообщений?
- [ ] Есть ли интерфейс для упрощения тестирования?
- [ ] Все ли зависимости заменены на моки?
- [ ] **Проверены ли типы параметров в моках?**
- [ ] **Используются ли уникальные ID для тестовых чатов?**
- [ ] **Настроены ли все необходимые методы Telegram API?**

### При написании теста:

- [ ] Регистрируется ли envelope в FakeTelegramClient?
- [ ] Создается ли сообщение через CreateMessageFromEnvelope?
- [ ] Проверяется ли результат через WasMessageDeleted?
- [ ] Настроены ли все необходимые моки?
- [ ] Добавлены ли отладочные логи для сложных случаев?
- [ ] **Используется ли SetupStandardBanTestScenario() для повторяющейся настройки?**
- [ ] **Проверены ли соответствия методов в тестах и реализации?**

### После написания теста:

- [ ] Тест проходит локально?
- [ ] Тест не влияет на другие тесты?
- [ ] Код покрыт ассертами?
- [ ] Отладочные логи убраны или оставлены только необходимые?
- [ ] **Проверены ли все типы в Moq настройках?**
- [ ] **Убраны ли временные скрипты для исправления типов?**

## 🔧 Инструменты и утилиты

### Отладочные логи

```csharp
// Добавлять в сложные тесты для отладки
Console.WriteLine($"DEBUG: {variable} = {value}");
```

### Проверка состояния моков

```csharp
// Проверка вызовов моков
MessageServiceMock.Verify(x => x.SendUserNotificationWithReplyAsync(
    It.Is<User>(u => u.Id == expectedUserId),
    It.Is<Chat>(c => c.Id == expectedChatId),
    It.Is<UserNotificationType>(t => t == expectedType),
    It.IsAny<object>(),
    It.IsAny<CancellationToken>()), Times.Once);
```

### Создание тестовых данных

```csharp
// Использовать TestDataFactory для создания тестовых данных
var message = TestDataFactory.CreateValidMessage();
var user = TestDataFactory.CreateUser(789);
var chat = TestDataFactory.CreateChat(123);
```

### Скрипты для массового исправления

```python
# Для исправления типов в больших файлах
import re

def fix_types_in_file(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Исправляем типы
    content = re.sub(
        r'BanChatMember\(It\.IsAny<long>\(\)',
        'BanChatMember(It.IsAny<ChatId>()',
        content
    )
    
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)
```

## 📚 Примеры хороших тестов

### Тест удаления сообщения

```csharp
[Test]
public async Task MessageHandlerDeletionTest_TraceProblem()
{
    // Arrange
    var factory = new MessageHandlerTestFactory();
    var fakeClient = factory.FakeTelegramClient;
    var handler = factory.CreateMessageHandlerWithFake(fakeClient);
    
    var envelope = new MessageEnvelope
    {
        MessageId = 456,
        ChatId = 123,
        UserId = 789,
        Text = "SPAM MESSAGE"
    };
    
    fakeClient.RegisterMessageEnvelope(envelope);
    var message = fakeClient.CreateMessageFromEnvelope(envelope);
    
    // Настройка моков
    factory.ModerationServiceMock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
        .ReturnsAsync(new ModerationResult { Action = ModerationAction.Delete, Reason = "SPAM detected" });
    
    factory.ModerationServiceMock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
        .Returns(false);
    
    // Act
    await handler.HandleAsync(message);
    
    // Assert
    Assert.That(fakeClient.WasMessageDeleted(envelope), Is.True, "Сообщение должно быть удалено");
    factory.MessageServiceMock.Verify(x => x.SendUserNotificationWithReplyAsync(
        It.Is<User>(u => u.Id == 789),
        It.Is<Chat>(c => c.Id == 123),
        It.Is<UserNotificationType>(t => t == UserNotificationType.ModerationWarning),
        It.IsAny<object>(),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

### Тест с использованием стандартной настройки

```csharp
[Test]
public async Task BanUserForLongName_PrivateChat_LogsWarningAndSendsAdminNotification()
{
    // Arrange
    var factory = new MessageHandlerTestFactory();
    var user = TestDataFactory.CreateValidUser();
    var chat = TestDataFactory.CreateGroupChat();
    var userJoinMessage = TestDataFactory.CreateNewUserJoinMessage(user.Id);
    userJoinMessage.Chat = chat;

    // Используем стандартную настройку из инфраструктуры + специфичные для теста моки
    factory.SetupStandardBanTestScenario()
        .WithUserManagerSetup(mock =>
        {
            mock.Setup(x => x.Approved(user.Id, null)).Returns(false);
            mock.Setup(x => x.InBanlist(user.Id)).ReturnsAsync(false);
            mock.Setup(x => x.GetClubUsername(user.Id)).ReturnsAsync((string?)null);
        })
        .WithModerationServiceSetup(mock => 
        {
            mock.Setup(x => x.CheckMessageAsync(It.IsAny<Message>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Allow, "Valid message"));
            mock.Setup(x => x.IsUserApproved(It.IsAny<long>(), It.IsAny<long>()))
                .Returns(false);
            mock.Setup(x => x.CheckUserNameAsync(It.IsAny<User>()))
                .ReturnsAsync(new ModerationResult(ModerationAction.Ban, "Длинное имя пользователя"));
        });
    
    var handler = factory.CreateMessageHandler();

    // Act
    var update = new Update { Message = userJoinMessage };
    await handler.HandleAsync(update, CancellationToken.None);

    // Assert
    factory.UserBanServiceMock.Verify(
        x => x.BanUserForLongNameAsync(
            It.IsAny<Message>(),
            It.IsAny<User>(),
            It.IsAny<string>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

## 🎯 Заключение

Следование этим принципам поможет:

1. **Упростить отладку** - централизованная инфраструктура
2. **Избежать дублирования** - переиспользование фабрик
3. **Повысить надежность** - единообразные подходы
4. **Ускорить разработку** - готовые инструменты
5. **Избежать ошибок типов** - правильные типы в моках
6. **Обеспечить консистентность** - единообразие методов

**Помните:** Хороший тест - это не только проверка функциональности, но и документирование поведения системы. 