# План улучшения покрытия тестами

## Текущий статус
- ✅ **59 тестов проходят успешно**
- ✅ **Критические null-safety тесты добавлены**
- ✅ **ModerationService покрыт базовыми тестами**
- ✅ **Интерфейс IAiChecks создан для тестируемости**

## Следующие приоритеты

### 1. Расширение тестов ModerationService (Высокий приоритет)
- [ ] Тесты для `CheckTextContentAsync` с различными сценариями
- [ ] Тесты для `CheckMediaContent` с фото, видео, стикерами
- [ ] Тесты для `IncrementGoodMessageCountAsync` с различными конфигурациями
- [ ] Тесты для `IsUserApproved` с разными режимами

### 2. Тесты для UserManager (Высокий приоритет)
- [ ] Тесты для `UserManager` (старая система)
- [ ] Тесты для `UserManagerV2` (новая система)
- [ ] Тесты для методов `Approve`, `InBanlist`, `Approved`
- [ ] Тесты для интеграции с внешними API

### 3. Тесты для SpamHamClassifier (Средний приоритет)
- [ ] Тесты для `IsSpam` с различными сообщениями
- [ ] Тесты для `AddSpam` и `AddHam`
- [ ] Тесты для обучения модели
- [ ] Тесты для обработки ошибок

### 4. Тесты для CaptchaService (Средний приоритет)
- [ ] Тесты для `CreateCaptchaAsync`
- [ ] Тесты для `VerifyCaptchaAsync`
- [ ] Тесты для `BanExpiredCaptchaUsersAsync`
- [ ] Тесты для обработки ошибок

### 5. Тесты для MessageHandler (Низкий приоритет)
- [ ] Тесты для обработки различных типов сообщений
- [ ] Тесты для интеграции с ModerationService
- [ ] Тесты для AI анализа профилей
- [ ] Тесты для обработки ошибок

### 6. Интеграционные тесты (Низкий приоритет)
- [ ] Тесты полного цикла модерации
- [ ] Тесты для Worker.cs
- [ ] Тесты для UpdateDispatcher
- [ ] Тесты для CallbackQueryHandler

## Метрики качества

### Целевые показатели
- **Покрытие кода**: >80%
- **Покрытие критических путей**: 100%
- **Время выполнения тестов**: <30 секунд
- **Количество тестов**: >100

### Текущие показатели
- **Покрытие кода**: ~40% (оценка)
- **Количество тестов**: 59
- **Время выполнения**: ~5 секунд

## Рекомендации по реализации

### 1. Использование TestData
```csharp
public static class TestData
{
    public static User CreateTestUser(long id = 123, string firstName = "Test")
    {
        return new User { Id = id, FirstName = firstName };
    }
    
    public static Chat CreateTestChat(long id = 456, string title = "Test Chat")
    {
        return new Chat { Id = id, Title = title, Type = ChatType.Group };
    }
    
    public static Message CreateTestMessage(User? user = null, Chat? chat = null, string? text = null)
    {
        return new Message 
        { 
            From = user ?? CreateTestUser(),
            Chat = chat ?? CreateTestChat(),
            Text = text ?? "Test message"
        };
    }
}
```

### 2. Использование TestFixtures
```csharp
[TestFixture]
public class ModerationServiceIntegrationTests
{
    private ModerationService _moderationService;
    private TestData _testData;
    
    [SetUp]
    public void Setup()
    {
        // Настройка с реальными зависимостями для интеграционных тестов
    }
}
```

### 3. Мокирование внешних зависимостей
```csharp
// Для тестов, требующих внешних API
_mockHttpClient.Setup(x => x.GetAsync(It.IsAny<string>()))
    .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("{}") });
```

## Следующие шаги

1. **Немедленно**: Добавить тесты для `CheckTextContentAsync` в ModerationService
2. **На этой неделе**: Покрыть UserManager тестами
3. **На следующей неделе**: Добавить тесты для SpamHamClassifier
4. **В долгосрочной перспективе**: Интеграционные тесты

## Файлы для создания

- `ClubDoorman.Test/TestData.cs` - общие тестовые данные
- `ClubDoorman.Test/UserManagerTests.cs` - тесты для UserManager
- `ClubDoorman.Test/SpamHamClassifierTests.cs` - тесты для SpamHamClassifier
- `ClubDoorman.Test/CaptchaServiceTests.cs` - тесты для CaptchaService
- `ClubDoorman.Test/IntegrationTests.cs` - интеграционные тесты 