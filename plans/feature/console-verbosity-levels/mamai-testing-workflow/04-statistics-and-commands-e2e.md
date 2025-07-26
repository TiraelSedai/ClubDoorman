# Issue: [E2E тестирование статистики и команд](https://github.com/momai/ClubDoorman/issues/65)

## Описание
Создание E2E тестов для статистики и команд с использованием моков FakeTelegram.

## Основные сценарии
- Команда /stat (статистика чата)
- Автоматическая статистика (ежедневные отчеты)
- Команды /spam и /ham (обучение ML)
- Права доступа к командам
- Статистика в тихом режиме

## Затрагиваемые файлы
- `ClubDoorman/Services/StatisticsService.cs`
- `ClubDoorman/Services/GlobalStatsManager.cs`
- `ClubDoorman/Handlers/Commands/SuspiciousCommandHandler.cs`
- `ClubDoorman/Models/ChatStats.cs`
- `ClubDoorman.Test/Integration/StatisticsTests.cs`

## Технические требования
- Использование существующих моков `MockTelegram`
- Проверка форматирования статистики
- Валидация прав доступа

## Приоритет: Средний
## Оценка: 1 день 