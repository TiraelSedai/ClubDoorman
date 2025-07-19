# План исправления критичных проблем ClubDoorman

## 🎯 Принципы работы

### Безопасность превыше всего:
- **Никаких изменений без тестов**
- **Пошаговые коммиты** с проверкой после каждого
- **Откат готов** при любых проблемах
- **Проверка сборки** после каждого изменения

### Лучшие практики:
- **TDD подход**: тест → исправление → проверка
- **Малые изменения**: один файл за раз
- **Документирование**: каждое изменение
- **Валидация**: тесты проверяют логику, а не код

## 📋 Фаза 1: Критичные NullReferenceException (День 1)

### 1.1 Исправление SimpleFilters.cs

**Проблема**: Методы падают при null входных данных
**Приоритет**: КРИТИЧЕСКИЙ
**Риск**: НИЗКИЙ (добавление проверок)

#### Шаг 1.1.1: Создание тестов
```csharp
[Test]
public void HasStopWords_WithNullInput_ThrowsArgumentNullException()
{
    Assert.Throws<ArgumentNullException>(() => SimpleFilters.HasStopWords(null!));
}

[Test]
public void HasStopWords_WithEmptyString_ReturnsFalse()
{
    var result = SimpleFilters.HasStopWords("");
    Assert.That(result, Is.False);
}

[Test]
public void FindAllRussianWordsWithLookalikeSymbols_WithNullInput_ThrowsArgumentNullException()
{
    Assert.Throws<ArgumentNullException>(() => 
        SimpleFilters.FindAllRussianWordsWithLookalikeSymbols(null!));
}
```

#### Шаг 1.1.2: Исправление кода
```csharp
public static bool HasStopWords(string message)
{
    ArgumentNullException.ThrowIfNull(message);
    return StopWords.Any(sw => message.Contains(sw, StringComparison.InvariantCultureIgnoreCase));
}

public static List<string> FindAllRussianWordsWithLookalikeSymbols(string message)
{
    ArgumentNullException.ThrowIfNull(message);
    return TextProcessor
        .NormalizeText(message)
        .Split(null)
        .Where(word => IsRussianWord(word) && word.Any(c => !IsCyrillicLowercase(c) && !AllowedNonRussianCyrillicOrDigit(c)))
        .ToList();
}
```

#### Шаг 1.1.3: Проверка
- [ ] Запуск тестов
- [ ] Проверка сборки
- [ ] Коммит изменений

### 1.2 Исправление TextProcessor.cs

**Проблема**: NormalizeText падает при null
**Приоритет**: КРИТИЧЕСКИЙ
**Риск**: НИЗКИЙ

#### Шаг 1.2.1: Создание тестов
```csharp
[Test]
public void NormalizeText_WithNullInput_ThrowsArgumentNullException()
{
    Assert.Throws<ArgumentNullException>(() => TextProcessor.NormalizeText(null!));
}

[Test]
public void NormalizeText_WithEmptyString_ReturnsEmptyString()
{
    var result = TextProcessor.NormalizeText("");
    Assert.That(result, Is.EqualTo(""));
}

[Test]
public void NormalizeText_WithNormalText_ReturnsNormalizedText()
{
    var input = "Hello, World! 🌍";
    var result = TextProcessor.NormalizeText(input);
    Assert.That(result, Is.EqualTo("hello world"));
}
```

#### Шаг 1.2.2: Исправление кода
```csharp
public static string NormalizeText(string input)
{
    ArgumentNullException.ThrowIfNull(input);
    
    var result = input.ReplaceLineEndings(" ");
    result = result.ToLowerInvariant();
    result = StripEmojisAndPunctuation(result);
    result = WhitespaceCompacter.Replace(result, " ");
    result = StripDiacritics(result);
    return result;
}
```

## 📋 Фаза 2: Утечки памяти в Worker (День 1-2)

### 2.1 Анализ проблемы

**Проблема**: PeriodicTimer'ы не освобождаются
**Приоритет**: КРИТИЧЕСКИЙ
**Риск**: СРЕДНИЙ (изменение жизненного цикла)

#### Шаг 2.1.1: Создание тестов жизненного цикла
```csharp
[Test]
public void Worker_Dispose_ReleasesAllResources()
{
    // Arrange
    var worker = CreateWorker();
    
    // Act
    worker.Dispose();
    
    // Assert - проверяем, что ресурсы освобождены
    // (через reflection или интерфейс)
}
```

#### Шаг 2.1.2: Исправление Worker.cs
```csharp
public class Worker : BackgroundService, IDisposable
{
    private readonly PeriodicTimer _timer;
    private readonly PeriodicTimer _banlistRefreshTimer;
    private readonly PeriodicTimer _membersCountUpdateTimer;
    private bool _disposed;

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _timer?.Dispose();
            _banlistRefreshTimer?.Dispose();
            _membersCountUpdateTimer?.Dispose();
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}
```

## 📋 Фаза 3: Расширение покрытия тестами (День 2-3)

### 3.1 Тесты для критичной логики

#### 3.1.1 Тесты модерации
```csharp
[TestFixture]
public class ModerationServiceTests
{
    [Test]
    public void CheckMessage_WithSpamText_ReturnsSpamResult()
    {
        // Arrange
        var service = CreateModerationService();
        var message = "SPAM SPAM SPAM SPAM SPAM SPAM SPAM SPAM SPAM SPAM";
        
        // Act
        var result = service.CheckMessage(message);
        
        // Assert
        Assert.That(result.IsSpam, Is.True);
    }
    
    [Test]
    public void CheckMessage_WithNormalText_ReturnsGoodResult()
    {
        // Arrange
        var service = CreateModerationService();
        var message = "Привет, как дела?";
        
        // Act
        var result = service.CheckMessage(message);
        
        // Assert
        Assert.That(result.IsSpam, Is.False);
    }
}
```

#### 3.1.2 Тесты AI анализа
```csharp
[TestFixture]
public class AiChecksTests
{
    [Test]
    public void CheckProfile_WithAiGeneratedName_ReturnsHighScore()
    {
        // Arrange
        var service = CreateAiChecks();
        var user = new User { FirstName = "AI Generated Name 123" };
        
        // Act
        var result = service.CheckProfile(user);
        
        // Assert
        Assert.That(result.Score, Is.GreaterThan(0.7));
    }
}
```

## 📋 Фаза 4: Производительность (День 3-4)

### 4.1 LoggerMessage оптимизация

#### Шаг 4.1.1: Создание LoggerMessage делегатов
```csharp
public static partial class LogMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "User {UserId} sent message")]
    public static partial void LogUserMessage(this ILogger logger, long userId);
    
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Spam detected for user {UserId}")]
    public static partial void LogSpamDetected(this ILogger logger, long userId);
}
```

#### Шаг 4.1.2: Замена обычного логирования
```csharp
// Было
_logger.LogInformation("User {UserId} sent message", userId);

// Стало
_logger.LogUserMessage(userId);
```

## 📋 Фаза 5: Локализация (День 4-5)

### 5.1 Исправление проблем с культурой

#### Шаг 5.1.1: Использование инвариантной культуры
```csharp
// Было
var result = text.ToLower();

// Стало
var result = text.ToLowerInvariant();

// Было
if (text.StartsWith("http"))

// Стало
if (text.StartsWith("http", StringComparison.OrdinalIgnoreCase))
```

## 🔄 Процесс выполнения

### Для каждого изменения:

1. **Создание ветки**: `fix/critical-null-checks`
2. **Написание тестов**: покрытие логики, а не кода
3. **Исправление кода**: минимальные изменения
4. **Проверка**: тесты + сборка
5. **Коммит**: с описанием изменений
6. **Слияние**: только после всех проверок

### Критерии готовности:

- [ ] Все тесты проходят
- [ ] Сборка успешна
- [ ] Покрытие тестами > 50%
- [ ] Нет регрессий в функциональности
- [ ] Документация обновлена

## 📊 Метрики успеха

### Количественные:
- **Покрытие тестами**: 15% → 70%+
- **Предупреждения**: 974 → < 200
- **Критичные ошибки**: 4 → 0

### Качественные:
- **Стабильность**: нет падений при null
- **Производительность**: улучшение логирования
- **Поддерживаемость**: читаемый код

## 🚨 План отката

### При любых проблемах:
1. **Остановка изменений**
2. **Анализ проблемы**
3. **Откат к последнему стабильному состоянию**
4. **Пересмотр плана**

### Контрольные точки:
- После каждого файла
- После каждой фазы
- Перед слиянием в основную ветку 