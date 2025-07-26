# Issue: [E2E тестирование прав доступа и тихого режима](https://github.com/momai/ClubDoorman/issues/63)

## Описание
Создание E2E тестов для прав доступа и тихого режима с использованием моков FakeTelegram.

## Основные сценарии
- Работа с правами администратора
- Тихий режим (без прав администратора)
- Отключение/включение капчи по ID чата
- Удаление сообщений при входе без капчи
- Проверка прав в разных типах чатов

## Затрагиваемые файлы
- `ClubDoorman/Services/BotPermissionsService.cs`
- `ClubDoorman/Services/AppConfig.cs`
- `ClubDoorman/Infrastructure/Config.cs`
- `ClubDoorman.Test/Integration/PermissionsTests.cs`

## Технические требования
- Использование существующих моков `MockTelegram`
- Тестирование разных типов чатов
- Валидация альтернативных методов модерации

## Приоритет: Средний
## Оценка: 1 день 