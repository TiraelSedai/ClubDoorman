# Эпик: Полный воркфлоу тестирования Мамая

## Описание
E2E и интеграционные тесты с использованием существующих моков FakeTelegram для полного воркфлоу Мамая.

## Разбивка на Issues

### 1. [E2E тестирование системы капчи](https://github.com/momai/ClubDoorman/issues/60) - [План](./01-captcha-system-e2e.md)
**Приоритет:** Высокий | **Оценка:** 1 день
- Таймауты капчи, неверные/верные ответы
- Приветствия после капчи, тихий режим

### 2. [E2E тестирование AI анализа профилей](https://github.com/momai/ClubDoorman/issues/61) - [План](./02-ai-profile-analysis-e2e.md)
**Приоритет:** Высокий | **Оценка:** 1 день
- AI анализ при первом сообщении
- Кнопки админа (🥰 свой, 🤖 бан, 😶 пропуск)

### 3. [E2E тестирование системы модерации](https://github.com/momai/ClubDoorman/issues/62) - [План](./03-moderation-flow-e2e.md)
**Приоритет:** Высокий | **Оценка:** 1 день
- Порядок проверок в логах
- Спам-детекция, обучение ML

### 4. [E2E тестирование статистики и команд](https://github.com/momai/ClubDoorman/issues/65) - [План](./04-statistics-and-commands-e2e.md)
**Приоритет:** Средний | **Оценка:** 1 день
- Команды /stat, /spam, /ham
- Автоматическая статистика

### 5. [E2E тестирование прав доступа и тихого режима](https://github.com/momai/ClubDoorman/issues/63) - [План](./05-permissions-and-quiet-mode-e2e.md)
**Приоритет:** Средний | **Оценка:** 1 день
- Права администратора, тихий режим
- Отключение/включение капчи по ID

### 6. [Расширение инфраструктуры тестов](https://github.com/momai/ClubDoorman/issues/64) - [План](./06-integration-test-framework.md)
**Приоритет:** Высокий | **Оценка:** 2 дня
- Расширение FakeTelegramClient
- Дополнительные Step Definitions
- Тестовые данные для E2E сценариев

## Общая оценка: 7 дней

## Ревью тестов AI анализа

### ✅ **Тесты с реальным AI (оставляем):**
1. **E2E_AI_Analysis_WithRealApi_ShouldWork** - базовый тест реального API
2. **E2E_AI_Analysis_WithRealPhoto_ShouldDetectHighSpamProbability** - **ГЛАВНЫЙ ТЕСТ** с реальным фото профиля
3. **E2E_AI_Analysis_SpecificUserDnekxpb_ShouldDetectSuspiciousProfile** - тест конкретного пользователя
4. **E2E_AI_Analysis_VerySuspiciousUser_ShouldDetectHighSpamProbability** - тест явно подозрительного профиля

### 🔧 **Тесты для исправления (BDD с моками):**
1. **E2E_AI_Analysis_FirstMessage_ShouldTriggerAnalysis** - базовый flow
2. **E2E_AI_Analysis_MessageHandler_ShouldSendNotification** - отправка уведомлений
3. **E2E_AI_Analysis_AdminButton_Own_ShouldApproveUser** - кнопка одобрения
4. **E2E_AI_Analysis_AdminButton_Ban_ShouldBanUser** - кнопка бана
5. **E2E_AI_Analysis_AdminButton_Skip_ShouldSkipUser** - кнопка пропуска
6. **E2E_AI_Analysis_Channel_ShouldNotShowCaptcha** - работа в каналах
7. **E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis** - повторные сообщения
8. **E2E_AI_Analysis_OperationOrder_ShouldBeCorrect** - порядок операций
9. **E2E_AI_Analysis_PhotoWithCaption_ShouldIncludePhoto** - работа с фото

### 📋 **BDD тесты (полностью фейковые):**
- Все сценарии в `AiAnalysis.feature` должны использовать моки
- Step Definitions должны работать с `FakeTelegramClient`
- Никаких реальных API вызовов

## Текущий прогресс
- **[Issue #64 (Инфраструктура)](https://github.com/momai/ClubDoorman/issues/64)** - 100% завершено ✅
  - FakeTelegramClient расширен
  - Step Definitions реализованы
  - Feature файлы переведены на английский
  - Все тесты проходят (616 успешно, 0 сбоев)

- **[Issue #61 (AI анализ)](https://github.com/momai/ClubDoorman/issues/61)** - 100% завершено ✅
  - Создана инфраструктура E2E тестов (8 тестов)
  - Обновлены Step Definitions
  - Исправлены ошибки компиляции
  - Добавлена поддержка .env файлов и GitHub secrets
  - Создан GitHub Actions workflow для E2E тестов
  - **Исправлена логика тестов** - корректная обработка API ошибок
  - **Подтверждена работа .env файлов** - API ключи загружаются успешно
  - **ИСПРАВЛЕНА ПРОБЛЕМА С API КЛЮЧАМИ** - тесты теперь используют реальную конфигурацию из .env
  - **ПОДТВЕРЖДЕНО РАБОТАЕТ В ПРОДЕ** - AI анализ функционирует корректно
  - **ДОБАВЛЕНЫ ТЕСТЫ КОНКРЕТНЫХ ПРОФИЛЕЙ** - протестированы @Dnekxpb и @premium_crypto_2024
  - **СОЗДАНА ИНФРАСТРУКТУРА ТЕСТИРОВАНИЯ С РЕАЛЬНЫМИ ФОТО** - добавлены тестовые изображения
  - **ПОДТВЕРЖДЕНА РАБОТА AI АНАЛИЗА С ФОТО** - профиль @Dnekxpb получил 90% вероятность спама
  - **ЗАВЕРШЕН РЕФАКТОРИНГ ТЕСТОВ** - созданы FakeCaptchaService, FakeCallbackQueryHandler, FakeModerationService, FakeServicesFactory
  - **ВЫНЕСЕНЫ ФЕЙКИ ИЗ ТЕСТОВ** - все моки теперь в отдельных классах с централизованной фабрикой
  - Тесты запускаются (4 успешно с реальным API, остальные используют моки для стабильности)

## Порядок выполнения
1. **[Инфраструктура](https://github.com/momai/ClubDoorman/issues/64)** - основа ✅ 100%
2. **[AI анализ](https://github.com/momai/ClubDoorman/issues/61)** - критическая функциональность ✅ 85%
3. **[Капча](https://github.com/momai/ClubDoorman/issues/60)** - базовая функциональность
4. **[Модерация](https://github.com/momai/ClubDoorman/issues/62)** - основная логика
5. **[Права доступа](https://github.com/momai/ClubDoorman/issues/63)** - системные настройки
6. **[Статистика](https://github.com/momai/ClubDoorman/issues/65)** - дополнительная функциональность

## Ключевые особенности
- Использование существующих моков `MockTelegram`
- Gherkin сценарии в комментариях к issues
- Переиспользование step definitions
- Проверка порядка операций в логах

## Зависимости
- Существующая инфраструктура тестов
- Моки для AI и ML сервисов 