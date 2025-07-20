# Система таймаутов тестов

## Обзор

Новая система таймаутов позволяет гибко настраивать время выполнения тестов через конфигурационный файл `test-timeouts.json`.

## Конфигурация

### Структура файла test-timeouts.json

```json
{
  "defaultTimeoutSeconds": 5,
  "testTimeouts": {
    "ModerationServiceTests": {
      "defaultTimeoutSeconds": 10,
      "specificTests": {
        "CheckMessage_WithSpamMessage_ReturnsBan": 15,
        "CheckMessage_WithHamMessage_ReturnsAllow": 15,
        "IncrementGoodMessageCount_WithValidUser_ApprovesUserAfterThreeMessages": 20
      }
    },
    "ModerationServiceSimpleTests": {
      "defaultTimeoutSeconds": 8,
      "specificTests": {
        "CheckMessage_WithBannedUser_ReturnsBan": 10
      }
    }
  }
}
```

### Параметры

- `defaultTimeoutSeconds` - глобальный таймаут по умолчанию (5 секунд)
- `testTimeouts` - настройки для конкретных классов тестов
  - `defaultTimeoutSeconds` - таймаут по умолчанию для класса
  - `specificTests` - таймауты для конкретных методов

## Использование

### Вариант 1: Наследование от TestBase (рекомендуется)

```csharp
[TestFixture]
public class MyTests : TestBase
{
    [Test]
    public async Task MyTest()
    {
        await ExecuteWithTimeout(async (cancellationToken) =>
        {
            // Ваш тест здесь
            var result = await someAsyncOperation(cancellationToken);
            Assert.That(result, Is.Not.Null);
        });
    }
}
```

### Вариант 2: Ручное управление

```csharp
[Test]
public async Task MyTest()
{
    var timeoutToken = TestTimeoutHelper.CreateTimeoutToken();
    
    try
    {
        // Ваш тест здесь
        var result = await someAsyncOperation(timeoutToken.Token);
        Assert.That(result, Is.Not.Null);
    }
    catch (OperationCanceledException) when (timeoutToken.Token.IsCancellationRequested)
    {
        var timeout = TestTimeoutHelper.GetTimeoutForTest("MyTests", "MyTest");
        throw new TimeoutException($"Test timed out after {timeout} seconds");
    }
    finally
    {
        timeoutToken.Dispose();
    }
}
```

## Рекомендуемые таймауты

### Быстрые тесты (3-5 секунд)
- Простые unit-тесты
- Тесты без внешних зависимостей
- Тесты с моками

### Средние тесты (8-12 секунд)
- Интеграционные тесты
- Тесты с реальными компонентами
- Тесты с ML-классификаторами

### Медленные тесты (15-20 секунд)
- Тесты с обучением ML-моделей
- Тесты с внешними API
- Комплексные интеграционные тесты

## Запуск тестов

```bash
# Все тесты
./scripts/run_tests_with_timeout.sh

# Конкретный класс
./scripts/run_tests_with_timeout.sh "ModerationServiceTests"

# Конкретный тест
./scripts/run_tests_with_timeout.sh "ModerationServiceTests.CheckMessage_WithSpamMessage_ReturnsBan"
```

## Отладка таймаутов

Если тест превышает таймаут:

1. Проверьте конфигурацию в `test-timeouts.json`
2. Увеличьте таймаут для конкретного теста
3. Проверьте, нет ли зависаний в коде
4. Используйте логирование для диагностики

## Преимущества

- ✅ Гибкая настройка таймаутов
- ✅ Возможность переопределения для конкретных тестов
- ✅ Автоматическое управление ресурсами
- ✅ Понятные сообщения об ошибках
- ✅ Централизованная конфигурация 