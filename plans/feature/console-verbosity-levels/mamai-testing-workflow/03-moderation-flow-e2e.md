# Issue: [E2E тестирование системы модерации сообщений](https://github.com/momai/ClubDoorman/issues/62)

## Описание
Создание E2E тестов для системы модерации с проверкой порядка операций в логах.

## Основные сценарии
- Порядок проверок: lols.bot → длинные имена → капча → стоп-слова → ML → AI
- Стандартные и спам-сообщения
- Обучение ML модели (/spam, /ham)
- Форварды сообщений
- Длинные имена пользователей

## Затрагиваемые файлы
- `ClubDoorman/Services/ModerationService.cs`
- `ClubDoorman/Services/SpamHamClassifier.cs`
- `ClubDoorman/Services/SimpleFilters.cs`
- `ClubDoorman/Handlers/Commands/SuspiciousCommandHandler.cs`
- `ClubDoorman.Test/Integration/ModerationFlowTests.cs`

## Технические требования
- Использование существующих моков `MockTelegram`
- Проверка строгого порядка операций в логах
- Моки для ML и AI сервисов

## Приоритет: Высокий
## Оценка: 1 день 