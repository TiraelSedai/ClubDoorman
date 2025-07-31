# Структура тестов (рекомендуемая)

## Текущая структура → Новая структура

```
ClubDoorman.Test/
├── Unit/                    # Быстрые unit тесты
│   ├── Services/           # Тесты сервисов
│   │   ├── AiChecks/       # AI логика
│   │   ├── Moderation/     # Модерация
│   │   ├── Captcha/        # Капча
│   │   └── Callback/       # Callback обработчики
│   ├── Handlers/           # Тесты хендлеров
│   └── Utils/              # Утилиты
├── Integration/            # Интеграционные тесты
│   ├── Moderation/         # Полная модерация
│   ├── Captcha/            # Капча + Telegram
│   └── Callback/           # Callback + Telegram
├── E2E/                    # End-to-end тесты
│   ├── Scenarios/          # Бизнес-сценарии
│   ├── TelegramSim/        # С FakeTelegramClient
│   └── AiReal/             # С реальными AI сервисами
├── EdgeCases/              # Граничные случаи
│   ├── AiFailures/         # AI недоступен
│   ├── TelegramErrors/     # Telegram API ошибки
│   ├── RaceConditions/     # Race conditions
│   └── InconsistentState/  # Неконсистентные состояния
├── Fixtures/               # Тестовые данные
│   ├── AiResponses/        # Контракты AI ответов
│   ├── TelegramUpdates/    # Telegram обновления
│   └── TestData/           # Общие тестовые данные
└── TestInfrastructure/     # Инфраструктура (оставить как есть)
```

## Категоризация тестов

### По типу
```csharp
[Category("fast")]      // Unit тесты, < 30 сек
[Category("integration")] // Интеграционные, < 2 мин
[Category("e2e")]        // E2E, < 15 мин
[Category("edge")]       // Edge-case, < 5 мин
```

### По используемым сервисам
```csharp
[Category("uses:ai")]        // Использует AI сервисы
[Category("uses:telegram")]  // Использует Telegram API
[Category("uses:captcha")]   // Использует капчу
[Category("uses:callback")]  // Использует callback'ы
```

### По критичности
```csharp
[Category("critical")]   // Критичная бизнес-логика
[Category("important")]  // Важная функциональность
[Category("nice-to-have")] // Дополнительная функциональность
```

## Контракты AI ответов

### Структура контракта
```json
{
  "photo_analysis": {
    "result": "suspicious|normal|erotic",
    "confidence": 0.98,
    "reason": "Photo contains suspicious content"
  },
  "text_analysis": {
    "result": "spam|ham|attention_bait",
    "confidence": 0.85,
    "reason": "Contains spam keywords"
  },
  "user_analysis": {
    "result": "suspicious|normal",
    "confidence": 0.92,
    "reason": "Suspicious username pattern"
  }
}
```

### Файлы контрактов
```
Fixtures/AiResponses/
├── spam_scenarios.json
├── normal_scenarios.json
├── suspicious_user_scenarios.json
└── edge_cases.json
```

## План миграции

### Этап 1: Создание новой структуры
- [ ] Создать новые папки
- [ ] Переместить существующие тесты
- [ ] Обновить namespace'ы

### Этап 2: Добавление категорий
- [ ] Добавить категории к существующим тестам
- [ ] Создать контракты AI ответов
- [ ] Обновить CI конфигурацию

### Этап 3: Создание edge-case тестов
- [ ] Тесты AI failures
- [ ] Тесты Telegram API ошибок
- [ ] Тесты race conditions

### Этап 4: Оптимизация
- [ ] Анализ времени выполнения
- [ ] Параллелизация тестов
- [ ] Кэширование фикстур

## Метрики качества

### Покрытие
- Unit тесты: > 80%
- Интеграционные: > 70%
- E2E: > 50%
- Edge-case: > 60%

### Время выполнения
- Unit: < 30 сек
- Integration: < 2 мин
- E2E: < 15 мин
- Edge-case: < 5 мин

### Стабильность
- Flaky rate: < 5%
- Mutation score: > 80% (для core logic)
- Retry success rate: > 95% 