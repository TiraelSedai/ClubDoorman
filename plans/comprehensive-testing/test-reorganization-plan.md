# План реорганизации структуры тестов

## Текущие проблемы
1. **Хаотичная структура** - тесты разбросаны по разным папкам
2. **Дублирование** - одинаковые тесты в разных местах
3. **Нет четкой категоризации** - сложно найти нужные тесты
4. **Смешение типов** - unit, integration, e2e в одной папке

## Новая структура

```
ClubDoorman.Test/
├── Unit/                          # Unit тесты (быстрые, изолированные)
│   ├── Handlers/                  # Тесты обработчиков
│   │   ├── CallbackQueryHandlerTests.cs ✅
│   │   ├── MessageHandlerTests.cs
│   │   └── ChatMemberHandlerTests.cs
│   ├── Services/                  # Тесты сервисов
│   │   ├── AiChecksTests.cs
│   │   ├── ModerationServiceTests.cs
│   │   ├── CaptchaServiceTests.cs
│   │   ├── UserManagerTests.cs
│   │   └── BotPermissionsServiceTests.cs
│   ├── Infrastructure/           # Тесты инфраструктуры
│   │   ├── WorkerTests.cs ✅
│   │   └── ConfigTests.cs
│   └── EdgeCases/                # Edge-case тесты
│       ├── ErrorHandlingTests.cs
│       ├── TimeoutTests.cs
│       └── FaultInjectionTests.cs
├── Integration/                   # Integration тесты (с FakeTelegramClient)
│   ├── MessageFlowTests.cs
│   ├── CallbackFlowTests.cs
│   ├── CaptchaFlowTests.cs
│   └── UserApprovalFlowTests.cs
├── E2E/                          # End-to-End тесты (с реальным API)
│   ├── CompleteWorkflowTests.cs
│   ├── AdminActionsTests.cs
│   └── SpamDetectionTests.cs
├── TestInfrastructure/           # Инфраструктура для тестов
│   ├── Factories/                # TestFactories
│   │   ├── MockAiChecksFactory.cs ✅
│   │   ├── MockTelegramClientFactory.cs
│   │   └── TestDataFactory.cs
│   ├── Mocks/                    # Моки и стабы
│   │   ├── FakeTelegramClient.cs
│   │   └── MockServices.cs
│   └── Helpers/                  # Вспомогательные классы
│       ├── TestBase.cs
│       ├── TestTimeoutHelper.cs
│       └── TestData.cs
└── TestData/                     # Тестовые данные
    ├── SampleMessages.cs
    ├── TestScenarios.cs
    └── ExpectedResults.cs
```

## План миграции

### Этап 1: Создание новой структуры ✅
- [x] Создать папки Unit/, Integration/, E2E/
- [x] Создать подпапки в Unit/
- [x] Переместить существующие тесты

### Этап 2: Миграция существующих тестов
- [ ] Переместить CallbackQueryHandlerTests.cs в Unit/Handlers/ ✅
- [ ] Переместить WorkerTests.cs в Unit/Infrastructure/ ✅
- [ ] Переместить ModerationServiceTests.cs в Unit/Services/
- [ ] Переместить ErrorHandlingTests.cs в Unit/EdgeCases/
- [ ] Переместить Integration тесты в Integration/
- [ ] Удалить дублирующиеся тесты

### Этап 3: Создание недостающих фабрик
- [ ] MockTelegramClientFactory.cs
- [ ] TestDataFactory.cs
- [ ] MockServices.cs

### Этап 4: Категоризация тестов
- [ ] Добавить атрибуты [Category] ко всем тестам
- [ ] Создать конфигурацию для разных типов тестов
- [ ] Настроить CI/CD для разных категорий

### Этап 5: Документация
- [ ] README для каждой папки
- [ ] Документация по запуску тестов
- [ ] Руководство по написанию тестов

## Категории тестов

### Unit тесты [Category("unit")]
- **fast** - выполняются быстро (< 100ms)
- **critical** - критически важные компоненты
- **uses:service** - использует конкретный сервис

### Integration тесты [Category("integration")]
- **medium** - средняя скорость выполнения
- **uses:telegram** - использует FakeTelegramClient
- **uses:database** - использует тестовую БД

### E2E тесты [Category("e2e")]
- **slow** - медленные тесты
- **uses:ai** - использует AI API
- **uses:real-telegram** - использует реальный Telegram API

### Edge-case тесты [Category("edge-case")]
- **fault-injection** - тесты с инъекцией ошибок
- **timeout** - тесты таймаутов
- **error-handling** - тесты обработки ошибок

## Команды для запуска

```bash
# Все unit тесты
dotnet test --filter "Category=unit"

# Только быстрые unit тесты
dotnet test --filter "Category=fast"

# Критические компоненты
dotnet test --filter "Category=critical"

# Integration тесты
dotnet test --filter "Category=integration"

# E2E тесты (только в CI)
dotnet test --filter "Category=e2e"

# Edge-case тесты
dotnet test --filter "Category=edge-case"
```

## Следующие шаги
1. Начать с миграции существующих тестов
2. Создать недостающие фабрики
3. Добавить категоризацию
4. Настроить CI/CD
5. Создать документацию 