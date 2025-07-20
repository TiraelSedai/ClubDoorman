# BDD Scenarios - Technical Roadmap

Эта папка содержит Gherkin сценарии, которые служат **техническим roadmap** для разработки.

## 📋 Содержание

### Активные сценарии (готовы к автоматизации)
- `MessageModeration.feature` - Модерация сообщений
- `ErrorHandling.feature` - Обработка ошибок
- `UserApproval.feature` - Система одобрения пользователей
- `UserNameModeration.feature` - Модерация имен пользователей

### Документация сценариев
- `CompleteSystem.feature.ru` - Полная система
- `SuspiciousUserSystem.feature.ru` - Система подозрительных пользователей
- `UserNotifications.feature.ru` - Уведомления пользователей
- `CaptchaSystem.feature.ru` - Система капчи

## 🎯 План использования

1. **Сейчас**: Используем как документацию кейсов для TDD
2. **Потом**: Автоматизируем через SpecFlow когда техчасть будет готова

## 📁 Структура

```
docs/scenarios/
├── README.md                    # Этот файл
├── *.feature                    # Gherkin сценарии (EN)
├── *.feature.ru                 # Gherkin сценарии (RU)
└── *.feature.cs                 # Автогенерированные SpecFlow классы
```

## 🔄 Когда вернуться к BDD

- ✅ Стабильная архитектура
- ✅ Готовые моки (Telegram, AI, Storage)
- ✅ Покрытие unit-тестами
- ✅ Вылизанная ModerationService

Тогда останется только формализовать бизнес-логику через StepDefinitions. 