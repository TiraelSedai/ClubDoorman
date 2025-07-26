# Issue: Добавление уровней verbosity для консольного вывода и debug для логов

## Описание
Необходимо добавить настраиваемые уровни verbosity для консольного вывода и уровни debug для логов, чтобы улучшить отладку и мониторинг системы.

## Затрагиваемые файлы 

### Основные файлы конфигурации:
- `ClubDoorman/Infrastructure/Config.cs` - добавление переменных окружения для уровней verbosity
- `ClubDoorman/Services/AppConfig.cs` - добавление свойств для доступа к настройкам verbosity
- `ClubDoorman/Services/IAppConfig.cs` - добавление интерфейса для настроек verbosity

### Файлы логирования:
- `ClubDoorman/Program.cs` - настройка уровней логирования
- `ClubDoorman/Worker.cs` - применение уровней verbosity для консольного вывода

### Сервисы с консольным выводом:
- `ClubDoorman/Services/UserManager.cs` - замена Console.WriteLine на структурированное логирование
- `ClubDoorman/Services/AiChecks.cs` - добавление debug логов
- `ClubDoorman/Services/ModerationService.cs` - добавление debug логов
- `ClubDoorman/Services/CaptchaService.cs` - добавление debug логов

## Переменные окружения для добавления:
- `DOORMAN_CONSOLE_VERBOSITY` - уровень детализации консольного вывода (quiet, normal, verbose, debug)
- `DOORMAN_LOG_LEVEL` - уровень логирования (Information, Warning, Error, Debug, Trace)

## Требования:
1. Заменить все `Console.WriteLine` на структурированное логирование
2. Добавить debug логи в ключевые методы сервисов
3. Настроить фильтрацию консольного вывода по уровню verbosity
4. Сохранить обратную совместимость (по умолчанию - normal verbosity)
5. Добавить логирование в JSON формате для production

## Приоритет: Средний
## Оценка: 1-2 дня 