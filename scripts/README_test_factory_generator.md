# TestFactory Generator

DX Tool для автоматической генерации TestFactory на основе C# конструкторов.

## Возможности

- ✅ Автоматический анализ конструкторов C# классов
- ✅ Генерация TestFactory с правильными моками
- ✅ Поддержка интерфейсов, конкретных классов и логгеров
- ✅ Автоматическое определение необходимых using директив
- ✅ Генерация тестов для TestFactory
- ✅ Правильная обработка сложных зависимостей
- ✅ Модульная архитектура для расширяемости

## Использование

```bash
# Генерация TestFactory для всех сервисных классов
python3 scripts/generate_test_factory.py .

# С перезаписью существующих файлов
python3 scripts/generate_test_factory.py . --force

# Подробный вывод
python3 scripts/generate_test_factory.py . --verbose

# Или через модуль напрямую
python3 -m scripts.test_factory_generator . --force
```

## Архитектура

Тулза построена на модульной архитектуре:

```
scripts/test_factory_generator/
├── __init__.py              # Инициализация пакета
├── __main__.py              # Точка входа (CLI)
├── models.py                # Модели данных
├── csharp_analyzer.py       # Анализатор C# кода
├── factory_generator.py     # Генератор TestFactory
├── utils.py                 # Утилиты
└── README.md               # Документация модулей
```

### Модули

- **models.py** - модели данных (`ClassInfo`, `ConstructorParam`)
- **csharp_analyzer.py** - анализ C# кода и поиск классов
- **factory_generator.py** - генерация TestFactory и тестов
- **utils.py** - утилиты для работы с типами и строками
- **__main__.py** - CLI интерфейс и координация модулей

## Что генерируется

Для каждого сервисного класса создается:

1. **TestFactory** - фабрика для создания экземпляров с настроенными моками
2. **Тесты для TestFactory** - проверка корректности генерации

### Пример TestFactory

```csharp
[TestFixture]
[Category("test-infrastructure")]
public class ModerationServiceTestFactory
{
    public Mock<ISpamHamClassifier> ClassifierMock { get; } = new();
    public Mock<IMimicryClassifier> MimicryClassifierMock { get; } = new();
    // ... другие моки

    public ModerationService CreateModerationService()
    {
        return new ModerationService(
            ClassifierMock.Object,
            MimicryClassifierMock.Object,
            // ... другие зависимости
        );
    }

    // Методы для настройки моков
    public ModerationServiceTestFactory WithClassifierSetup(Action<Mock<ISpamHamClassifier>> setup)
    {
        setup(ClassifierMock);
        return this;
    }
}
```

## Поддерживаемые типы

### Интерфейсы
Автоматически определяются и создаются моки для:
- `ITelegramBotClient`, `IUserManager`, `IModerationService`
- `ICaptchaService`, `IStatisticsService`, `IUpdateDispatcher`
- `IUpdateHandler`, `ICommandHandler`, `ISpamHamClassifier`
- И другие интерфейсы с паттерном `I + PascalCase`

### Конкретные классы
Создаются реальные экземпляры для:
- `BadMessageManager`, `GlobalStatsManager`
- `SpamHamClassifier`, `MimicryClassifier`
- `SuspiciousUsersStorage`, `AiChecks`
- `TelegramBotClient` (с тестовым токеном)

### Логгеры
Используются `NullLogger<T>()` для тестов

## Планы развития

См. `plans/test_factory_generator_modular_development.md` для детального плана развития.

## Отчет о миграции

См. `scripts/test_factory_generator/MIGRATION_REPORT.md` для информации о переходе на модульную архитектуру. 