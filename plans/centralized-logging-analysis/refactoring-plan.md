# План рефакторинга централизованной системы логирования

## Цель
Полностью внедрить централизованную систему логирования и уведомлений, заменив все прямые вызовы на использование MessageService и UserFlowLogger.

## Этап 1: Внедрение MessageService

### 1.1 Инжекция IMessageService
**Файлы для изменения:**
- `Worker.cs` - добавить `IMessageService _messageService`
- `MessageHandler.cs` - добавить `IMessageService _messageService`
- `CallbackQueryHandler.cs` - добавить `IMessageService _messageService`
- `ModerationService.cs` - добавить `IMessageService _messageService`
- `IntroFlowService.cs` - добавить `IMessageService _messageService`
- `ChatMemberHandler.cs` - добавить `IMessageService _messageService`

### 1.2 Замена прямых вызовов на MessageService
**Worker.cs:**
- [ ] `AutoBan` - использовать `SendAdminNotificationAsync(AdminNotificationType.AutoBanBlacklist, data)`
- [ ] `AutoBanWithLogging` - использовать `SendLogNotificationAsync(LogNotificationType.AutoBanFromBlacklist, data)`
- [ ] Статистические отчеты - использовать `SendAdminNotificationAsync(AdminNotificationType.SystemError, data)`

**MessageHandler.cs:**
- [ ] `BanUserForLongName` - использовать `SendAdminNotificationAsync(AdminNotificationType.BanForLongName, data)`
- [ ] `BanBlacklistedUser` - использовать `SendAdminNotificationAsync(AdminNotificationType.AutoBanFromBlacklist, data)`
- [ ] `AutoBanChannel` - использовать `SendAdminNotificationAsync(AdminNotificationType.BanChannel, data)`
- [ ] `AutoBan` - использовать `SendAdminNotificationAsync(AdminNotificationType.AutoBanBlacklist, data)`
- [ ] `DeleteAndReportMessage` - использовать `ForwardToAdminWithNotificationAsync`
- [ ] `DontDeleteButReportMessage` - использовать `ForwardToAdminWithNotificationAsync`
- [ ] AI анализ профиля - использовать `SendAdminNotificationAsync(AdminNotificationType.AiProfileAnalysis, data)`

**CallbackQueryHandler.cs:**
- [ ] Обработка капчи - использовать `SendAdminNotificationAsync(AdminNotificationType.ModerationError, data)`
- [ ] Админские действия - использовать `SendAdminNotificationAsync(AdminNotificationType.ModerationError, data)`

**ModerationService.cs:**
- [ ] Подозрительные пользователи - использовать `SendAdminNotificationAsync(AdminNotificationType.SuspiciousUser, data)`
- [ ] AI детект - использовать `SendAdminNotificationAsync(AdminNotificationType.AiProfileAnalysis, data)`

**IntroFlowService.cs:**
- [ ] Бан за длинное имя - использовать `SendAdminNotificationAsync(AdminNotificationType.BanForLongName, data)`
- [ ] Бан из блэклиста - использовать `SendAdminNotificationAsync(AdminNotificationType.AutoBanFromBlacklist, data)`

**ChatMemberHandler.cs:**
- [ ] Попытка бана в приватном чате - использовать `SendAdminNotificationAsync(AdminNotificationType.PrivateChatBanAttempt, data)`

### 1.3 Создание недостающих типов уведомлений
**Добавить в AdminNotificationType:**
- [ ] `ChannelMessage` - сообщения от каналов
- [ ] `RemovedFromApproved` - удаление из списка одобренных

**Добавить в LogNotificationType:**
- [ ] `ChannelMessage` - сообщения от каналов

## Этап 2: Внедрение UserFlowLogger

### 2.1 Инжекция IUserFlowLogger
**Файлы для изменения:**
- `Worker.cs` - добавить `IUserFlowLogger _userFlowLogger`
- `IntroFlowService.cs` - добавить `IUserFlowLogger _userFlowLogger`
- `ApprovedUsersStorage.cs` - добавить `IUserFlowLogger _userFlowLogger`
- `SuspiciousUsersStorage.cs` - добавить `IUserFlowLogger _userFlowLogger`
- `CaptchaService.cs` - добавить `IUserFlowLogger _userFlowLogger`
- `ModerationService.cs` - добавить `IUserFlowLogger _userFlowLogger`
- `CallbackQueryHandler.cs` - добавить `IUserFlowLogger _userFlowLogger`

### 2.2 Добавление недостающих методов в UserFlowLogger
**Добавить в IUserFlowLogger:**
- [ ] `LogUserRemovedFromApproved(User user, Chat chat, string reason)`
- [ ] `LogUserAddedToApproved(User user, Chat chat, string reason)`
- [ ] `LogUserMarkedAsSuspicious(User user, Chat chat, double mimicryScore, List<string> firstMessages)`
- [ ] `LogUserRemovedFromSuspicious(User user, Chat chat, string reason)`
- [ ] `LogAiProfileAnalysis(User user, Chat chat, double spamProbability, string reason)`
- [ ] `LogChannelMessage(Chat senderChat, Chat targetChat, string messageText)`
- [ ] `LogSystemError(Exception exception, string context, User? user = null, Chat? chat = null)`

### 2.3 Замена прямого логирования
**Worker.cs:**
- [ ] Автобан по блэклисту - использовать `LogUserBanned`

**IntroFlowService.cs:**
- [ ] Бан пользователей - использовать `LogUserBanned`

**ApprovedUsersStorage.cs:**
- [ ] Добавление в одобренные - использовать `LogUserAddedToApproved`
- [ ] Удаление из одобренных - использовать `LogUserRemovedFromApproved`

**SuspiciousUsersStorage.cs:**
- [ ] Добавление в подозрительные - использовать `LogUserMarkedAsSuspicious`
- [ ] Удаление из подозрительных - использовать `LogUserRemovedFromSuspicious`

**CaptchaService.cs:**
- [ ] Результаты капчи - использовать `LogCaptchaPassed`/`LogCaptchaFailed`

**ModerationService.cs:**
- [ ] Результаты модерации - использовать `LogModerationResult`
- [ ] AI анализ - использовать `LogAiProfileAnalysis`

**CallbackQueryHandler.cs:**
- [ ] Результаты капчи - использовать `LogCaptchaPassed`/`LogCaptchaFailed`
- [ ] Админские действия - использовать `LogUserApproved`/`LogUserBanned`

## Этап 3: Тестирование и проверка

### 3.1 Проверка покрытия
- [ ] Убедиться, что все уведомления отправляются через MessageService
- [ ] Убедиться, что все пользовательские события логируются через UserFlowLogger
- [ ] Проверить, что нет дублирования логики

### 3.2 Тестирование
- [ ] Создать тесты для MessageService
- [ ] Создать тесты для UserFlowLogger
- [ ] Проверить работу всех типов уведомлений

## Статус: План создан, готов к выполнению 