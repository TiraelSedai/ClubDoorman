# Тестирование ClubDoorman

## Обзор

Проект использует NUnit для unit-тестирования и SpecFlow для BDD тестов.

## Группы тестов

### 🚀 Быстрые тесты (Fast Tests)
- **Время выполнения**: < 1 секунды на тест
- **Назначение**: Основная функциональность, критичные компоненты
- **Запуск**: `./scripts/run-tests.sh fast` или `dotnet test --filter "TestCategory!=slow"`

**Включают:**
- MessageHandler (основная логика)
- CaptchaService (базовая функциональность)
- ModerationService (простые проверки)
- UserManager (операции с пользователями)
- Команды бота

### 🐌 Медленные тесты (Slow Tests)
- **Время выполнения**: 10-120 секунд на тест
- **Назначение**: ML модели, интеграционные тесты, тяжелые вычисления
- **Запуск**: `./scripts/run-tests.sh slow` или `dotnet test --filter "TestCategory=slow"`

**Включают:**
- SpamHamClassifier (ML модель)
- AiChecks (AI анализ)
- Интеграционные тесты с реальными API
- Тесты производительности

### 📊 Все тесты (All Tests)
- **Время выполнения**: 3-5 минут
- **Назначение**: Полная проверка системы
- **Запуск**: `./scripts/run-tests.sh all` или `dotnet test`

## Рекомендации по использованию

### Для разработки
```bash
# Быстрые тесты - для прекоммита
./scripts/run-tests.sh fast

# Или напрямую
dotnet test --filter "TestCategory!=slow"
```

### Для CI/CD
```bash
# Полная проверка перед релизом
./scripts/run-tests.sh all
```

### Для отладки ML компонентов
```bash
# Только ML тесты
dotnet test --filter "TestCategory=slow"
```

## Структура тестов

```
ClubDoorman.Test/
├── Unit/                    # Unit тесты
│   ├── Handlers/           # Тесты обработчиков
│   ├── Services/           # Тесты сервисов
│   └── Moderation/         # Тесты модерации
├── Integration/            # Интеграционные тесты
├── TestInfrastructure/     # Тестовая инфраструктура
└── TestData/              # Тестовые данные
```

## Категории тестов

- `[Category("unit")]` - Unit тесты
- `[Category("integration")]` - Интеграционные тесты
- `[Category("slow")]` - Медленные тесты (ML, AI)
- `[Category("critical")]` - Критичные компоненты
- `[Category("handlers")]` - Обработчики сообщений
- `[Category("services")]` - Сервисы
- `[Category("moderation")]` - Модерация
- `[Category("ml")]` - Machine Learning

## Настройка тестов

### Таймауты
Медленные тесты имеют таймауты:
```csharp
[Test]
[CancelAfter(10000)] // 10 секунд максимум
public async Task IsSpam_ValidMessage_ReturnsNotSpam()
```

### Мокирование
Используется Moq для мокирования зависимостей:
```csharp
var mockLogger = new Mock<ILogger<Service>>();
var service = new Service(mockLogger.Object);
```

### Тестовые данные
Тестовые данные вынесены в отдельные классы:
```csharp
var message = MessageTestData.ValidMessage();
var user = TestDataFactory.CreateValidUser();
```

## Отладка тестов

### Подробный вывод
```bash
dotnet test --verbosity detailed
```

### Запуск конкретного теста
```bash
dotnet test --filter "TestName=HandleAsync_ValidMessage_ProcessesSuccessfully"
```

### Параллельное выполнение
```bash
dotnet test --maxcpucount:4
```

## Производительность

### Статистика выполнения
- **Быстрые тесты**: ~350 тестов, < 30 секунд
- **Медленные тесты**: ~14 тестов, 2-3 минуты
- **Все тесты**: ~364 теста, 3-5 минут

### Оптимизация
1. Используйте быстрые тесты для прекоммита
2. Медленные тесты запускайте только при необходимости
3. В CI/CD используйте параллельное выполнение
4. Кэшируйте результаты ML моделей в тестах

## Troubleshooting

### Тесты зависают
- Проверьте таймауты в медленных тестах
- Убедитесь, что ML модель инициализирована
- Проверьте доступность внешних API

### Ошибки компиляции
- Восстановите зависимости: `dotnet restore`
- Очистите кэш: `dotnet clean`
- Пересоберите: `dotnet build`

### Проблемы с ML моделью
- Проверьте наличие файла `data/spam-ham.txt`
- Убедитесь, что файл `data/exclude-tokens.txt` существует
- Проверьте права доступа к файлам данных 