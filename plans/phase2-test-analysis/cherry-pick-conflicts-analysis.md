# Анализ конфликтов при Cherry-Pick оставшихся коммитов

## Попытка cherry-pick коммита 47f832d (Chat settings migration)

```bash
git cherry-pick 47f832d
```

### Ожидаемые конфликты:
- `ClubDoorman/Infrastructure/Config.cs` - изменения в статической конфигурации
- `ClubDoorman/Program.cs` - изменения в DI контейнере
- `ClubDoorman/Services/AppConfig.cs` - новая архитектура конфигурации

## Попытка cherry-pick коммита e1206ea (Critical config fixes)

```bash
git cherry-pick e1206ea
```

### Ожидаемые конфликты:
- `ClubDoorman/Infrastructure/Config.cs` - критические исправления
- `ClubDoorman/Program.cs` - изменения в DI
- `ClubDoorman/Services/AppConfig.cs` - архитектурные изменения

## Попытка cherry-pick коммита 4d8d5db (API duplication removal)

```bash
git cherry-pick 4d8d5db
```

### Ожидаемые конфликты:
- `ClubDoorman/Services/IAiChecks.cs` - удаление дублирования API
- `ClubDoorman/Services/AiChecks.cs` - рефакторинг методов
- `ClubDoorman/Services/IMessageService.cs` - изменения интерфейса
- `ClubDoorman/Handlers/MessageHandler.cs` - изменения в обработчике

## Попытка cherry-pick коммита b700878 (Captcha/Message service refactor)

```bash
git cherry-pick b700878
```

### Ожидаемые конфликты:
- `ClubDoorman/Services/CaptchaService.cs` - рефакторинг сервиса
- `ClubDoorman/Services/MessageService.cs` - рефакторинг сервиса
- `ClubDoorman/Services/ICaptchaService.cs` - изменения интерфейса
- `ClubDoorman/Handlers/MessageHandler.cs` - изменения в обработчике

## Попытка cherry-pick коммита ac64b22 (DI Config migration)

```bash
git cherry-pick ac64b22
```

### Ожидаемые конфликты:
- `ClubDoorman/Infrastructure/Config.cs` - миграция на DI
- `ClubDoorman/Program.cs` - изменения в DI контейнере
- `ClubDoorman/Services/AppConfig.cs` - новая архитектура

## Выводы

Все оставшиеся коммиты затрагивают боевую логику и вызовут конфликты при cherry-pick. Рекомендуется НЕ переносить их для сохранения стабильности cherry-pick ветки. 