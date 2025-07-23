# DX Tool - Генератор TestFactory для C# классов

Автоматический инструмент для генерации TestFactory классов на основе анализа конструкторов C# классов.

## Описание

DX Tool анализирует конструкторы C# классов, оценивает их сложность и генерирует соответствующие TestFactory с правильной стратегией мокирования и инициализации.

## Возможности

### Анализ сложности
- Автоматическая оценка сложности конструкторов (0-10 баллов)
- Определение уровней сложности: LOW, MEDIUM, HIGH
- Анализ типов параметров (интерфейсы, конкретные классы, логгеры)
- Предложение маркеров для сложных случаев

### Генерация TestFactory
- Автоматическое определение стратегии мокирования
- Использование тестовых утилит вместо моков где это уместно
- Генерация кастомных конструкторов для сложной инициализации
- Создание дополнительных методов для гибкости
- Правильные using директивы и namespace

### Поддержка типов
- Интерфейсы - автоматическое создание моков
- Конкретные классы - умное определение стратегии мокирования
- Логгеры - использование NullLogger
- Telegram-зависимости - автоматическое использование FakeTelegramClient

## Архитектура

### Основные компоненты

#### ComplexityAnalyzer
Анализирует конструкторы и определяет:
- Оценку сложности (0-10)
- Уровень сложности (LOW/MEDIUM/HIGH)
- Предлагаемые маркеры
- Обоснование решений

#### CSharpAnalyzer
Парсит C# файлы и извлекает:
- Информацию о классах
- Параметры конструкторов
- Типы параметров
- Test-маркеры

#### TestFactoryGenerator
Генерирует TestFactory на основе:
- Результатов анализа сложности
- Найденных маркеров
- Типов параметров
- Конфигурации проекта

## Использование

### Базовое использование

```python
from csharp_analyzer import CSharpAnalyzer
from factory_generator import TestFactoryGenerator
from complexity_analyzer import ComplexityAnalyzer

# Инициализация
analyzer = CSharpAnalyzer("path/to/project")
generator = TestFactoryGenerator(Path("path/to/test/project"))
complexity_analyzer = ComplexityAnalyzer()

# Анализ класса
services = analyzer.find_service_classes()
target_class = next(s for s in services if s.name == "MessageHandler")

# Анализ сложности
complexity_report = analyzer.analyze_class_complexity(target_class)

# Поиск маркеров
markers = analyzer.find_test_markers(Path("path/to/source/file"))

# Генерация TestFactory
generator.set_complexity_analysis(complexity_report, markers)
factory_code = generator.generate_test_factory(target_class)
```

### Конфигурация

DX Tool поддерживает настройку через конфигурационные файлы:

```json
{
  "utility_types": {
    "ITelegramBotClientWrapper": "FakeTelegramClient",
    "ITelegramBotClient": "FakeTelegramClient",
    "TelegramBotClient": "FakeTelegramClient"
  },
  "always_mock_types": [
    "AiChecks", "SpamHamClassifier", "MimicryClassifier",
    "BadMessageManager", "GlobalStatsManager", "SuspiciousUsersStorage"
  ],
  "complexity_rules": {
    "high_threshold": 7,
    "medium_threshold": 3
  }
}
```

## Примеры генерации

### Сложный класс (MessageHandler)

```csharp
[TestFixture]
[Category("test-infrastructure")]
public class MessageHandlerTestFactory
{
    public Mock<IModerationService> ModerationServiceMock { get; } = new();
    public Mock<ICaptchaService> CaptchaServiceMock { get; } = new();
    public Mock<IUserManager> UserManagerMock { get; } = new();
    public Mock<SpamHamClassifier> ClassifierMock { get; } = new();
    public Mock<BadMessageManager> BadMessageManagerMock { get; } = new();
    public Mock<AiChecks> AiChecksMock { get; } = new();
    public Mock<GlobalStatsManager> GlobalStatsManagerMock { get; } = new();
    public Mock<IStatisticsService> StatisticsServiceMock { get; } = new();
    public Mock<IServiceProvider> ServiceProviderMock { get; } = new();
    public Mock<ILogger<MessageHandler>> LoggerMock { get; } = new();
    public FakeTelegramClient Bot { get; } = new();

    public MessageHandlerTestFactory()
    {
        // Кастомная инициализация для сложных типов
        var mockLogger = new Mock<ILogger<SpamHamClassifier>>();
        ClassifierMock = new Mock<SpamHamClassifier>(mockLogger.Object);
        
        var aiLogger = new Mock<ILogger<AiChecks>>();
        AiChecksMock = new Mock<AiChecks>(
            new TelegramBotClient("1234567890:ABCdefGHIjklMNOpqrsTUVwxyz"), 
            aiLogger.Object
        );
    }

    public MessageHandler CreateMessageHandler()
    {
        return new MessageHandler(
            Bot,
            ModerationServiceMock.Object,
            CaptchaServiceMock.Object,
            UserManagerMock.Object,
            ClassifierMock.Object,
            BadMessageManagerMock.Object,
            AiChecksMock.Object,
            GlobalStatsManagerMock.Object,
            StatisticsServiceMock.Object,
            ServiceProviderMock.Object,
            LoggerMock.Object
        );
    }

    // Дополнительные методы для гибкости
    public MessageHandler CreateMessageHandlerWithFake(FakeTelegramClient? Bot = null)
    {
        var client = Bot ?? new FakeTelegramClient();
        return new MessageHandler(client, /* ... */);
    }
}
```

### Простой класс (SpamHamClassifier)

```csharp
[TestFixture]
[Category("test-infrastructure")]
public class SpamHamClassifierTestFactory
{
    public Mock<ILogger<SpamHamClassifier>> LoggerMock { get; } = new();

    public SpamHamClassifier CreateSpamHamClassifier()
    {
        return new SpamHamClassifier(LoggerMock.Object);
    }
}
```

## Тестирование

### Запуск тестов

```bash
# Тест на одном классе
python test_improved_generator.py

# Тест на множестве классов
python test_multiple_classes.py

# Отладка парсинга
python debug_parser.py

# Отладка поиска файлов
python debug_finder.py
```

### Результаты тестирования

| Класс | Сложность | Покрытие | Статус |
|-------|-----------|----------|--------|
| MessageHandler | 10/10 | 100% | Полная функциональность |
| CallbackQueryHandler | 10/10 | 100% | Полная функциональность |
| ModerationService | 4/10 | 80% | Базовая функциональность |
| CaptchaService | 0/10 | 60% | Базовая функциональность |
| SpamHamClassifier | 0/10 | 40% | Базовая функциональность |

## Ограничения

### Известные проблемы
- Парсинг сложных generic типов может требовать доработки
- Некоторые паттерны C# кода могут не распознаваться корректно
- Логика сложности может требовать настройки для специфичных проектов

### Планы развития
- Конфигурационные файлы для настройки правил
- Поддержка других типов утилит
- Анализ зависимостей между параметрами
- Генерация тестов для TestFactory

## Требования

- Python 3.8+
- Доступ к исходному коду C# проекта
- Структура проекта с разделением на основной и тестовый проекты

## Лицензия

Внутренний инструмент для ClubDoorman проекта. 