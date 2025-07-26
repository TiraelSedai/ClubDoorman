# Необходимые изменения в коде проекта

## Для тестирования приватных методов Worker

### Проблема
- Методы `FullName`, `UserToKey`, `AdminDisplayName` в Worker.cs приватные
- Нельзя тестировать напрямую через reflection (плохая практика)

### Решение
Вынести в отдельный сервис/хелпер:

```csharp
// Создать новый файл: ClubDoorman/Services/UserDisplayService.cs
public interface IUserDisplayService
{
    string GetFullName(string firstName, string? lastName);
    string GetUserKey(long chatId, User user);
    string GetAdminDisplayName(User user);
}

public class UserDisplayService : IUserDisplayService
{
    public string GetFullName(string firstName, string? lastName)
    {
        // Перенести логику из Worker.FullName
    }
    
    public string GetUserKey(long chatId, User user)
    {
        // Перенести логику из Worker.UserToKey
    }
    
    public string GetAdminDisplayName(User user)
    {
        // Перенести логику из Worker.AdminDisplayName
    }
}
```

### Изменения в Worker.cs
- Удалить статические методы
- Добавить зависимость на IUserDisplayService
- Использовать сервис вместо статических методов

## Для улучшения тестируемости

### 1. Добавить InternalsVisibleTo для тестов
```csharp
// В ClubDoorman/AssemblyInfo.cs или .csproj
[assembly: InternalsVisibleTo("ClubDoorman.Test")]
```

### 2. Улучшить интерфейсы
- ITelegramBotClientWrapper - добавить недостающие методы
- IAiChecks - проверить полноту интерфейса

### 3. Создать TestDoubles вместо моков
- FakeTelegramClient (уже есть)
- FakeAiService
- FakeUserManager

## Приоритет изменений
1. **Высокий**: UserDisplayService - для тестирования Worker
2. **Средний**: InternalsVisibleTo - для доступа к internal методам
3. **Низкий**: Улучшение интерфейсов

## Пропущенные сервисы из-за необходимости изменений

### MessageService
**Проблема**: Сложная архитектура с множественными зависимостями, интерфейсы не полностью соответствуют реализации
**Статус**: Пропущен - требует рефакторинга интерфейсов и улучшения архитектуры
**Файлы**: `ClubDoorman/Services/MessageService.cs`, `ClubDoorman/Services/IMessageService.cs`

### IntroFlowService  
**Проблема**: Зависит от GlobalStatsManager (конкретный класс), который сложно мокать
**Статус**: Пропущен - требует создания интерфейса для GlobalStatsManager
**Файлы**: `ClubDoorman/Services/IntroFlowService.cs`

### MessageTemplates
**Проблема**: Отсутствует интерфейс, сложно тестировать без рефакторинга
**Статус**: Пропущен - требует создания интерфейса
**Файлы**: `ClubDoorman/Services/MessageTemplates.cs`

### LoggingConfigurationService
**Проблема**: Работает с файловой системой, требует интеграционных тестов
**Статус**: Пропущен - требует создания тестового окружения для файлов
**Файлы**: `ClubDoorman/Services/LoggingConfigurationService.cs`

### UserFlowLogger
**Проблема**: Сложные зависимости, требует рефакторинга для тестируемости
**Статус**: Пропущен - требует улучшения архитектуры
**Файлы**: `ClubDoorman/Services/UserFlowLogger.cs`

## Примечание
Эти изменения НЕ делаем сейчас. Сначала завершаем тестирование с текущей архитектурой. 