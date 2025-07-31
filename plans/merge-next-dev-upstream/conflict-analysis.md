# Анализ конфликтов мержа с upstream/next-dev

## Файлы с конфликтами

### 1. .gitleaks.baseline.json
- **Статус**: Удален в upstream, изменен в HEAD
- **Решение**: Сохранить вашу версию (ваша архитектура безопасности)

### 2. ClubDoorman.Test/Integration/AiChecksPhotoLoggingTest.cs
- **Тип**: Оба изменены
- **Анализ**: Нужно изучить различия

### 3. ClubDoorman/Handlers/CallbackQueryHandler.cs
- **Тип**: Оба изменены
- **Анализ**: Нужно изучить различия

### 4. ClubDoorman/Services/IMessageService.cs
- **Тип**: Оба изменены
- **Анализ**: Нужно изучить различия

### 5. ClubDoorman/Services/IntroFlowService.cs
- **Тип**: Оба изменены
- **Анализ**: Нужно изучить различия

### 6. ClubDoorman/Services/MessageService.cs
- **Тип**: Оба изменены
- **Анализ**: Нужно изучить различия

## Файлы, которые будут изменены (без конфликтов)

### Удаляемые файлы (ваша архитектура):
- `.gitleaks.toml` - заменен на .env.sample
- `ClubDoorman.Test/Integration/EnvironmentTest.cs`
- `ClubDoorman.Test/TestInfrastructure/MockAiChecksFactory.cs`
- `ClubDoorman.Test/TestInfrastructure/MockAiChecksFactoryTests.cs`
- `ClubDoorman.Test/TestInfrastructure/ServiceChatDispatcherTestFactory.cs`
- `ClubDoorman.Test/Unit/Handlers/CallbackQueryHandlerTests.cs`
- `ClubDoorman.Test/Unit/Infrastructure/WorkerTests.cs`
- `ClubDoorman.Test/Unit/Services/BotPermissionsServiceTests.cs`
- `ClubDoorman.Test/Unit/Services/ServiceChatDispatcherTests.cs`
- `ClubDoorman.Test/Unit/Services/UpdateDispatcherTests.cs`
- `docs/GITLEAKS_FIX.md`
- `docs/GITLEAKS_SETUP.md`
- `run_with_real_token.sh.example`
- `scripts/run_e2e_tests.sh`
- `scripts/run_integration_tests.sh`

### Новые файлы (поведение Мамая):
- `ClubDoorman.Test/TestInfrastructure/IntroFlowServiceTestFactory.cs`
- `ClubDoorman.Test/TestInfrastructure/IntroFlowServiceTestFactoryTests.cs`
- `run_with_real_token.sh`
- `test_bot.sh`

### Изменяемые файлы:
- `.env.sample`
- `CHANGELOG.md`
- `ClubDoorman.Test/Properties/launchSettings.json`
- `ClubDoorman/Handlers/MessageHandler.cs`
- `ClubDoorman/Infrastructure/Config.cs`
- `ClubDoorman/Services/AiChecks.cs`
- `ClubDoorman/Services/CaptchaService.cs`
- `ClubDoorman/Services/IAiChecks.cs`
- `ClubDoorman/Services/MessageTemplates.cs`
- `ClubDoorman/Services/ServiceChatDispatcher.cs`
- `ClubDoorman/env.example`
- `test_bot.sh.example`

## Стратегия разрешения

### Приоритет 1: Сохранить архитектуру
- `.gitleaks.baseline.json` - сохранить
- Тестовые файлы - сохранить вашу инфраструктуру

### Приоритет 2: Принять поведение Мамая
- Бизнес-логика в сервисах
- Обработчики сообщений

### Приоритет 3: Гибридный подход
- Конфигурация - сохранить вашу структуру
- Функциональность - принять изменения Мамая 