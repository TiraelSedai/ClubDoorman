# Golden Master Тесты для логики банов

## Обзор

Golden Master тесты фиксируют текущее поведение системы и помогают обнаружить регрессии при изменениях в коде. Для логики банов в `MessageHandler` создана специальная инфраструктура с использованием существующих билдеров, Bogus с сидами и Verify.NUnit.

## Архитектура

### Основные компоненты

1. **TestKitGoldenMaster** - основная инфраструктура
2. **BanScenarioBuilder** - билдер сценариев с сидами
3. **BanScenarioFactory** - фабрика множественных сценариев
4. **BanScenario/BanScenarioResult** - модели данных

### Принципы

- ✅ **Билдеры** - контролируемые, повторяемые входы
- ✅ **Bogus с сидами** - стабильные данные для snapshot тестов
- ✅ **Verify.NUnit** - автоматическое сравнение snapshot'ов
- ✅ **Множественные сценарии** - покрытие различных случаев
- ✅ **JSON сериализация** - детальный анализ данных

## Использование

### Базовый Golden Master тест

```csharp
[Test]
public async Task MyMethod_GoldenMaster()
{
    // Arrange: Создаем сценарии с сидами
    var scenarios = BanScenarioFactory.CreateScenarioSet(count: 20, baseSeed: 42);
    var results = new List<BanScenarioResult>();

    // Act: Выполняем каждый сценарий
    foreach (var scenario in scenarios)
    {
        try
        {
            await _messageHandler.BanUserForLongName(
                scenario.Message, 
                scenario.User, 
                scenario.Reason, 
                scenario.BanDuration, 
                CancellationToken.None);

            // Анализируем результат
            var result = new BanScenarioResult
            {
                Input = scenario,
                ShouldCallBanChatMember = scenario.Chat.Type != ChatType.Private,
                ShouldCallDeleteMessage = scenario.Message != null,
                // ... другие свойства
                HasException = false
            };

            results.Add(result);
        }
        catch (Exception ex)
        {
            var result = new BanScenarioResult
            {
                Input = scenario,
                HasException = true,
                ExceptionType = ex.GetType().Name,
                ExceptionMessage = ex.Message
            };

            results.Add(result);
        }
    }

    // Assert: Golden Master snapshot
    await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
        "MyMethod_GoldenMaster",
        results,
        "MyMethod_Snapshot");
}
```

### JSON Snapshot для детального анализа

```csharp
[Test]
public async Task MyMethod_JsonSnapshot_GoldenMaster()
{
    // Arrange: Создаем разнообразные сценарии
    var scenarios = new List<BanScenario>
    {
        new BanScenarioBuilder(1).CreateTemporaryBanScenario(),
        new BanScenarioBuilder(2).CreatePermanentBanScenario(),
        new BanScenarioBuilder(3).CreatePrivateChatBanScenario(),
        // ...
    };

    var testData = new
    {
        TestName = "MyMethod_JsonSnapshot_GoldenMaster",
        Scenarios = scenarios.Select(s => new
        {
            s.Seed,
            s.ScenarioType,
            User = new { s.User.Id, s.User.FirstName, s.User.Username },
            Chat = new { s.Chat.Id, s.Chat.Type, s.Chat.Title },
            Message = s.Message != null ? new { s.Message.MessageId, s.Message.Text } : null,
            s.BanDuration,
            s.Reason
        }).ToList()
    };

    // Act: Выполняем сценарии
    foreach (var scenario in scenarios)
    {
        await _messageHandler.BanUserForLongName(/* ... */);
    }

    // Assert: JSON snapshot
    await TestKitGoldenMaster.CreateGoldenMasterJsonSnapshot(
        "MyMethod_JsonSnapshot_GoldenMaster",
        testData,
        "JsonSnapshot_MyMethod");
}
```

### Тестирование исключений

```csharp
[Test]
public async Task MyMethod_ExceptionScenarios_GoldenMaster()
{
    // Arrange: Создаем сценарии с исключениями
    var scenarios = BanScenarioFactory.CreateExceptionScenarioSet(count: 5, baseSeed: 100);
    var results = new List<BanScenarioResult>();

    // Act: Выполняем каждый сценарий с исключением
    foreach (var scenario in scenarios)
    {
        // Создаем MessageHandler с исключением
        var factoryWithException = _factory.SetupExceptionScenario(
            new InvalidOperationException($"Bot API error for scenario {scenario.Seed}"));
        var messageHandlerWithException = factoryWithException.CreateMessageHandler();

        try
        {
            await messageHandlerWithException.BanUserForLongName(/* ... */);
            // ...
        }
        catch (Exception ex)
        {
            var result = new BanScenarioResult
            {
                Input = scenario,
                HasException = true,
                ExceptionType = ex.GetType().Name,
                ExceptionMessage = ex.Message
            };
            results.Add(result);
        }
    }

    // Assert: Golden Master snapshot для исключений
    await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
        "MyMethod_ExceptionScenarios_GoldenMaster",
        results,
        "ExceptionScenarios_MyMethod");
}
```

## Доступные сценарии

### BanScenarioBuilder методы

- `CreateTemporaryBanScenario()` - временный бан
- `CreatePermanentBanScenario()` - перманентный бан
- `CreatePrivateChatBanScenario()` - бан в приватном чате
- `CreateNullMessageBanScenario()` - бан без сообщения
- `CreateBotBanScenario()` - бан бота

### BanScenarioFactory методы

- `CreateScenarioSet(count, baseSeed)` - набор разнообразных сценариев
- `CreateExceptionScenarioSet(count, baseSeed)` - сценарии с исключениями

## Настройка моков

```csharp
// Базовые моки для Golden Master тестов
_factory = TK.CreateMessageHandlerFactory()
    .SetupGoldenMasterMocks();

// Моки с исключениями
var factoryWithException = _factory.SetupExceptionScenario(
    new InvalidOperationException("Bot API error"));
```

## Структура файлов

```
GoldenMasterSnapshots/
├── MultipleScenarios_BanUserForLongName.verified.json
├── JsonSnapshot_BanUserForLongName.verified.json
├── ExceptionScenarios_BanUserForLongName.verified.json
├── MultipleScenarios_BanBlacklistedUser.verified.json
├── MultipleScenarios_AutoBanChannel.verified.json
└── Comprehensive_AllBanTypes.verified.json
```

## Лучшие практики

### 1. Используйте сиды для стабильности

```csharp
// ✅ Хорошо - стабильные данные
var builder = new BanScenarioBuilder(42);

// ❌ Плохо - нестабильные данные
var builder = new BanScenarioBuilder(Random.Shared.Next());
```

### 2. Группируйте связанные сценарии

```csharp
// ✅ Хорошо - логическая группировка
var temporaryBans = Enumerable.Range(0, 5).Select(i => 
    new BanScenarioBuilder(400 + i).CreateTemporaryBanScenario());

var permanentBans = Enumerable.Range(0, 5).Select(i => 
    new BanScenarioBuilder(500 + i).CreatePermanentBanScenario());
```

### 3. Обрабатывайте исключения

```csharp
// ✅ Хорошо - полная обработка
try
{
    await _messageHandler.BanUserForLongName(/* ... */);
    results.Add(new BanScenarioResult { HasException = false });
}
catch (Exception ex)
{
    results.Add(new BanScenarioResult 
    { 
        HasException = true,
        ExceptionType = ex.GetType().Name,
        ExceptionMessage = ex.Message
    });
}
```

### 4. Используйте JSON для детального анализа

```csharp
// ✅ Хорошо - детальная информация
var testData = new
{
    TestName = "MyMethod_JsonSnapshot_GoldenMaster",
    Scenarios = scenarios.Select(s => new
    {
        s.Seed,
        s.ScenarioType,
        User = new { s.User.Id, s.User.FirstName, s.User.Username },
        Chat = new { s.Chat.Id, s.Chat.Type, s.Chat.Title },
        // ...
    }).ToList()
};
```

## Запуск тестов

### Все Golden Master тесты

```bash
dotnet test --filter "Category=golden-master"
```

### Конкретный тест

```bash
dotnet test --filter "TestName=BanUserForLongName_MultipleScenarios_GoldenMaster"
```

### Обновление snapshot'ов

```bash
# После изменения логики банов
dotnet test --filter "Category=golden-master" --verbosity normal
```

## Анализ результатов

### Просмотр snapshot файлов

```bash
# Просмотр JSON snapshot'ов
cat GoldenMasterSnapshots/JsonSnapshot_BanUserForLongName.verified.json | jq .

# Просмотр обычных snapshot'ов
cat GoldenMasterSnapshots/MultipleScenarios_BanUserForLongName.verified.json | jq .
```

### Интерпретация результатов

1. **HasException: false** - сценарий выполнен успешно
2. **HasException: true** - произошло исключение (проверить ExceptionType/Message)
3. **ShouldCallBanChatMember: true** - должен был вызваться BanChatMember
4. **ShouldCallDeleteMessage: true** - должен был вызваться DeleteMessage

## Расширение

### Добавление нового типа сценария

```csharp
public class BanScenarioBuilder
{
    public BanScenario CreateNewScenarioType()
    {
        var user = TestKitBuilders.CreateUser()
            .WithId(_faker.Random.Long(100000000, 999999999))
            .WithFirstName(_faker.Name.FirstName())
            .WithUsername(_faker.Internet.UserName())
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(_faker.Random.Long(-1000000000000, -100000000000))
            .WithType(ChatType.Supergroup)
            .WithTitle(_faker.Company.CompanyName())
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(chat)
            .WithMessageId(_faker.Random.Int(1, 99999))
            .WithText(_faker.Lorem.Sentence())
            .Build();

        return new BanScenario
        {
            User = user,
            Chat = chat,
            Message = message,
            BanDuration = TimeSpan.FromMinutes(_faker.Random.Int(5, 60)),
            Reason = "Новый тип сценария",
            ScenarioType = "NewScenarioType",
            Seed = _seed
        };
    }
}
```

### Добавление нового теста

```csharp
[Test]
public async Task NewMethod_GoldenMaster()
{
    // Arrange
    var scenarios = BanScenarioFactory.CreateScenarioSet(count: 15, baseSeed: 1000);
    var results = new List<BanScenarioResult>();

    // Act
    foreach (var scenario in scenarios)
    {
        try
        {
            await _messageHandler.NewMethod(/* ... */);
            // Анализ результата
            results.Add(new BanScenarioResult { /* ... */ });
        }
        catch (Exception ex)
        {
            results.Add(new BanScenarioResult 
            { 
                Input = scenario,
                HasException = true,
                ExceptionType = ex.GetType().Name,
                ExceptionMessage = ex.Message
            });
        }
    }

    // Assert
    await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
        "NewMethod_GoldenMaster",
        results,
        "NewMethod_Snapshot");
}
```

## Заключение

Golden Master тесты обеспечивают:

- **Стабильность** - фиксированное поведение с сидами
- **Покрытие** - множественные сценарии
- **Детализацию** - JSON сериализация для анализа
- **Регрессионное тестирование** - автоматическое сравнение
- **Расширяемость** - легко добавлять новые сценарии

Используйте эту инфраструктуру для надежного тестирования логики банов и обнаружения регрессий при изменениях в коде. 