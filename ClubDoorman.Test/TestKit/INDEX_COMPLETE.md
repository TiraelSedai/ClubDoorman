# TestKit Complete Index - Auto-Generated

**Generated:** 2025-08-01 12:51:30
**Components:** 29
**Methods:** 351
**Tags:** 22

## 📚 Components by Category

### 📁 AUTOFIXTURE

#### 📄 TestKit.AutoFixture.cs
*Расширение TestKit с AutoFixture для автоматического создания тестовых объектов*
**Lines:** 167 | **Methods:** 11

**Methods:**
- `CreateCaptchaService()` → `ICaptchaService` [static]
  - Создает CaptchaService с автозависимостями <tags>autofixture, captcha-service, dependencies, test-infrastructure</tags>
  - Tags: `captcha`, `factory`
- `CreateFixture()` → `IFixture` [static]
  - Расширение TestKit с AutoFixture для автоматического создания тестовых объектов <tags>autofixture, auto-generation, dependencies, test-infrastructure</tags>
  - Tags: `autofixture`, `factory`
- `CreateManyMessages()` → `List<Telegram.Bot.Types.Message>` [static]
  - Создает CallbackQueryUpdate <tags>autofixture, callback-query-update, telegram, test-infrastructure</tags>
  - Tags: `collection`, `factory`, `message`
- `CreateManySpamMessages()` → `List<Telegram.Bot.Types.Message>` [static]
  - Создает много пользователей <tags>autofixture, many-users, collection, test-infrastructure</tags>
  - Tags: `collection`, `factory`, `message`, `spam`
- `CreateManyUsers()` → `List<Telegram.Bot.Types.User>` [static]
  - Создает много сообщений <tags>autofixture, many-messages, collection, test-infrastructure</tags>
  - Tags: `collection`, `factory`, `user`
- `CreateMessageHandler()` → `MessageHandler` [static]
  - Заполняет свойства существующего объекта <tags>autofixture, populate, fill-properties, test-infrastructure</tags>
  - Tags: `factory`, `message`
- `CreateModerationService()` → `ModerationService` [static]
  - Создает MessageHandler с автозависимостями <tags>autofixture, message-handler, dependencies, test-infrastructure</tags>
  - Tags: `factory`, `moderation`
- `CreateRealisticMessages()` → `List<Telegram.Bot.Types.Message>` [static]
  - Создает список реалистичных пользователей <tags>autofixture, users, realistic, test-infrastructure</tags>
  - Tags: `bogus`, `collection`, `factory`, `message`
- `CreateRealisticUsers()` → `List<Telegram.Bot.Types.User>` [static]
  - Создает ModerationService с автозависимостями <tags>autofixture, moderation-service, dependencies, test-infrastructure</tags>
  - Tags: `bogus`, `collection`, `factory`, `user`
- `CreateUserManager()` → `IUserManager` [static]
  - Создает CaptchaService с автозависимостями <tags>autofixture, captcha-service, dependencies, test-infrastructure</tags>
  - Tags: `factory`, `user`
- `GetFixture()` → `IFixture` [static]
  - Создает настроенный AutoFixture с кастомизациями для Telegram-типов <tags>autofixture, customization, telegram, test-infrastructure</tags>
  - Tags: `autofixture`

### 📁 BOGUS

#### 📄 TestKit.Bogus.cs
*Расширение TestKit с Bogus для генерации реалистичных тестовых данных*
**Lines:** 146 | **Methods:** 18

**Methods:**
- `CreateConversation()` → `List<Message>` [static]
  - Создает список случайных пользователей <tags>bogus, users, collection, faker</tags>
  - Tags: `collection`, `factory`, `message`
- `CreateMediaMessage()` → `Message` [static]
  - Создает спам-сообщение <tags>bogus, spam-message, realistic, faker</tags>
  - Tags: `factory`, `message`
- `CreateRandomDate()` → `DateTime` [static]
  - Создает случайный URL <tags>bogus, url, faker, utility</tags>
  - Tags: `factory`
- `CreateRandomUrl()` → `string` [static]
  - Создает случайный текст на русском языке <tags>bogus, russian-text, faker, utility</tags>
  - Tags: `factory`
- `CreateRealisticBot()` → `User` [static]
  - Создает реалистичного пользователя с Bogus <tags>bogus, user, realistic, faker</tags>
  - Tags: `bogus`, `factory`, `user`
- `CreateRealisticChannel()` → `Chat` [static]
  - Создает спам-сообщение с реалистичными паттернами (alias) <tags>bogus, spam-message, realistic, faker</tags>
  - Tags: `bogus`, `chat`, `factory`
- `CreateRealisticGroup()` → `Chat` [static]
  - Создает реалистичную группу с Bogus <tags>bogus, group, realistic, faker</tags>
  - Tags: `bogus`, `chat`, `factory`
- `CreateRealisticMessage()` → `Message` [static]
  - Создает реалистичное сообщение <tags>bogus, message, realistic, faker</tags>
  - Tags: `bogus`, `factory`, `message`
- `CreateRealisticPrivateChat()` → `Chat` [static]
  - Создает реалистичный супергруппу <tags>bogus, supergroup, realistic, faker</tags>
  - Tags: `bogus`, `chat`, `factory`
- `CreateRealisticSpamMessage()` → `Message` [static]
  - Создает спам-сообщение с реалистичными паттернами (alias) <tags>bogus, spam-message, realistic, faker</tags>
  - Tags: `bogus`, `factory`, `message`, `spam`
- `CreateRealisticSupergroup()` → `Chat` [static]
  - Создает реалистичную группу с Bogus <tags>bogus, group, realistic, faker</tags>
  - Tags: `bogus`, `chat`, `factory`
- `CreateRealisticUser()` → `User` [static]
  - Создает реалистичного пользователя с Bogus <tags>bogus, user, realistic, faker</tags>
  - Tags: `bogus`, `factory`, `user`
- `CreateRussianText()` → `string` [static]
  - Получает базовый Faker для дополнительных генераций <tags>bogus, faker, utility, base</tags>
  - Tags: `factory`
- `CreateSpamMessage()` → `Message` [static]
  - Создает реалистичное сообщение <tags>bogus, message, realistic, faker</tags>
  - Tags: `factory`, `message`, `spam`
- `CreateSuspiciousUser()` → `User` [static]
  - Создает бота с реалистичными данными <tags>bogus, bot, realistic, faker</tags>
  - Tags: `factory`, `user`
- `CreateUserList()` → `List<User>` [static]
  - Создает список случайных пользователей <tags>bogus, users, collection, faker</tags>
  - Tags: `collection`, `factory`, `user`
- `GetFaker()` → `Faker` [static]
  - Получает базовый Faker для дополнительных генераций <tags>bogus, faker, utility, base</tags>
  - Tags: `fake`
- `IsSpamText()` → `bool` [static]
  - Создает реалистичный канал <tags>bogus, channel, realistic, faker</tags>
  - Tags: `spam`

### 📁 BUILDERS

#### 📄 AiChecksMockBuilder.cs
*Билдер для мока IAiChecks*
**Lines:** 57 | **Methods:** 4

**Methods:**
- `Build()` → `Mock<IAiChecks>`
  - Строит мок <tags>builders, ai-checks, build, fluent-api</tags>
  - Tags: `builder`, `mock`
- `BuildObject()` → `IAiChecks`
  - Строит мок <tags>builders, ai-checks, build, fluent-api</tags>
  - Tags: `builder`
- `ThatApprovesPhoto()` → `AiChecksMockBuilder`
  - Настраивает мок для одобрения фото <tags>builders, ai-checks, approve-photo, fluent-api</tags>
  - Tags: `builder`, `mock`
- `ThatRejectsPhoto()` → `AiChecksMockBuilder`
  - Настраивает мок для отклонения фото <tags>builders, ai-checks, reject-photo, fluent-api</tags>
  - Tags: `builder`, `mock`

#### 📄 CaptchaServiceMockBuilder.cs
*Билдер для мока ICaptchaService*
**Lines:** 50 | **Methods:** 4

**Methods:**
- `Build()` → `Mock<ICaptchaService>`
  - Строит мок <tags>builders, captcha-service, build, fluent-api</tags>
  - Tags: `builder`, `mock`
- `BuildObject()` → `ICaptchaService`
  - Строит мок <tags>builders, captcha-service, build, fluent-api</tags>
  - Tags: `builder`
- `ThatFails()` → `CaptchaServiceMockBuilder`
  - Настраивает мок для неудачного прохождения капчи <tags>builders, captcha-service, failure, fluent-api</tags>
  - Tags: `builder`, `mock`
- `ThatSucceeds()` → `CaptchaServiceMockBuilder`
  - Настраивает мок для успешного прохождения капчи <tags>builders, captcha-service, success, fluent-api</tags>
  - Tags: `builder`, `mock`

#### 📄 ChatBuilder.cs
*Builder для создания чатов Telegram*
**Lines:** 86 | **Methods:** 7

**Methods:**
- `AsGroup()` → `ChatBuilder`
  - Устанавливает чат как группу <tags>builders, chat, group, fluent-api</tags>
  - Tags: `builder`, `chat`
- `AsPrivate()` → `ChatBuilder`
  - Устанавливает чат как приватный <tags>builders, chat, private, fluent-api</tags>
  - Tags: `builder`, `chat`
- `AsSupergroup()` → `ChatBuilder`
  - Устанавливает чат как супергруппу <tags>builders, chat, supergroup, fluent-api</tags>
  - Tags: `builder`, `chat`
- `Build()` → `Chat`
  - Строит чат <tags>builders, chat, build, fluent-api</tags>
  - Tags: `builder`, `chat`
- `WithId()` → `ChatBuilder`
  - Устанавливает ID чата <tags>builders, chat, id, fluent-api</tags>
  - Tags: `builder`, `chat`
- `WithTitle()` → `ChatBuilder`
  - Устанавливает название чата <tags>builders, chat, title, fluent-api</tags>
  - Tags: `builder`, `chat`
- `WithType()` → `ChatBuilder`
  - Устанавливает тип чата <tags>builders, chat, type, fluent-api</tags>
  - Tags: `builder`, `chat`

#### 📄 MessageBuilder.cs
*Builder для создания сообщений Telegram*
**Lines:** 98 | **Methods:** 8

**Methods:**
- `AsSpam()` → `MessageBuilder`
  - Устанавливает сообщение как спам <tags>builders, message, spam, fluent-api</tags>
  - Tags: `builder`, `message`, `spam`
- `AsValid()` → `MessageBuilder`
  - Устанавливает сообщение как валидное <tags>builders, message, valid, fluent-api</tags>
  - Tags: `builder`, `message`, `valid`
- `Build()` → `Message`
  - Строит сообщение <tags>builders, message, build, fluent-api</tags>
  - Tags: `builder`, `message`
- `FromUser()` → `MessageBuilder`
  - Устанавливает отправителя сообщения <tags>builders, message, user, fluent-api</tags>
  - Tags: `builder`, `message`, `user`
- `FromUser()` → `MessageBuilder`
  - Устанавливает отправителя сообщения (полный объект) <tags>builders, message, user, fluent-api</tags>
  - Tags: `builder`, `message`, `user`
- `InChat()` → `MessageBuilder`
  - Устанавливает чат <tags>builders, message, chat, fluent-api</tags>
  - Tags: `builder`, `chat`, `message`
- `InChat()` → `MessageBuilder`
  - Устанавливает чат (полный объект) <tags>builders, message, chat, fluent-api</tags>
  - Tags: `builder`, `chat`, `message`
- `WithText()` → `MessageBuilder`
  - Устанавливает текст сообщения <tags>builders, message, text, fluent-api</tags>
  - Tags: `builder`, `message`

#### 📄 MessageHandlerMockBuilder.cs
*Билдер для мока MessageHandler*
**Lines:** 107 | **Methods:** 7

**Methods:**
- `Build()` → `Mock<MessageHandler>`
  - Строит мок <tags>builders, message-handler, build, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `BuildObject()` → `MessageHandler`
  - Строит мок <tags>builders, message-handler, build, fluent-api</tags>
  - Tags: `builder`, `message`
- `ThatDeletesMessages()` → `MessageHandlerMockBuilder`
  - Настраивает мок для удаления сообщений <tags>builders, message-handler, delete-messages, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `ThatReportsToLogChat()` → `MessageHandlerMockBuilder`
  - Настраивает мок для отправки в лог-чат <tags>builders, message-handler, report-to-log-chat, fluent-api</tags>
  - Tags: `builder`, `chat`, `message`, `mock`
- `ThatReportsWithoutDeleting()` → `MessageHandlerMockBuilder`
  - Настраивает мок для отправки уведомления без удаления <tags>builders, message-handler, report-without-deleting, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `ThatSendsSuspiciousMessageWithButtons()` → `MessageHandlerMockBuilder`
  - Настраивает мок для отправки подозрительного сообщения с кнопками <tags>builders, message-handler, send-suspicious-message-with-buttons, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `ThatThrowsException()` → `MessageHandlerMockBuilder`
  - Настраивает мок для выброса исключения <tags>builders, message-handler, throw-exception, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`

#### 📄 MessageServiceMockBuilder.cs
*Билдер для мока IMessageService*
**Lines:** 99 | **Methods:** 6

**Methods:**
- `Build()` → `Mock<IMessageService>`
  - Строит мок <tags>builders, message-service, build, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `BuildObject()` → `IMessageService`
  - Строит мок <tags>builders, message-service, build, fluent-api</tags>
  - Tags: `builder`, `message`
- `ThatSendsAdminNotification()` → `MessageServiceMockBuilder`
  - Настраивает мок для отправки уведомления админу <tags>builders, message-service, send-admin-notification, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `ThatSendsUserNotification()` → `MessageServiceMockBuilder`
  - Настраивает мок для отправки уведомления пользователю <tags>builders, message-service, send-user-notification, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`, `user`
- `ThatSendsUserNotificationWithReply()` → `MessageServiceMockBuilder`
  - Настраивает мок для отправки уведомления пользователю с ответом <tags>builders, message-service, send-user-notification-with-reply, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`, `user`
- `ThatThrowsException()` → `MessageServiceMockBuilder`
  - Настраивает мок для выброса исключения <tags>builders, message-service, throw-exception, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`

#### 📄 ModerationResultBuilder.cs
*Builder для создания результатов модерации*
**Lines:** 89 | **Methods:** 7

**Methods:**
- `AsAllow()` → `ModerationResultBuilder`
  - Устанавливает результат как разрешение <tags>builders, moderation-result, allow, fluent-api</tags>
  - Tags: `builder`
- `AsBan()` → `ModerationResultBuilder`
  - Устанавливает результат как бан <tags>builders, moderation-result, ban, fluent-api</tags>
  - Tags: `ban`, `builder`
- `AsDelete()` → `ModerationResultBuilder`
  - Устанавливает результат как удаление <tags>builders, moderation-result, delete, fluent-api</tags>
  - Tags: `builder`
- `Build()` → `ModerationResult`
  - Строит результат модерации <tags>builders, moderation-result, build, fluent-api</tags>
  - Tags: `builder`
- `WithAction()` → `ModerationResultBuilder`
  - Устанавливает действие модерации <tags>builders, moderation-result, action, fluent-api</tags>
  - Tags: `builder`
- `WithConfidence()` → `ModerationResultBuilder`
  - Устанавливает уверенность в результате <tags>builders, moderation-result, confidence, fluent-api</tags>
  - Tags: `builder`
- `WithReason()` → `ModerationResultBuilder`
  - Устанавливает причину модерации <tags>builders, moderation-result, reason, fluent-api</tags>
  - Tags: `builder`

#### 📄 ModerationServiceMockBuilder.cs
*Билдер для мока IModerationService*
**Lines:** 98 | **Methods:** 8

**Methods:**
- `Build()` → `Mock<IModerationService>`
  - Строит мок <tags>builders, moderation-service, build, fluent-api</tags>
  - Tags: `builder`, `mock`
- `BuildObject()` → `IModerationService`
  - Строит объект мока <tags>builders, moderation-service, build-object, fluent-api</tags>
  - Tags: `builder`
- `ThatAllowsMessages()` → `ModerationServiceMockBuilder`
  - Настраивает мок для разрешения сообщений <tags>builders, moderation-service, allow, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `ThatBansUsers()` → `ModerationServiceMockBuilder`
  - Настраивает мок для бана пользователей <tags>builders, moderation-service, ban, fluent-api</tags>
  - Tags: `ban`, `builder`, `mock`, `user`
- `ThatDeletesMessages()` → `ModerationServiceMockBuilder`
  - Настраивает мок для удаления сообщений <tags>builders, moderation-service, delete, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `ThatReturns()` → `ModerationServiceMockBuilder`
  - Настраивает мок для возврата указанного действия <tags>builders, moderation-service, action, fluent-api</tags>
  - Tags: `builder`, `mock`
- `WithConfidence()` → `ModerationServiceMockBuilder`
  - Настраивает мок для возврата указанной уверенности <tags>builders, moderation-service, confidence, fluent-api</tags>
  - Tags: `builder`, `mock`
- `WithReason()` → `ModerationServiceMockBuilder`
  - Настраивает мок для возврата указанной причины <tags>builders, moderation-service, reason, fluent-api</tags>
  - Tags: `builder`, `mock`

#### 📄 TelegramBotMockBuilder.cs
*Билдер для мока ITelegramBotClientWrapper*
**Lines:** 110 | **Methods:** 7

**Methods:**
- `Build()` → `Mock<ITelegramBotClientWrapper>`
  - Строит мок <tags>builders, telegram-bot, build, fluent-api</tags>
  - Tags: `builder`, `mock`
- `BuildObject()` → `ITelegramBotClientWrapper`
  - Строит мок <tags>builders, telegram-bot, build, fluent-api</tags>
  - Tags: `builder`
- `ThatDeletesMessagesSuccessfully()` → `TelegramBotMockBuilder`
  - Настраивает мок для успешного удаления сообщений <tags>builders, telegram-bot, delete-message, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `ThatForwardsMessages()` → `TelegramBotMockBuilder`
  - Настраивает мок для пересылки сообщений <tags>builders, telegram-bot, forward-message, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `ThatSendsMessageSuccessfully()` → `TelegramBotMockBuilder`
  - Настраивает мок для успешной отправки сообщений <tags>builders, telegram-bot, send-message, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `ThatThrowsOnDelete()` → `TelegramBotMockBuilder`
  - Настраивает мок для выброса исключения при удалении <tags>builders, telegram-bot, throw-on-delete, fluent-api</tags>
  - Tags: `builder`, `mock`
- `ThatThrowsOnSend()` → `TelegramBotMockBuilder`
  - Настраивает мок для выброса исключения при отправке <tags>builders, telegram-bot, throw-on-send, fluent-api</tags>
  - Tags: `builder`, `mock`

#### 📄 TestKit.Builders.cs
*Builders для создания тестовых данных в читаемом виде*
**Lines:** 350 | **Methods:** 33

**Methods:**
- `AsAllow()` → `ModerationResultBuilder`
  - Делает результат "Разрешить"
  - Tags: `builder`
- `AsBan()` → `ModerationResultBuilder`
  - Делает результат "Забанить"
  - Tags: `ban`, `builder`
- `AsBot()` → `UserBuilder`
  - Делает пользователя ботом
  - Tags: `builder`, `user`
- `AsDelete()` → `ModerationResultBuilder`
  - Делает результат "Удалить"
  - Tags: `builder`
- `AsGroup()` → `ChatBuilder`
  - Делает чат группой
  - Tags: `builder`, `chat`
- `AsPrivate()` → `ChatBuilder`
  - Делает чат приватным
  - Tags: `builder`, `chat`
- `AsRegularUser()` → `UserBuilder`
  - Делает пользователя обычным пользователем
  - Tags: `builder`, `user`
- `AsSpam()` → `MessageBuilder`
  - Делает сообщение спамом <tags>builders, message, spam, moderation, fluent-api</tags>
  - Tags: `builder`, `message`, `spam`
- `AsSupergroup()` → `ChatBuilder`
  - Делает чат супергруппой
  - Tags: `builder`, `chat`
- `AsValid()` → `MessageBuilder`
  - Делает сообщение валидным
  - Tags: `builder`, `message`, `valid`
- `Build()` → `Message`
  - Создает сообщение
  - Tags: `builder`, `message`
- `Build()` → `User`
  - Создает пользователя
  - Tags: `builder`, `user`
- `Build()` → `Chat`
  - Создает чат
  - Tags: `builder`, `chat`
- `Build()` → `ModerationResult`
  - Создает результат модерации
  - Tags: `builder`
- `CreateChat()` → `ChatBuilder` [static]
  - Создает builder для пользователя Telegram <tags>builders, user, telegram, fluent-api</tags>
  - Tags: `builder`, `chat`, `factory`
- `CreateMessage()` → `MessageBuilder` [static]
  - Builders для создания тестовых данных в читаемом виде <tags>builders, fluent-api, test-data, readable-tests</tags>
  - Tags: `builder`, `factory`, `message`
- `CreateModerationResult()` → `ModerationResultBuilder` [static]
  - Создает builder для чата Telegram <tags>builders, chat, telegram, fluent-api</tags>
  - Tags: `builder`, `factory`, `moderation`
- `CreateUser()` → `UserBuilder` [static]
  - Создает builder для сообщения Telegram <tags>builders, message, telegram, fluent-api</tags>
  - Tags: `builder`, `factory`, `user`
- `FromUser()` → `MessageBuilder`
  - Устанавливает отправителя сообщения <tags>builders, message, user, fluent-api</tags>
  - Tags: `builder`, `message`, `user`
- `FromUser()` → `MessageBuilder`
  - Устанавливает отправителя сообщения (полный объект) <tags>builders, message, user, fluent-api</tags>
  - Tags: `builder`, `message`, `user`
- `InChat()` → `MessageBuilder`
  - Устанавливает чат <tags>builders, message, chat, fluent-api</tags>
  - Tags: `builder`, `chat`, `message`
- `InChat()` → `MessageBuilder`
  - Устанавливает чат (полный объект) <tags>builders, message, chat, fluent-api</tags>
  - Tags: `builder`, `chat`, `message`
- `WithAction()` → `ModerationResultBuilder`
  - Устанавливает действие модерации
  - Tags: `builder`
- `WithConfidence()` → `ModerationResultBuilder`
  - Устанавливает уровень уверенности
  - Tags: `builder`
- `WithFirstName()` → `UserBuilder`
  - Устанавливает имя
  - Tags: `builder`, `user`
- `WithId()` → `UserBuilder`
  - Builder для создания пользователей Telegram
  - Tags: `builder`, `user`
- `WithId()` → `ChatBuilder`
  - Builder для создания чатов Telegram
  - Tags: `builder`, `chat`
- `WithMessageId()` → `MessageBuilder`
  - Устанавливает ID сообщения (только для чтения, не изменяет) <tags>builders, message, id, fluent-api</tags>
  - Tags: `builder`, `message`
- `WithReason()` → `ModerationResultBuilder`
  - Устанавливает причину
  - Tags: `builder`
- `WithText()` → `MessageBuilder`
  - Устанавливает текст сообщения <tags>builders, message, text, fluent-api</tags>
  - Tags: `builder`, `message`
- `WithTitle()` → `ChatBuilder`
  - Устанавливает название чата
  - Tags: `builder`, `chat`
- `WithType()` → `ChatBuilder`
  - Устанавливает тип чата
  - Tags: `builder`, `chat`
- `WithUsername()` → `UserBuilder`
  - Устанавливает имя пользователя
  - Tags: `builder`, `user`

#### 📄 TestKitBuilders.cs
*Builders для создания тестовых данных в читаемом виде*
**Lines:** 35 | **Methods:** 4

**Methods:**
- `CreateChat()` → `ChatBuilder` [static]
  - Создает builder для пользователя Telegram <tags>builders, user, telegram, fluent-api</tags>
  - Tags: `builder`, `chat`, `factory`
- `CreateMessage()` → `MessageBuilder` [static]
  - Builders для создания тестовых данных в читаемом виде <tags>builders, fluent-api, test-data, readable-tests</tags>
  - Tags: `builder`, `factory`, `message`
- `CreateModerationResult()` → `ModerationResultBuilder` [static]
  - Создает builder для чата Telegram <tags>builders, chat, telegram, fluent-api</tags>
  - Tags: `builder`, `factory`, `moderation`
- `CreateUser()` → `UserBuilder` [static]
  - Создает builder для сообщения Telegram <tags>builders, message, telegram, fluent-api</tags>
  - Tags: `builder`, `factory`, `user`

#### 📄 TestKitMockBuilders.cs
*Билдеры для создания и настройки моков*
**Lines:** 53 | **Methods:** 7

**Methods:**
- `CreateAiChecksMock()` → `AiChecksMockBuilder` [static]
  - Создает билдер для мока ICaptchaService <tags>builders, captcha-service, mocks, fluent-api</tags>
  - Tags: `ai`, `builder`, `factory`, `mock`
- `CreateCaptchaServiceMock()` → `CaptchaServiceMockBuilder` [static]
  - Создает билдер для мока IUserManager <tags>builders, user-manager, mocks, fluent-api</tags>
  - Tags: `builder`, `captcha`, `factory`, `mock`
- `CreateMessageHandlerMock()` → `MessageHandlerMockBuilder` [static]
  - Создает билдер для мока IMessageService <tags>builders, message-service, mocks, fluent-api</tags>
  - Tags: `builder`, `factory`, `message`, `mock`
- `CreateMessageServiceMock()` → `MessageServiceMockBuilder` [static]
  - Создает билдер для мока ITelegramBotClientWrapper <tags>builders, telegram-bot, mocks, fluent-api</tags>
  - Tags: `builder`, `factory`, `message`, `mock`
- `CreateModerationServiceMock()` → `ModerationServiceMockBuilder` [static]
  - Билдеры для создания и настройки моков <tags>builders, mocks, fluent-api, test-infrastructure</tags>
  - Tags: `builder`, `factory`, `mock`, `moderation`
- `CreateTelegramBotMock()` → `TelegramBotMockBuilder` [static]
  - Создает билдер для мока IAiChecks <tags>builders, ai-checks, mocks, fluent-api</tags>
  - Tags: `builder`, `factory`, `mock`, `telegram`
- `CreateUserManagerMock()` → `UserManagerMockBuilder` [static]
  - Создает билдер для мока IModerationService <tags>builders, moderation-service, mocks, fluent-api</tags>
  - Tags: `builder`, `factory`, `mock`, `user`

#### 📄 UserBuilder.cs
*Builder для создания пользователей Telegram*
**Lines:** 77 | **Methods:** 6

**Methods:**
- `AsBot()` → `UserBuilder`
  - Устанавливает пользователя как бота <tags>builders, user, bot, fluent-api</tags>
  - Tags: `builder`, `user`
- `AsRegularUser()` → `UserBuilder`
  - Устанавливает пользователя как обычного пользователя <tags>builders, user, regular, fluent-api</tags>
  - Tags: `builder`, `user`
- `Build()` → `User`
  - Строит пользователя <tags>builders, user, build, fluent-api</tags>
  - Tags: `builder`, `user`
- `WithFirstName()` → `UserBuilder`
  - Устанавливает имя пользователя <tags>builders, user, firstname, fluent-api</tags>
  - Tags: `builder`, `user`
- `WithId()` → `UserBuilder`
  - Устанавливает ID пользователя <tags>builders, user, id, fluent-api</tags>
  - Tags: `builder`, `user`
- `WithUsername()` → `UserBuilder`
  - Устанавливает username пользователя <tags>builders, user, username, fluent-api</tags>
  - Tags: `builder`, `user`

#### 📄 UserManagerMockBuilder.cs
*Билдер для мока IUserManager*
**Lines:** 69 | **Methods:** 6

**Methods:**
- `Build()` → `Mock<IUserManager>`
  - Строит мок <tags>builders, user-manager, build, fluent-api</tags>
  - Tags: `builder`, `mock`, `user`
- `BuildObject()` → `IUserManager`
  - Строит мок <tags>builders, user-manager, build, fluent-api</tags>
  - Tags: `builder`, `user`
- `ThatApprovesUser()` → `UserManagerMockBuilder`
  - Настраивает мок для одобрения пользователя <tags>builders, user-manager, approve, fluent-api</tags>
  - Tags: `builder`, `mock`, `user`
- `ThatIsInBanlist()` → `UserManagerMockBuilder`
  - Настраивает мок для проверки наличия пользователя в блэклисте <tags>builders, user-manager, in-banlist, fluent-api</tags>
  - Tags: `ban`, `builder`, `mock`, `user`
- `ThatIsNotInBanlist()` → `UserManagerMockBuilder`
  - Настраивает мок для проверки отсутствия пользователя в блэклисте <tags>builders, user-manager, not-in-banlist, fluent-api</tags>
  - Tags: `ban`, `builder`, `mock`, `user`
- `ThatRejectsUser()` → `UserManagerMockBuilder`
  - Настраивает мок для отклонения пользователя <tags>builders, user-manager, reject, fluent-api</tags>
  - Tags: `builder`, `mock`, `user`

### 📁 CORE

#### 📄 TestCategories.cs
*Категории тестов для оптимизации CI/CD и параллельного выполнения*
**Lines:** 164 | **Methods:** 0

#### 📄 TestKit.BuilderTests.cs
*Тесты для новых билдеров*
**Lines:** 276 | **Methods:** 16

**Methods:**
- `AiChecksMockBuilder_ThatApprovesPhoto_CreatesCorrectMock()` → `void`
  - Tags: `ai`, `builder`, `factory`, `mock`
- `CaptchaServiceMockBuilder_ThatSucceeds_CreatesCorrectMock()` → `void`
  - Tags: `builder`, `captcha`, `factory`, `mock`
- `CreateTestBotClient_ReturnsValidClient()` → `void`
  - Tags: `factory`, `test-infrastructure`, `valid`
- `CreateTestToken_ReturnsConsistentToken()` → `void`
  - Tags: `factory`, `test-infrastructure`
- `MessageHandlerBuilder_ComplexScenario_CreatesValidHandler()` → `void`
  - Tags: `builder`, `factory`, `message`, `scenario`, `valid`
- `MessageHandlerBuilder_WithBanMocks_CreatesHandlerWithBanConfiguration()` → `void`
  - Tags: `ban`, `builder`, `factory`, `message`, `mock`
- `MessageHandlerBuilder_WithModerationService_CreatesHandlerWithCustomModeration()` → `void`
  - Tags: `builder`, `factory`, `message`, `moderation`
- `MessageHandlerBuilder_WithStandardMocks_CreatesValidHandler()` → `void`
  - Тесты для новых билдеров <tags>tests, builders, fluent-api, test-infrastructure</tags>
  - Tags: `builder`, `factory`, `message`, `mock`, `valid`
- `MessageHandlerBuilder_WithUserManager_CreatesHandlerWithCustomUserManager()` → `void`
  - Tags: `builder`, `factory`, `message`, `user`
- `ModerationScenarios_CompleteSetup_WorksCorrectly()` → `void`
  - Tags: `moderation`, `scenario`, `setup`
- `ModerationScenarios_MinimalSetup_WorksCorrectly()` → `void`
  - Tags: `moderation`, `scenario`, `setup`
- `ModerationScenarios_MockedSetup_WorksCorrectly()` → `void`
  - Tags: `mock`, `moderation`, `scenario`, `setup`
- `ModerationServiceMockBuilder_ThatAllowsMessages_CreatesCorrectMock()` → `void`
  - Tags: `builder`, `factory`, `message`, `mock`, `moderation`
- `ModerationServiceMockBuilder_ThatBansUsers_CreatesCorrectMock()` → `void`
  - Tags: `ban`, `builder`, `factory`, `mock`, `moderation`, `user`
- `TelegramBotMockBuilder_ThatSendsMessageSuccessfully_CreatesCorrectMock()` → `void`
  - Tags: `builder`, `factory`, `message`, `mock`, `telegram`
- `UserManagerMockBuilder_ThatApprovesUser_CreatesCorrectMock()` → `void`
  - Tags: `builder`, `factory`, `mock`, `user`

#### 📄 TestKit.Facade.cs
**Lines:** 317 | **Methods:** 19

**Methods:**
- `CreateAiChecksFactory()` → `AiChecksTestFactory` [static]
  - Создает конфигурацию приложения без AI <tags>config, app-config, no-ai, test-infrastructure</tags>
  - Tags: `ai`, `factory`
- `CreateAppConfig()` → `IAppConfig` [static]
  - Создает фабрику для ServiceChatDispatcher <tags>factory, service-chat-dispatcher, test-infrastructure</tags>
  - Tags: `factory`
- `CreateAppConfigWithoutAi()` → `IAppConfig` [static]
  - Создает базовую конфигурацию приложения <tags>config, app-config, test-infrastructure</tags>
  - Tags: `ai`, `factory`
- `CreateCallbackQueryHandlerFactory()` → `CallbackQueryHandlerTestFactory` [static]
  - Создает фабрику для CaptchaService <tags>factory, captcha-service, test-infrastructure</tags>
  - Tags: `factory`
- `CreateCaptchaServiceFactory()` → `CaptchaServiceTestFactory` [static]
  - Создает фабрику для ModerationService <tags>factory, moderation-service, test-infrastructure</tags>
  - Tags: `captcha`, `factory`
- `CreateChatMemberHandlerFactory()` → `ChatMemberHandlerTestFactory` [static]
  - Создает фабрику для CallbackQueryHandler <tags>factory, callback-query-handler, test-infrastructure</tags>
  - Tags: `chat`, `factory`
- `CreateFakeClient()` → `FakeTelegramClient` [static]
  - Создает FakeTelegramClient для тестов
  - Tags: `factory`, `fake`
- `CreateMessageHandlerBuilder()` → `MessageHandlerBuilder` [static]
  - Создает фабрику для MessageHandler <tags>factory, message-handler, test-infrastructure</tags>
  - Tags: `builder`, `factory`, `message`
- `CreateMessageHandlerFactory()` → `MessageHandlerTestFactory` [static]
  - Создает фабрику для MessageHandler <tags>factory, message-handler, test-infrastructure</tags>
  - Tags: `factory`, `message`
- `CreateMessageHandlerWithDefaults()` → `MessageHandler` [static]
  - Создает MessageHandler с базовой настройкой для тестов
  - Tags: `factory`, `message`
- `CreateMessageHandlerWithFake()` → `MessageHandler` [static]
  - Создает MessageHandler с FakeTelegramClient
  - Tags: `factory`, `fake`, `message`
- `CreateMimicryClassifierFactory()` → `MimicryClassifierTestFactory` [static]
  - Создает фабрику для SpamHamClassifier <tags>factory, spam-ham-classifier, ml, test-infrastructure</tags>
  - Tags: `factory`
- `CreateModerationServiceFactory()` → `ModerationServiceTestFactory` [static]
  - Создает билдер для MessageHandler <tags>builder, message-handler, test-infrastructure, fluent-api</tags>
  - Tags: `factory`, `moderation`
- `CreateNotificationServiceBuilder()` → `NotificationServiceBuilder` [static]
  - Создает билдер для мока MessageHandler <tags>builders, message-handler, mocks, fluent-api</tags>
  - Tags: `builder`, `factory`
- `CreateServiceChatDispatcherFactory()` → `ServiceChatDispatcherTestFactory` [static]
  - Создает фабрику для ChatMemberHandler <tags>factory, chat-member-handler, test-infrastructure</tags>
  - Tags: `chat`, `factory`
- `CreateSpamHamClassifierFactory()` → `SpamHamClassifierTestFactory` [static]
  - Создает фабрику для StatisticsService <tags>factory, statistics-service, test-infrastructure</tags>
  - Tags: `factory`, `spam`
- `CreateStatisticsServiceFactory()` → `StatisticsServiceTestFactory` [static]
  - Создает фабрику для AiChecks <tags>factory, ai-checks, ai, test-infrastructure</tags>
  - Tags: `factory`
- `CreateUserJoinServiceBuilder()` → `UserJoinServiceBuilder` [static]
  - Создает билдер для NotificationService <tags>builders, notification-service, fluent-api</tags>
  - Tags: `builder`, `factory`, `user`
- `ResetMessageIdCounter()` → `void` [static]
  - Создает сценарий нового участника с FakeTelegramClient
  - Tags: `message`

#### 📄 TestKit.Main.cs
**Lines:** 284 | **Methods:** 43

**Methods:**
- `CreateAdminApproveCallback()` → `CallbackQuery` [static]
  - Создает невалидный callback query <tags>callback, invalid, interaction, telegram</tags>
  - Tags: `factory`
- `CreateAdminBanCallback()` → `CallbackQuery` [static]
  - Создает callback для одобрения админом <tags>callback, admin, approve, moderation, telegram</tags>
  - Tags: `ban`, `factory`
- `CreateAdminNotificationMessage()` → `Message` [static]
  - Создает сообщение от подозрительного пользователя <tags>message, suspicious, user, ai-analysis, telegram</tags>
  - Tags: `factory`, `message`
- `CreateAdminSkipCallback()` → `CallbackQuery` [static]
  - Создает callback для бана админом <tags>callback, admin, ban, moderation, telegram</tags>
  - Tags: `factory`
- `CreateAllowResult()` → `ModerationResult` [static]
  - Создает приманку-капчу <tags>captcha, bait, user-verification, moderation</tags>
  - Tags: `factory`
- `CreateAnonymousUser()` → `User` [static]
  - Создает бота-пользователя <tags>user, bot, basic, telegram</tags>
  - Tags: `factory`, `user`
- `CreateBaitCaptchaInfo()` → `CaptchaInfo` [static]
  - Создает update с изменением участника чата <tags>update, chat-member, member-management, telegram</tags>
  - Tags: `captcha`, `factory`
- `CreateBaitUser()` → `User` [static]
  - Создает неправильный результат капчи <tags>captcha, incorrect, result, user-verification</tags>
  - Tags: `factory`, `user`
- `CreateBanResult()` → `ModerationResult` [static]
  - Создает результат модерации: удалить <tags>moderation, delete, result, ml, ai</tags>
  - Tags: `ban`, `factory`
- `CreateBotUser()` → `User` [static]
  - Создает валидного пользователя <tags>user, valid, basic, telegram</tags>
  - Tags: `factory`, `user`
- `CreateCallbackQueryUpdate()` → `Update` [static]
  - Создает update с сообщением <tags>update, message, basic, telegram</tags>
  - Tags: `factory`
- `CreateChannel()` → `Chat` [static]
  - Создает супергруппу <tags>chat, supergroup, basic, telegram</tags>
  - Tags: `chat`, `factory`
- `CreateChannelMessage()` → `Message` [static]
  - Создает текстовое сообщение <tags>message, text, basic, telegram</tags>
  - Tags: `factory`, `message`
- `CreateChatMemberUpdate()` → `Update` [static]
  - Создает update с callback query <tags>update, callback, interaction, telegram</tags>
  - Tags: `chat`, `factory`
- `CreateCorrectCaptchaResult()` → `bool` [static]
  - Создает истекшую капчу <tags>captcha, expired, user-verification, moderation</tags>
  - Tags: `captcha`, `factory`
- `CreateDeleteResult()` → `ModerationResult` [static]
  - Создает результат модерации: разрешить <tags>moderation, allow, result, ml, ai</tags>
  - Tags: `factory`
- `CreateEmptyMessage()` → `Message` [static]
  - Создает спам-сообщение <tags>message, spam, moderation, ml</tags>
  - Tags: `factory`, `message`
- `CreateExpiredCaptchaInfo()` → `CaptchaInfo` [static]
  - Создает валидную капчу <tags>captcha, valid, user-verification, moderation</tags>
  - Tags: `captcha`, `factory`
- `CreateGroupChat()` → `Chat` [static]
  - Создает анонимного пользователя <tags>user, anonymous, basic, telegram</tags>
  - Tags: `chat`, `factory`
- `CreateHelpCommandMessage()` → `Message` [static]
  - Создает команду статистики <tags>message, admin, stats, command, telegram</tags>
  - Tags: `factory`, `message`
- `CreateIncorrectCaptchaResult()` → `bool` [static]
  - Создает правильный результат капчи <tags>captcha, correct, result, user-verification</tags>
  - Tags: `captcha`, `factory`
- `CreateInvalidCallbackQuery()` → `CallbackQuery` [static]
  - Создает валидный callback query <tags>callback, valid, interaction, telegram</tags>
  - Tags: `factory`, `invalid`
- `CreateLongMessage()` → `Message` [static]
  - Создает пустое сообщение <tags>message, empty, basic, telegram</tags>
  - Tags: `factory`, `message`
- `CreateMemberBanned()` → `ChatMemberUpdated` [static]
  - Создает событие выхода участника <tags>chat-member, left, member-management, telegram</tags>
  - Tags: `ban`, `chat`, `factory`
- `CreateMemberDemoted()` → `ChatMemberUpdated` [static]
  - Создает событие повышения участника <tags>chat-member, promoted, member-management, telegram</tags>
  - Tags: `chat`, `factory`
- `CreateMemberJoined()` → `ChatMemberUpdated` [static]
  - Создает событие присоединения участника <tags>chat-member, joined, member-management, telegram</tags>
  - Tags: `chat`, `factory`
- `CreateMemberLeft()` → `ChatMemberUpdated` [static]
  - Создает событие присоединения участника <tags>chat-member, joined, member-management, telegram</tags>
  - Tags: `chat`, `factory`
- `CreateMemberPromoted()` → `ChatMemberUpdated` [static]
  - Создает событие ограничения участника <tags>chat-member, restricted, member-management, telegram</tags>
  - Tags: `chat`, `factory`
- `CreateMemberRestricted()` → `ChatMemberUpdated` [static]
  - Создает событие бана участника <tags>chat-member, banned, member-management, ban, telegram</tags>
  - Tags: `chat`, `factory`
- `CreateMessageUpdate()` → `Update` [static]
  - Создает callback для пропуска админом <tags>callback, admin, skip, moderation, telegram</tags>
  - Tags: `factory`, `message`
- `CreateNewUserJoinMessage()` → `Message` [static]
  - Создает сообщение канала <tags>message, channel, basic, telegram</tags>
  - Tags: `factory`, `message`, `user`
- `CreateNullTextMessage()` → `Message` [static]
  - Создает длинное сообщение <tags>message, long, basic, telegram</tags>
  - Tags: `factory`, `message`
- `CreatePrivateChat()` → `Chat` [static]
  - Создает канал <tags>chat, channel, basic, telegram</tags>
  - Tags: `chat`, `factory`
- `CreateSpamMessage()` → `Message` [static]
  - Создает валидное сообщение с указанным ID <tags>message, valid, id, basic, telegram</tags>
  - Tags: `factory`, `message`, `spam`
- `CreateStatsCommandMessage()` → `Message` [static]
  - Создает уведомление для админов <tags>message, admin, notification, moderation, telegram</tags>
  - Tags: `factory`, `message`
- `CreateSupergroupChat()` → `Chat` [static]
  - Создает групповой чат <tags>chat, group, basic, telegram</tags>
  - Tags: `chat`, `factory`
- `CreateSuspiciousUserMessage()` → `Message` [static]
  - Создает сообщение о присоединении нового пользователя <tags>message, new-user, join, member, telegram</tags>
  - Tags: `factory`, `message`, `user`
- `CreateTextMessage()` → `Message` [static]
  - Создает приватный чат <tags>chat, private, basic, telegram</tags>
  - Tags: `factory`, `message`
- `CreateValidCallbackQuery()` → `CallbackQuery` [static]
  - Создает команду помощи <tags>message, admin, help, command, telegram</tags>
  - Tags: `factory`, `valid`
- `CreateValidCaptchaInfo()` → `CaptchaInfo` [static]
  - Создает результат модерации: забанить <tags>moderation, ban, result, ml, ai</tags>
  - Tags: `captcha`, `factory`, `valid`
- `CreateValidMessage()` → `Message` [static]
  - Создает валидное сообщение <tags>message, valid, basic, telegram</tags>
  - Tags: `factory`, `message`, `valid`
- `CreateValidMessageWithId()` → `Message` [static]
  - Создает валидное сообщение <tags>message, valid, basic, telegram</tags>
  - Tags: `factory`, `message`, `valid`
- `CreateValidUser()` → `User` [static]
  - Создает сообщение с null текстом <tags>message, null-text, basic, telegram</tags>
  - Tags: `factory`, `user`, `valid`

#### 📄 TestKit.MessageHandlerBuilder.cs
*Билдер для создания MessageHandler с настроенными зависимостями*
**Lines:** 359 | **Methods:** 12

**Methods:**
- `Build()` → `MessageHandler`
  - Создает MessageHandler с настроенными зависимостями <tags>builders, message-handler, build, fluent-api</tags>
  - Tags: `builder`, `message`
- `BuildMock()` → `Mock<IMessageHandler>`
  - Создает Mock<IMessageHandler> для прокси-сервисов <tags>builders, message-handler, proxy-services, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `WithAiChecks()` → `MessageHandlerBuilder`
  - Настраивает AI проверки через билдер <tags>builders, message-handler, ai-checks, fluent-api</tags>
  - Tags: `ai`, `builder`, `message`
- `WithAiMlMocks()` → `MessageHandlerBuilder`
  - Настраивает моки для сценария AI/ML <tags>builders, message-handler, ai-ml-scenario, fluent-api</tags>
  - Tags: `ai`, `builder`, `message`, `mock`
- `WithBanMocks()` → `MessageHandlerBuilder`
  - Настраивает моки для сценария бана <tags>builders, message-handler, ban-scenario, fluent-api</tags>
  - Tags: `ban`, `builder`, `message`, `mock`
- `WithCaptchaService()` → `MessageHandlerBuilder`
  - Настраивает сервис капчи через билдер <tags>builders, message-handler, captcha-service, fluent-api</tags>
  - Tags: `builder`, `captcha`, `message`
- `WithChannelMocks()` → `MessageHandlerBuilder`
  - Настраивает моки для сценария канала <tags>builders, message-handler, channel-scenario, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `WithModerationMocks()` → `MessageHandlerBuilder`
  - Настраивает моки для сценария модерации <tags>builders, message-handler, moderation-scenario, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`, `moderation`
- `WithModerationService()` → `MessageHandlerBuilder`
  - Настраивает модерационный сервис через билдер <tags>builders, message-handler, moderation-service, fluent-api</tags>
  - Tags: `builder`, `message`, `moderation`
- `WithStandardMocks()` → `MessageHandlerBuilder`
  - Настраивает стандартные моки (базовая конфигурация) <tags>builders, message-handler, standard-mocks, fluent-api</tags>
  - Tags: `builder`, `message`, `mock`
- `WithTelegramBot()` → `MessageHandlerBuilder`
  - Настраивает Telegram бота через билдер <tags>builders, message-handler, telegram-bot, fluent-api</tags>
  - Tags: `builder`, `message`, `telegram`
- `WithUserManager()` → `MessageHandlerBuilder`
  - Настраивает менеджер пользователей через билдер <tags>builders, message-handler, user-manager, fluent-api</tags>
  - Tags: `builder`, `message`, `user`

#### 📄 TestKit.NotificationServiceBuilder.cs
*Билдер для создания NotificationService с настроенными зависимостями*
**Lines:** 84 | **Methods:** 2

**Methods:**
- `Build()` → `NotificationService`
  - Создает NotificationService с настроенными зависимостями <tags>builders, notification-service, build, fluent-api</tags>
  - Tags: `builder`
- `WithStandardMocks()` → `NotificationServiceBuilder`
  - Настраивает стандартные моки для базового сценария <tags>builders, notification-service, standard-mocks, fluent-api</tags>
  - Tags: `builder`, `mock`

#### 📄 TestKit.UserJoinServiceBuilder.cs
*Билдер для создания UserJoinService с настроенными зависимостями*
**Lines:** 109 | **Methods:** 5

**Methods:**
- `Build()` → `UserJoinService`
  - Создает UserJoinService с настроенными зависимостями <tags>builders, user-join-service, build, fluent-api</tags>
  - Tags: `builder`, `user`
- `WithBlacklistedUserScenario()` → `UserJoinServiceBuilder`
  - Настраивает моки для сценария пользователя в блэклисте <tags>builders, user-join-service, blacklist-scenario, fluent-api</tags>
  - Tags: `builder`, `scenario`, `user`
- `WithCaptchaScenario()` → `UserJoinServiceBuilder`
  - Настраивает моки для сценария создания капчи <tags>builders, user-join-service, captcha-scenario, fluent-api</tags>
  - Tags: `builder`, `captcha`, `scenario`, `user`
- `WithStandardMocks()` → `UserJoinServiceBuilder`
  - Настраивает стандартные моки для базового сценария <tags>builders, user-join-service, standard-mocks, fluent-api</tags>
  - Tags: `builder`, `mock`, `user`
- `WithSuccessfulJoinScenario()` → `UserJoinServiceBuilder`
  - Настраивает моки для сценария успешного присоединения пользователя <tags>builders, user-join-service, success-scenario, fluent-api</tags>
  - Tags: `builder`, `scenario`, `user`

### 📁 GOLDEN-MASTER

#### 📄 TestKit.GoldenMaster.cs
*Golden Master инфраструктура для тестирования логики банов*
**Lines:** 439 | **Methods:** 8

**Methods:**
- `CreateBotBanScenario()` → `BanScenario`
  - Создает сценарий бана бота <tags>golden-master, bot-ban, scenario</tags>
  - Tags: `ban`, `factory`, `scenario`
- `CreateExceptionScenarioSet()` → `List<BanScenario>` [static]
  - Создает набор сценариев с исключениями <tags>golden-master, exception-scenarios, error-testing</tags>
  - Tags: `collection`, `factory`, `scenario`
- `CreateNullMessageBanScenario()` → `BanScenario`
  - Создает сценарий бана без сообщения <tags>golden-master, null-message, scenario</tags>
  - Tags: `ban`, `factory`, `message`, `scenario`
- `CreatePermanentBanScenario()` → `BanScenario`
  - Создает сценарий перманентного бана <tags>golden-master, permanent-ban, scenario</tags>
  - Tags: `ban`, `factory`, `scenario`
- `CreatePrivateChatBanScenario()` → `BanScenario`
  - Создает сценарий бана в приватном чате <tags>golden-master, private-chat, scenario</tags>
  - Tags: `ban`, `chat`, `factory`, `scenario`
- `CreateScenarioSet()` → `List<BanScenario>` [static]
  - Создает набор разнообразных сценариев <tags>golden-master, scenario-set, variety</tags>
  - Tags: `collection`, `factory`, `scenario`
- `CreateTemporaryBanScenario()` → `BanScenario`
  - Создает сценарий временного бана <tags>golden-master, temporary-ban, scenario</tags>
  - Tags: `ban`, `factory`, `scenario`
- `SetupGoldenMasterMocks()` → `MessageHandlerTestFactory` [static]
  - Создает стандартный набор моков для Golden Master тестов <tags>golden-master, mocks, test-setup</tags>
  - Tags: `factory`, `golden-master`, `message`, `mock`, `setup`

### 📁 INFRASTRUCTURE

#### 📄 TestKitAutoFixture.cs
*Расширение TestKit с AutoFixture для автоматического создания тестовых объектов*
**Lines:** 291 | **Methods:** 11

**Methods:**
- `CreateCaptchaService()` → `ICaptchaService` [static]
  - Создает CaptchaService с автозависимостями <tags>autofixture, captcha-service, dependencies, test-infrastructure</tags>
  - Tags: `captcha`, `factory`
- `CreateFixture()` → `IFixture` [static]
  - Создает настроенный AutoFixture с кастомизациями для Telegram-типов <tags>autofixture, customization, telegram, test-infrastructure</tags>
  - Tags: `autofixture`, `factory`
- `CreateManyMessages()` → `List<Telegram.Bot.Types.Message>` [static]
  - Создает много сообщений <tags>autofixture, many-messages, collection, test-infrastructure</tags>
  - Tags: `collection`, `factory`, `message`
- `CreateManySpamMessages()` → `List<Telegram.Bot.Types.Message>` [static]
  - Создает много спам-сообщений <tags>autofixture, many-spam-messages, collection, test-infrastructure</tags>
  - Tags: `collection`, `factory`, `message`, `spam`
- `CreateManyUsers()` → `List<Telegram.Bot.Types.User>` [static]
  - Создает много пользователей <tags>autofixture, many-users, collection, test-infrastructure</tags>
  - Tags: `collection`, `factory`, `user`
- `CreateMessageHandler()` → `MessageHandler` [static]
  - Создает MessageHandler с автозависимостями <tags>autofixture, message-handler, dependencies, test-infrastructure</tags>
  - Tags: `factory`, `message`
- `CreateModerationService()` → `ModerationService` [static]
  - Создает ModerationService с автозависимостями <tags>autofixture, moderation-service, dependencies, test-infrastructure</tags>
  - Tags: `factory`, `moderation`
- `CreateRealisticMessages()` → `List<Telegram.Bot.Types.Message>` [static]
  - Создает список реалистичных сообщений <tags>autofixture, messages, realistic, test-infrastructure</tags>
  - Tags: `bogus`, `collection`, `factory`, `message`
- `CreateRealisticUsers()` → `List<Telegram.Bot.Types.User>` [static]
  - Создает список реалистичных пользователей <tags>autofixture, users, realistic, test-infrastructure</tags>
  - Tags: `bogus`, `collection`, `factory`, `user`
- `CreateUserManager()` → `IUserManager` [static]
  - Создает CaptchaService с автозависимостями <tags>autofixture, captcha-service, dependencies, test-infrastructure</tags>
  - Tags: `factory`, `user`
- `GetFixture()` → `IFixture` [static]
  - Получает глобальный экземпляр AutoFixture <tags>autofixture, global, singleton, test-infrastructure</tags>
  - Tags: `autofixture`

#### 📄 TestKitBogus.cs
*Расширение TestKit с Bogus для генерации реалистичных тестовых данных*
**Lines:** 327 | **Methods:** 18

**Methods:**
- `CreateConversation()` → `List<Message>` [static]
  - Создает историю сообщений для чата <tags>bogus, message-history, conversation, faker</tags>
  - Tags: `collection`, `factory`, `message`
- `CreateMediaMessage()` → `Message` [static]
  - Создает сообщение с медиа <tags>bogus, media-message, realistic, faker</tags>
  - Tags: `factory`, `message`
- `CreateRandomDate()` → `DateTime` [static]
  - Создает случайную дату в диапазоне <tags>bogus, date, faker, utility</tags>
  - Tags: `factory`
- `CreateRandomUrl()` → `string` [static]
  - Создает случайный URL <tags>bogus, url, faker, utility</tags>
  - Tags: `factory`
- `CreateRealisticBot()` → `User` [static]
  - Создает бота с реалистичными данными <tags>bogus, bot, realistic, faker</tags>
  - Tags: `bogus`, `factory`, `user`
- `CreateRealisticChannel()` → `Chat` [static]
  - Создает спам-сообщение с реалистичными паттернами (alias для CreateSpamMessage) <tags>bogus, spam-message, realistic, faker</tags>
  - Tags: `bogus`, `chat`, `factory`
- `CreateRealisticGroup()` → `Chat` [static]
  - Создает реалистичную группу с Bogus <tags>bogus, group, realistic, faker</tags>
  - Tags: `bogus`, `chat`, `factory`
- `CreateRealisticMessage()` → `Message` [static]
  - Создает реалистичное сообщение <tags>bogus, message, realistic, faker</tags>
  - Tags: `bogus`, `factory`, `message`
- `CreateRealisticPrivateChat()` → `Chat` [static]
  - Создает приватный чат <tags>bogus, private-chat, realistic, faker</tags>
  - Tags: `bogus`, `chat`, `factory`
- `CreateRealisticSpamMessage()` → `Message` [static]
  - Создает спам-сообщение с реалистичными паттернами (alias для CreateSpamMessage) <tags>bogus, spam-message, realistic, faker</tags>
  - Tags: `bogus`, `factory`, `message`, `spam`
- `CreateRealisticSupergroup()` → `Chat` [static]
  - Создает реалистичный супергруппу <tags>bogus, supergroup, realistic, faker</tags>
  - Tags: `bogus`, `chat`, `factory`
- `CreateRealisticUser()` → `User` [static]
  - Создает реалистичного пользователя с Bogus <tags>bogus, user, realistic, faker</tags>
  - Tags: `bogus`, `factory`, `user`
- `CreateRussianText()` → `string` [static]
  - Получает базовый Faker для дополнительных генераций <tags>bogus, faker, utility, base</tags>
  - Tags: `factory`
- `CreateSpamMessage()` → `Message` [static]
  - Создает спам-сообщение <tags>bogus, spam-message, realistic, faker</tags>
  - Tags: `factory`, `message`, `spam`
- `CreateSuspiciousUser()` → `User` [static]
  - Создает подозрительного пользователя (потенциальный спаммер) <tags>bogus, suspicious-user, spammer, faker</tags>
  - Tags: `factory`, `user`
- `CreateUserList()` → `List<User>` [static]
  - Создает список случайных пользователей <tags>bogus, users, collection, faker</tags>
  - Tags: `collection`, `factory`, `user`
- `GetFaker()` → `Faker` [static]
  - Получает базовый Faker для дополнительных генераций <tags>bogus, faker, utility, base</tags>
  - Tags: `fake`
- `IsSpamText()` → `bool` [static]
  - Проверяет, содержит ли текст спам-паттерны <tags>bogus, spam-check, utility</tags>
  - Tags: `spam`

### 📁 MOCKS

#### 📄 TestKit.Mocks.cs
**Lines:** 500 | **Methods:** 33

**Methods:**
- `CreateApprovedUserManager()` → `Mock<IUserManager>` [static]
  - Создает мок IUserManager с предустановленным одобренным пользователем
  - Tags: `factory`, `mock`, `user`
- `CreateBanModerationService()` → `Mock<IModerationService>` [static]
  - Создает мок IModerationService с предустановленным действием бана
  - Tags: `ban`, `factory`, `mock`, `moderation`
- `CreateBanTriggeringViolationTracker()` → `Mock<IViolationTracker>` [static]
  - Создает мок IViolationTracker с предустановленным триггером бана
  - Tags: `ban`, `factory`, `mock`
- `CreateBotPermissionsServiceMock()` → `Mock<IBotPermissionsService>` [static]
  - Создает мок IBotPermissionsService с заданным значением isAdmin <tags>mock, bot-permissions, admin, test-infrastructure</tags>
  - Tags: `factory`, `mock`
- `CreateBotPermissionsServiceMockForChat()` → `Mock<IBotPermissionsService>` [static]
  - Создает мок IBotPermissionsService, который возвращает true только для заданного chatId <tags>mock, bot-permissions, admin, test-infrastructure, chat-specific</tags>
  - Tags: `chat`, `factory`, `mock`
- `CreateDeleteModerationService()` → `Mock<IModerationService>` [static]
  - Создает мок IModerationService с предустановленным действием удаления
  - Tags: `factory`, `mock`, `moderation`
- `CreateErrorAiChecks()` → `Mock<IAiChecks>` [static]
  - Создает Mock IAiChecks для сценария "ошибка API"
  - Tags: `ai`, `factory`, `mock`
- `CreateFailedCaptchaService()` → `Mock<ICaptchaService>` [static]
  - Создает мок ICaptchaService с предустановленным неуспешным ответом
  - Tags: `captcha`, `factory`, `mock`
- `CreateMockAppConfig()` → `Mock<IAppConfig>` [static]
  - Создает мок IAppConfig с базовыми настройками
  - Tags: `factory`, `mock`
- `CreateMockBotClient()` → `Mock<ITelegramBotClient>` [static]
  - Создает мок ITelegramBotClient <tags>mock, telegram, bot-client, api</tags>
  - Tags: `factory`, `mock`
- `CreateMockBotClientWrapper()` → `Mock<ITelegramBotClientWrapper>` [static]
  - Создает мок ITelegramBotClientWrapper <tags>mock, telegram, bot-client-wrapper, api</tags>
  - Tags: `factory`, `mock`
- `CreateMockBotPermissionsService()` → `Mock<IBotPermissionsService>` [static]
  - <summary> Создает Mock IBotPermissionsService </summary>
  - Tags: `factory`, `mock`
- `CreateMockCaptchaService()` → `Mock<ICaptchaService>` [static]
  - Tags: `captcha`, `factory`, `mock`
- `CreateMockMessageService()` → `Mock<IMessageService>` [static]
  - Tags: `factory`, `message`, `mock`
- `CreateMockModerationService()` → `Mock<IModerationService>` [static]
  - Создает реальный SpamHamClassifier с мок-логгером
  - Tags: `factory`, `mock`, `moderation`
- `CreateMockServiceProvider()` → `Mock<IServiceProvider>` [static]
  - <summary> Создает Mock IServiceProvider </summary>
  - Tags: `factory`, `mock`
- `CreateMockSpamHamClassifier()` → `Mock<ISpamHamClassifier>` [static]
  - Создает мок ISpamHamClassifier
  - Tags: `factory`, `mock`, `spam`
- `CreateMockStatisticsService()` → `Mock<IStatisticsService>` [static]
  - <summary> Создает Mock IStatisticsService </summary>
  - Tags: `factory`, `mock`
- `CreateMockUserBanService()` → `Mock<IUserBanService>` [static]
  - Tags: `ban`, `factory`, `mock`, `user`
- `CreateMockUserManager()` → `Mock<IUserManager>` [static]
  - Tags: `factory`, `mock`, `user`
- `CreateMockViolationTracker()` → `Mock<IViolationTracker>` [static]
  - Tags: `factory`, `mock`
- `CreateNormalAiChecks()` → `Mock<IAiChecks>` [static]
  - Создает Mock IAiChecks для сценария "нормальное сообщение"
  - Tags: `ai`, `factory`, `mock`
- `CreateSpamAiChecks()` → `Mock<IAiChecks>` [static]
  - Создает Mock IAiChecks для сценария "спам"
  - Tags: `ai`, `factory`, `mock`, `spam`
- `CreateSpamHamClassifier()` → `SpamHamClassifier` [static]
  - Создает реальный SpamHamClassifier с мок-логгером
  - Tags: `factory`, `spam`
- `CreateSuccessfulCaptchaService()` → `Mock<ICaptchaService>` [static]
  - Создает мок ICaptchaService с предустановленным успешным ответом
  - Tags: `captcha`, `factory`, `mock`
- `CreateSuspiciousUserAiChecks()` → `Mock<IAiChecks>` [static]
  - Создает Mock IAiChecks для сценария "подозрительный пользователь"
  - Tags: `ai`, `factory`, `mock`, `user`
- `CreateTestBotClient()` → `TelegramBotClient` [static]
  - Создает реальный TelegramBotClient с унифицированным тестовым токеном Заменяет различные hardcoded токены в тестах <tags>telegram, bot-client, test-token, unified</tags>
  - Tags: `factory`, `test-infrastructure`
- `CreateTestChat()` → `Chat` [static]
  - Создает тестовый чат
  - Tags: `chat`, `factory`, `test-infrastructure`
- `CreateTestMessage()` → `Message` [static]
  - Создает тестовое сообщение <tags>mock, telegram, message, test-data</tags>
  - Tags: `factory`, `message`, `test-infrastructure`
- `CreateTestToken()` → `string` [static]
  - Возвращает стандартный тестовый токен для всех тестов Унифицирует различные токены используемые в тестах <tags>telegram, test-token, unified</tags>
  - Tags: `factory`, `test-infrastructure`
- `CreateTestUpdate()` → `Update` [static]
  - Создает тестовый Update
  - Tags: `factory`, `test-infrastructure`
- `CreateTestUser()` → `User` [static]
  - Создает тестового пользователя
  - Tags: `factory`, `test-infrastructure`, `user`
- `CreateUnapprovedUserManager()` → `Mock<IUserManager>` [static]
  - Создает мок IUserManager с предустановленным неодобренным пользователем
  - Tags: `factory`, `mock`, `user`

### 📁 SPECIALIZED

#### 📄 TestKit.Specialized.cs
*Специализированные генераторы для конкретных доменных областей*
**Lines:** 747 | **Methods:** 36

**Methods:**
- `Allow()` → `ModerationResult` [static]
  - Генераторы для результатов модерации <tags>moderation, ml, ai, spam-detection</tags>
- `AllowResult()` → `ModerationResult` [static]
  - Результат модерации: удаление <tags>ban, moderation, delete, result, ml</tags>
- `ApproveCallback()` → `CallbackQuery` [static]
  - Генераторы для админских действий <tags>admin, callback, moderation, admin-actions</tags>
- `Bait()` → `CaptchaInfo` [static]
  - Истекшая капча для тестов <tags>captcha, expired, user-verification</tags>
- `Bait()` → `User` [static]
  - Генераторы для специальных пользователей <tags>users, special, domain-specific</tags>
  - Tags: `user`
- `Ban()` → `ModerationResult` [static]
  - Результат модерации: удалить сообщение <tags>moderation, delete, ml, ai</tags>
  - Tags: `ban`
- `BanCallback()` → `CallbackQuery` [static]
  - Callback для одобрения пользователя админом <tags>admin, callback, approve, moderation</tags>
  - Tags: `ban`
- `BanResult()` → `ModerationResult` [static]
  - Результат модерации: бан <tags>ban, moderation, result, ml</tags>
  - Tags: `ban`
- `ChannelMessage()` → `Message` [static]
  - Сообщение от канала для тестов банов <tags>ban, channel, message, moderation, deprecated</tags>
  - Tags: `message`
- `ChatForBanTest()` → `Chat` [static]
  - Чат для тестов банов <tags>ban, chat, integration, deprecated</tags>
  - Tags: `ban`, `chat`, `test-infrastructure`
- `CompleteSetup()` → `ModerationSetup` [static]
  - Создает полный setup ModerationService со всеми реальными и мокированными зависимостями Заменяет ~45 строк дублированного кода в SetUp методах
  - Tags: `setup`
- `CorrectResult()` → `bool` [static]
  - Приманка-капча для тестов <tags>captcha, bait, user-verification</tags>
- `Delete()` → `ModerationResult` [static]
  - Результат модерации: разрешить сообщение <tags>moderation, allow, ml, ai</tags>
- `DeleteResult()` → `ModerationResult` [static]
  - Результат модерации: бан <tags>ban, moderation, result, ml</tags>
- `Expired()` → `CaptchaInfo` [static]
  - Валидная капча для тестов <tags>captcha, valid, user-verification</tags>
- `ForBanTest()` → `Chat` [static]
  - Генераторы для чатов <tags>chat, domain-specific, test-scenarios</tags>
  - Tags: `ban`, `chat`, `test-infrastructure`
- `ForChannelTest()` → `Chat` [static]
  - Чат для тестов банов <tags>chat, ban, integration</tags>
  - Tags: `chat`, `test-infrastructure`
- `HelpCommand()` → `Message` [static]
  - Команда статистики для админов <tags>admin, stats, command</tags>
  - Tags: `message`
- `IncorrectResult()` → `bool` [static]
  - Правильный результат капчи <tags>captcha, correct, result</tags>
- `Invalid()` → `CallbackQuery` [static]
  - Валидный callback query <tags>callback, valid, interaction</tags>
  - Tags: `invalid`
- `MemberBanned()` → `ChatMemberUpdated` [static]
  - Пользователь покинул чат <tags>chat, member, left, updates</tags>
  - Tags: `ban`, `chat`
- `MemberDemoted()` → `ChatMemberUpdated` [static]
  - Пользователь повышен в чате <tags>chat, member, promoted, updates</tags>
  - Tags: `chat`
- `MemberJoined()` → `ChatMemberUpdated` [static]
  - Генераторы для обновлений чата <tags>chat, updates, member-management</tags>
  - Tags: `chat`
- `MemberLeft()` → `ChatMemberUpdated` [static]
  - Пользователь присоединился к чату <tags>chat, member, joined, updates</tags>
  - Tags: `chat`
- `MemberPromoted()` → `ChatMemberUpdated` [static]
  - Пользователь ограничен в чате <tags>chat, member, restricted, updates</tags>
  - Tags: `chat`
- `MemberRestricted()` → `ChatMemberUpdated` [static]
  - Пользователь забанен в чате <tags>chat, member, banned, updates, ban</tags>
  - Tags: `chat`
- `MinimalSetup()` → `ModerationSetup` [static]
  - Создает минимальный setup только с необходимыми компонентами Для простых unit тестов где не нужны все зависимости
  - Tags: `setup`
- `NewUserJoin()` → `Message` [static]
  - Сообщение от подозрительного пользователя <tags>messages, suspicious, user, ai-analysis</tags>
  - Tags: `message`, `user`
- `Notification()` → `Message` [static]
  - Callback для пропуска пользователя админом <tags>admin, callback, skip, moderation</tags>
  - Tags: `message`
- `SkipCallback()` → `CallbackQuery` [static]
  - Callback для бана пользователя админом <tags>admin, callback, ban, moderation</tags>
- `SpamMessage()` → `Message` [static]
  - Спам сообщение для тестов банов <tags>ban, spam, message, moderation, deprecated</tags>
  - Tags: `message`, `spam`
- `StatsCommand()` → `Message` [static]
  - Уведомление для админов <tags>admin, notification, moderation</tags>
  - Tags: `message`
- `SuspiciousUser()` → `Message` [static]
  - Генераторы для специальных сообщений <tags>messages, special, domain-specific</tags>
  - Tags: `message`, `user`
- `UserForBan()` → `User` [static]
  - Пользователь для тестов банов <tags>ban, user, integration, deprecated</tags>
  - Tags: `ban`, `user`
- `Valid()` → `CaptchaInfo` [static]
  - Генераторы для работы с капчей <tags>captcha, moderation, user-verification</tags>
  - Tags: `valid`
- `Valid()` → `CallbackQuery` [static]
  - Генераторы для callback query'ев <tags>callback, query, interaction</tags>
  - Tags: `valid`

### 📁 TELEGRAM

#### 📄 TestKit.Telegram.cs
*Улучшенная работа с Telegram объектами для тестов*
**Lines:** 275 | **Methods:** 5

**Methods:**
- `CreateFakeClient()` → `FakeTelegramClient` [static]
  - Создает FakeTelegramClient с предустановленными настройками <tags>telegram, fake-client, test-infrastructure</tags>
  - Tags: `factory`, `fake`
- `CreateMessageFromEnvelope()` → `Message` [static]
  - Создает Message из MessageEnvelope через FakeTelegramClient <tags>telegram, message, message-envelope, fake-client, test-infrastructure</tags>
  - Tags: `factory`, `message`
- `CreateUpdateFromEnvelope()` → `Update` [static]
  - Создает Update с Message из MessageEnvelope
  - Tags: `factory`
- `ResetMessageIdCounter()` → `void` [static]
  - Сбрасывает счетчик MessageId (для изоляции тестов)
  - Tags: `message`
- `SetNextMessageId()` → `void` [static]
  - Устанавливает следующий MessageId (для предсказуемых тестов)
  - Tags: `message`

## 🏷️ Methods by Tags

### 🏷️ ai
- `AiChecksMockBuilder_ThatApprovesPhoto_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `CreateAiChecksFactory()` in *TestKit.Facade.cs*
- `CreateAiChecksMock()` in *TestKitMockBuilders.cs*
- `CreateAppConfigWithoutAi()` in *TestKit.Facade.cs*
- `CreateErrorAiChecks()` in *TestKit.Mocks.cs*
- `CreateNormalAiChecks()` in *TestKit.Mocks.cs*
- `CreateSpamAiChecks()` in *TestKit.Mocks.cs*
- `CreateSuspiciousUserAiChecks()` in *TestKit.Mocks.cs*
- `WithAiChecks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithAiMlMocks()` in *TestKit.MessageHandlerBuilder.cs*

### 🏷️ autofixture
- `CreateFixture()` in *TestKit.AutoFixture.cs*
- `CreateFixture()` in *TestKitAutoFixture.cs*
- `GetFixture()` in *TestKit.AutoFixture.cs*
- `GetFixture()` in *TestKitAutoFixture.cs*

### 🏷️ ban
- `AsBan()` in *TestKit.Builders.cs*
- `AsBan()` in *ModerationResultBuilder.cs*
- `Ban()` in *TestKit.Specialized.cs*
- `BanCallback()` in *TestKit.Specialized.cs*
- `BanResult()` in *TestKit.Specialized.cs*
- `ChatForBanTest()` in *TestKit.Specialized.cs*
- `CreateAdminBanCallback()` in *TestKit.Main.cs*
- `CreateBanModerationService()` in *TestKit.Mocks.cs*
- `CreateBanResult()` in *TestKit.Main.cs*
- `CreateBanTriggeringViolationTracker()` in *TestKit.Mocks.cs*
- `CreateBotBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreateMemberBanned()` in *TestKit.Main.cs*
- `CreateMockUserBanService()` in *TestKit.Mocks.cs*
- `CreateNullMessageBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreatePermanentBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreatePrivateChatBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreateTemporaryBanScenario()` in *TestKit.GoldenMaster.cs*
- `ForBanTest()` in *TestKit.Specialized.cs*
- `MemberBanned()` in *TestKit.Specialized.cs*
- `MessageHandlerBuilder_WithBanMocks_CreatesHandlerWithBanConfiguration()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatBansUsers_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `ThatBansUsers()` in *ModerationServiceMockBuilder.cs*
- `ThatIsInBanlist()` in *UserManagerMockBuilder.cs*
- `ThatIsNotInBanlist()` in *UserManagerMockBuilder.cs*
- `UserForBan()` in *TestKit.Specialized.cs*
- `WithBanMocks()` in *TestKit.MessageHandlerBuilder.cs*

### 🏷️ bogus
- `CreateRealisticBot()` in *TestKit.Bogus.cs*
- `CreateRealisticBot()` in *TestKitBogus.cs*
- `CreateRealisticChannel()` in *TestKit.Bogus.cs*
- `CreateRealisticChannel()` in *TestKitBogus.cs*
- `CreateRealisticGroup()` in *TestKit.Bogus.cs*
- `CreateRealisticGroup()` in *TestKitBogus.cs*
- `CreateRealisticMessage()` in *TestKit.Bogus.cs*
- `CreateRealisticMessage()` in *TestKitBogus.cs*
- `CreateRealisticMessages()` in *TestKit.AutoFixture.cs*
- `CreateRealisticMessages()` in *TestKitAutoFixture.cs*
- `CreateRealisticPrivateChat()` in *TestKit.Bogus.cs*
- `CreateRealisticPrivateChat()` in *TestKitBogus.cs*
- `CreateRealisticSpamMessage()` in *TestKit.Bogus.cs*
- `CreateRealisticSpamMessage()` in *TestKitBogus.cs*
- `CreateRealisticSupergroup()` in *TestKit.Bogus.cs*
- `CreateRealisticSupergroup()` in *TestKitBogus.cs*
- `CreateRealisticUser()` in *TestKit.Bogus.cs*
- `CreateRealisticUser()` in *TestKitBogus.cs*
- `CreateRealisticUsers()` in *TestKit.AutoFixture.cs*
- `CreateRealisticUsers()` in *TestKitAutoFixture.cs*

### 🏷️ builder
- `AiChecksMockBuilder_ThatApprovesPhoto_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `AsAllow()` in *TestKit.Builders.cs*
- `AsAllow()` in *ModerationResultBuilder.cs*
- `AsBan()` in *TestKit.Builders.cs*
- `AsBan()` in *ModerationResultBuilder.cs*
- `AsBot()` in *TestKit.Builders.cs*
- `AsBot()` in *UserBuilder.cs*
- `AsDelete()` in *TestKit.Builders.cs*
- `AsDelete()` in *ModerationResultBuilder.cs*
- `AsGroup()` in *TestKit.Builders.cs*
- `AsGroup()` in *ChatBuilder.cs*
- `AsPrivate()` in *TestKit.Builders.cs*
- `AsPrivate()` in *ChatBuilder.cs*
- `AsRegularUser()` in *TestKit.Builders.cs*
- `AsRegularUser()` in *UserBuilder.cs*
- `AsSpam()` in *TestKit.Builders.cs*
- `AsSpam()` in *MessageBuilder.cs*
- `AsSupergroup()` in *TestKit.Builders.cs*
- `AsSupergroup()` in *ChatBuilder.cs*
- `AsValid()` in *TestKit.Builders.cs*
- `AsValid()` in *MessageBuilder.cs*
- `Build()` in *TestKit.UserJoinServiceBuilder.cs*
- `Build()` in *TestKit.MessageHandlerBuilder.cs*
- `Build()` in *TestKit.Builders.cs*
- `Build()` in *TestKit.Builders.cs*
- `Build()` in *TestKit.Builders.cs*
- `Build()` in *TestKit.Builders.cs*
- `Build()` in *TestKit.NotificationServiceBuilder.cs*
- `Build()` in *MessageBuilder.cs*
- `Build()` in *UserBuilder.cs*
- `Build()` in *ChatBuilder.cs*
- `Build()` in *ModerationResultBuilder.cs*
- `Build()` in *TelegramBotMockBuilder.cs*
- `Build()` in *MessageHandlerMockBuilder.cs*
- `Build()` in *CaptchaServiceMockBuilder.cs*
- `Build()` in *AiChecksMockBuilder.cs*
- `Build()` in *ModerationServiceMockBuilder.cs*
- `Build()` in *MessageServiceMockBuilder.cs*
- `Build()` in *UserManagerMockBuilder.cs*
- `BuildMock()` in *TestKit.MessageHandlerBuilder.cs*
- `BuildObject()` in *TelegramBotMockBuilder.cs*
- `BuildObject()` in *MessageHandlerMockBuilder.cs*
- `BuildObject()` in *CaptchaServiceMockBuilder.cs*
- `BuildObject()` in *AiChecksMockBuilder.cs*
- `BuildObject()` in *ModerationServiceMockBuilder.cs*
- `BuildObject()` in *MessageServiceMockBuilder.cs*
- `BuildObject()` in *UserManagerMockBuilder.cs*
- `CaptchaServiceMockBuilder_ThatSucceeds_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `CreateAiChecksMock()` in *TestKitMockBuilders.cs*
- `CreateCaptchaServiceMock()` in *TestKitMockBuilders.cs*
- `CreateChat()` in *TestKit.Builders.cs*
- `CreateChat()` in *TestKitBuilders.cs*
- `CreateMessage()` in *TestKit.Builders.cs*
- `CreateMessage()` in *TestKitBuilders.cs*
- `CreateMessageHandlerBuilder()` in *TestKit.Facade.cs*
- `CreateMessageHandlerMock()` in *TestKitMockBuilders.cs*
- `CreateMessageServiceMock()` in *TestKitMockBuilders.cs*
- `CreateModerationResult()` in *TestKit.Builders.cs*
- `CreateModerationResult()` in *TestKitBuilders.cs*
- `CreateModerationServiceMock()` in *TestKitMockBuilders.cs*
- `CreateNotificationServiceBuilder()` in *TestKit.Facade.cs*
- `CreateTelegramBotMock()` in *TestKitMockBuilders.cs*
- `CreateUser()` in *TestKit.Builders.cs*
- `CreateUser()` in *TestKitBuilders.cs*
- `CreateUserJoinServiceBuilder()` in *TestKit.Facade.cs*
- `CreateUserManagerMock()` in *TestKitMockBuilders.cs*
- `FromUser()` in *TestKit.Builders.cs*
- `FromUser()` in *TestKit.Builders.cs*
- `FromUser()` in *MessageBuilder.cs*
- `FromUser()` in *MessageBuilder.cs*
- `InChat()` in *TestKit.Builders.cs*
- `InChat()` in *TestKit.Builders.cs*
- `InChat()` in *MessageBuilder.cs*
- `InChat()` in *MessageBuilder.cs*
- `MessageHandlerBuilder_ComplexScenario_CreatesValidHandler()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithBanMocks_CreatesHandlerWithBanConfiguration()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithModerationService_CreatesHandlerWithCustomModeration()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithStandardMocks_CreatesValidHandler()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithUserManager_CreatesHandlerWithCustomUserManager()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatAllowsMessages_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatBansUsers_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `TelegramBotMockBuilder_ThatSendsMessageSuccessfully_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `ThatAllowsMessages()` in *ModerationServiceMockBuilder.cs*
- `ThatApprovesPhoto()` in *AiChecksMockBuilder.cs*
- `ThatApprovesUser()` in *UserManagerMockBuilder.cs*
- `ThatBansUsers()` in *ModerationServiceMockBuilder.cs*
- `ThatDeletesMessages()` in *MessageHandlerMockBuilder.cs*
- `ThatDeletesMessages()` in *ModerationServiceMockBuilder.cs*
- `ThatDeletesMessagesSuccessfully()` in *TelegramBotMockBuilder.cs*
- `ThatFails()` in *CaptchaServiceMockBuilder.cs*
- `ThatForwardsMessages()` in *TelegramBotMockBuilder.cs*
- `ThatIsInBanlist()` in *UserManagerMockBuilder.cs*
- `ThatIsNotInBanlist()` in *UserManagerMockBuilder.cs*
- `ThatRejectsPhoto()` in *AiChecksMockBuilder.cs*
- `ThatRejectsUser()` in *UserManagerMockBuilder.cs*
- `ThatReportsToLogChat()` in *MessageHandlerMockBuilder.cs*
- `ThatReportsWithoutDeleting()` in *MessageHandlerMockBuilder.cs*
- `ThatReturns()` in *ModerationServiceMockBuilder.cs*
- `ThatSendsAdminNotification()` in *MessageServiceMockBuilder.cs*
- `ThatSendsMessageSuccessfully()` in *TelegramBotMockBuilder.cs*
- `ThatSendsSuspiciousMessageWithButtons()` in *MessageHandlerMockBuilder.cs*
- `ThatSendsUserNotification()` in *MessageServiceMockBuilder.cs*
- `ThatSendsUserNotificationWithReply()` in *MessageServiceMockBuilder.cs*
- `ThatSucceeds()` in *CaptchaServiceMockBuilder.cs*
- `ThatThrowsException()` in *MessageHandlerMockBuilder.cs*
- `ThatThrowsException()` in *MessageServiceMockBuilder.cs*
- `ThatThrowsOnDelete()` in *TelegramBotMockBuilder.cs*
- `ThatThrowsOnSend()` in *TelegramBotMockBuilder.cs*
- `UserManagerMockBuilder_ThatApprovesUser_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `WithAction()` in *TestKit.Builders.cs*
- `WithAction()` in *ModerationResultBuilder.cs*
- `WithAiChecks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithAiMlMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithBanMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithBlacklistedUserScenario()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithCaptchaScenario()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithCaptchaService()` in *TestKit.MessageHandlerBuilder.cs*
- `WithChannelMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithConfidence()` in *TestKit.Builders.cs*
- `WithConfidence()` in *ModerationResultBuilder.cs*
- `WithConfidence()` in *ModerationServiceMockBuilder.cs*
- `WithFirstName()` in *TestKit.Builders.cs*
- `WithFirstName()` in *UserBuilder.cs*
- `WithId()` in *TestKit.Builders.cs*
- `WithId()` in *TestKit.Builders.cs*
- `WithId()` in *UserBuilder.cs*
- `WithId()` in *ChatBuilder.cs*
- `WithMessageId()` in *TestKit.Builders.cs*
- `WithModerationMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithModerationService()` in *TestKit.MessageHandlerBuilder.cs*
- `WithReason()` in *TestKit.Builders.cs*
- `WithReason()` in *ModerationResultBuilder.cs*
- `WithReason()` in *ModerationServiceMockBuilder.cs*
- `WithStandardMocks()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithStandardMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithStandardMocks()` in *TestKit.NotificationServiceBuilder.cs*
- `WithSuccessfulJoinScenario()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithTelegramBot()` in *TestKit.MessageHandlerBuilder.cs*
- `WithText()` in *TestKit.Builders.cs*
- `WithText()` in *MessageBuilder.cs*
- `WithTitle()` in *TestKit.Builders.cs*
- `WithTitle()` in *ChatBuilder.cs*
- `WithType()` in *TestKit.Builders.cs*
- `WithType()` in *ChatBuilder.cs*
- `WithUserManager()` in *TestKit.MessageHandlerBuilder.cs*
- `WithUsername()` in *TestKit.Builders.cs*
- `WithUsername()` in *UserBuilder.cs*

### 🏷️ captcha
- `CaptchaServiceMockBuilder_ThatSucceeds_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `CreateBaitCaptchaInfo()` in *TestKit.Main.cs*
- `CreateCaptchaService()` in *TestKit.AutoFixture.cs*
- `CreateCaptchaService()` in *TestKitAutoFixture.cs*
- `CreateCaptchaServiceFactory()` in *TestKit.Facade.cs*
- `CreateCaptchaServiceMock()` in *TestKitMockBuilders.cs*
- `CreateCorrectCaptchaResult()` in *TestKit.Main.cs*
- `CreateExpiredCaptchaInfo()` in *TestKit.Main.cs*
- `CreateFailedCaptchaService()` in *TestKit.Mocks.cs*
- `CreateIncorrectCaptchaResult()` in *TestKit.Main.cs*
- `CreateMockCaptchaService()` in *TestKit.Mocks.cs*
- `CreateSuccessfulCaptchaService()` in *TestKit.Mocks.cs*
- `CreateValidCaptchaInfo()` in *TestKit.Main.cs*
- `WithCaptchaScenario()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithCaptchaService()` in *TestKit.MessageHandlerBuilder.cs*

### 🏷️ chat
- `AsGroup()` in *TestKit.Builders.cs*
- `AsGroup()` in *ChatBuilder.cs*
- `AsPrivate()` in *TestKit.Builders.cs*
- `AsPrivate()` in *ChatBuilder.cs*
- `AsSupergroup()` in *TestKit.Builders.cs*
- `AsSupergroup()` in *ChatBuilder.cs*
- `Build()` in *TestKit.Builders.cs*
- `Build()` in *ChatBuilder.cs*
- `ChatForBanTest()` in *TestKit.Specialized.cs*
- `CreateBotPermissionsServiceMockForChat()` in *TestKit.Mocks.cs*
- `CreateChannel()` in *TestKit.Main.cs*
- `CreateChat()` in *TestKit.Builders.cs*
- `CreateChat()` in *TestKitBuilders.cs*
- `CreateChatMemberHandlerFactory()` in *TestKit.Facade.cs*
- `CreateChatMemberUpdate()` in *TestKit.Main.cs*
- `CreateGroupChat()` in *TestKit.Main.cs*
- `CreateMemberBanned()` in *TestKit.Main.cs*
- `CreateMemberDemoted()` in *TestKit.Main.cs*
- `CreateMemberJoined()` in *TestKit.Main.cs*
- `CreateMemberLeft()` in *TestKit.Main.cs*
- `CreateMemberPromoted()` in *TestKit.Main.cs*
- `CreateMemberRestricted()` in *TestKit.Main.cs*
- `CreatePrivateChat()` in *TestKit.Main.cs*
- `CreatePrivateChatBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreateRealisticChannel()` in *TestKit.Bogus.cs*
- `CreateRealisticChannel()` in *TestKitBogus.cs*
- `CreateRealisticGroup()` in *TestKit.Bogus.cs*
- `CreateRealisticGroup()` in *TestKitBogus.cs*
- `CreateRealisticPrivateChat()` in *TestKit.Bogus.cs*
- `CreateRealisticPrivateChat()` in *TestKitBogus.cs*
- `CreateRealisticSupergroup()` in *TestKit.Bogus.cs*
- `CreateRealisticSupergroup()` in *TestKitBogus.cs*
- `CreateServiceChatDispatcherFactory()` in *TestKit.Facade.cs*
- `CreateSupergroupChat()` in *TestKit.Main.cs*
- `CreateTestChat()` in *TestKit.Mocks.cs*
- `ForBanTest()` in *TestKit.Specialized.cs*
- `ForChannelTest()` in *TestKit.Specialized.cs*
- `InChat()` in *TestKit.Builders.cs*
- `InChat()` in *TestKit.Builders.cs*
- `InChat()` in *MessageBuilder.cs*
- `InChat()` in *MessageBuilder.cs*
- `MemberBanned()` in *TestKit.Specialized.cs*
- `MemberDemoted()` in *TestKit.Specialized.cs*
- `MemberJoined()` in *TestKit.Specialized.cs*
- `MemberLeft()` in *TestKit.Specialized.cs*
- `MemberPromoted()` in *TestKit.Specialized.cs*
- `MemberRestricted()` in *TestKit.Specialized.cs*
- `ThatReportsToLogChat()` in *MessageHandlerMockBuilder.cs*
- `WithId()` in *TestKit.Builders.cs*
- `WithId()` in *ChatBuilder.cs*
- `WithTitle()` in *TestKit.Builders.cs*
- `WithTitle()` in *ChatBuilder.cs*
- `WithType()` in *TestKit.Builders.cs*
- `WithType()` in *ChatBuilder.cs*

### 🏷️ collection
- `CreateConversation()` in *TestKit.Bogus.cs*
- `CreateConversation()` in *TestKitBogus.cs*
- `CreateExceptionScenarioSet()` in *TestKit.GoldenMaster.cs*
- `CreateManyMessages()` in *TestKit.AutoFixture.cs*
- `CreateManyMessages()` in *TestKitAutoFixture.cs*
- `CreateManySpamMessages()` in *TestKit.AutoFixture.cs*
- `CreateManySpamMessages()` in *TestKitAutoFixture.cs*
- `CreateManyUsers()` in *TestKit.AutoFixture.cs*
- `CreateManyUsers()` in *TestKitAutoFixture.cs*
- `CreateRealisticMessages()` in *TestKit.AutoFixture.cs*
- `CreateRealisticMessages()` in *TestKitAutoFixture.cs*
- `CreateRealisticUsers()` in *TestKit.AutoFixture.cs*
- `CreateRealisticUsers()` in *TestKitAutoFixture.cs*
- `CreateScenarioSet()` in *TestKit.GoldenMaster.cs*
- `CreateUserList()` in *TestKit.Bogus.cs*
- `CreateUserList()` in *TestKitBogus.cs*

### 🏷️ factory
- `AiChecksMockBuilder_ThatApprovesPhoto_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `CaptchaServiceMockBuilder_ThatSucceeds_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `CreateAdminApproveCallback()` in *TestKit.Main.cs*
- `CreateAdminBanCallback()` in *TestKit.Main.cs*
- `CreateAdminNotificationMessage()` in *TestKit.Main.cs*
- `CreateAdminSkipCallback()` in *TestKit.Main.cs*
- `CreateAiChecksFactory()` in *TestKit.Facade.cs*
- `CreateAiChecksMock()` in *TestKitMockBuilders.cs*
- `CreateAllowResult()` in *TestKit.Main.cs*
- `CreateAnonymousUser()` in *TestKit.Main.cs*
- `CreateAppConfig()` in *TestKit.Facade.cs*
- `CreateAppConfigWithoutAi()` in *TestKit.Facade.cs*
- `CreateApprovedUserManager()` in *TestKit.Mocks.cs*
- `CreateBaitCaptchaInfo()` in *TestKit.Main.cs*
- `CreateBaitUser()` in *TestKit.Main.cs*
- `CreateBanModerationService()` in *TestKit.Mocks.cs*
- `CreateBanResult()` in *TestKit.Main.cs*
- `CreateBanTriggeringViolationTracker()` in *TestKit.Mocks.cs*
- `CreateBotBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreateBotPermissionsServiceMock()` in *TestKit.Mocks.cs*
- `CreateBotPermissionsServiceMockForChat()` in *TestKit.Mocks.cs*
- `CreateBotUser()` in *TestKit.Main.cs*
- `CreateCallbackQueryHandlerFactory()` in *TestKit.Facade.cs*
- `CreateCallbackQueryUpdate()` in *TestKit.Main.cs*
- `CreateCaptchaService()` in *TestKit.AutoFixture.cs*
- `CreateCaptchaService()` in *TestKitAutoFixture.cs*
- `CreateCaptchaServiceFactory()` in *TestKit.Facade.cs*
- `CreateCaptchaServiceMock()` in *TestKitMockBuilders.cs*
- `CreateChannel()` in *TestKit.Main.cs*
- `CreateChannelMessage()` in *TestKit.Main.cs*
- `CreateChat()` in *TestKit.Builders.cs*
- `CreateChat()` in *TestKitBuilders.cs*
- `CreateChatMemberHandlerFactory()` in *TestKit.Facade.cs*
- `CreateChatMemberUpdate()` in *TestKit.Main.cs*
- `CreateConversation()` in *TestKit.Bogus.cs*
- `CreateConversation()` in *TestKitBogus.cs*
- `CreateCorrectCaptchaResult()` in *TestKit.Main.cs*
- `CreateDeleteModerationService()` in *TestKit.Mocks.cs*
- `CreateDeleteResult()` in *TestKit.Main.cs*
- `CreateEmptyMessage()` in *TestKit.Main.cs*
- `CreateErrorAiChecks()` in *TestKit.Mocks.cs*
- `CreateExceptionScenarioSet()` in *TestKit.GoldenMaster.cs*
- `CreateExpiredCaptchaInfo()` in *TestKit.Main.cs*
- `CreateFailedCaptchaService()` in *TestKit.Mocks.cs*
- `CreateFakeClient()` in *TestKit.Telegram.cs*
- `CreateFakeClient()` in *TestKit.Facade.cs*
- `CreateFixture()` in *TestKit.AutoFixture.cs*
- `CreateFixture()` in *TestKitAutoFixture.cs*
- `CreateGroupChat()` in *TestKit.Main.cs*
- `CreateHelpCommandMessage()` in *TestKit.Main.cs*
- `CreateIncorrectCaptchaResult()` in *TestKit.Main.cs*
- `CreateInvalidCallbackQuery()` in *TestKit.Main.cs*
- `CreateLongMessage()` in *TestKit.Main.cs*
- `CreateManyMessages()` in *TestKit.AutoFixture.cs*
- `CreateManyMessages()` in *TestKitAutoFixture.cs*
- `CreateManySpamMessages()` in *TestKit.AutoFixture.cs*
- `CreateManySpamMessages()` in *TestKitAutoFixture.cs*
- `CreateManyUsers()` in *TestKit.AutoFixture.cs*
- `CreateManyUsers()` in *TestKitAutoFixture.cs*
- `CreateMediaMessage()` in *TestKit.Bogus.cs*
- `CreateMediaMessage()` in *TestKitBogus.cs*
- `CreateMemberBanned()` in *TestKit.Main.cs*
- `CreateMemberDemoted()` in *TestKit.Main.cs*
- `CreateMemberJoined()` in *TestKit.Main.cs*
- `CreateMemberLeft()` in *TestKit.Main.cs*
- `CreateMemberPromoted()` in *TestKit.Main.cs*
- `CreateMemberRestricted()` in *TestKit.Main.cs*
- `CreateMessage()` in *TestKit.Builders.cs*
- `CreateMessage()` in *TestKitBuilders.cs*
- `CreateMessageFromEnvelope()` in *TestKit.Telegram.cs*
- `CreateMessageHandler()` in *TestKit.AutoFixture.cs*
- `CreateMessageHandler()` in *TestKitAutoFixture.cs*
- `CreateMessageHandlerBuilder()` in *TestKit.Facade.cs*
- `CreateMessageHandlerFactory()` in *TestKit.Facade.cs*
- `CreateMessageHandlerMock()` in *TestKitMockBuilders.cs*
- `CreateMessageHandlerWithDefaults()` in *TestKit.Facade.cs*
- `CreateMessageHandlerWithFake()` in *TestKit.Facade.cs*
- `CreateMessageServiceMock()` in *TestKitMockBuilders.cs*
- `CreateMessageUpdate()` in *TestKit.Main.cs*
- `CreateMimicryClassifierFactory()` in *TestKit.Facade.cs*
- `CreateMockAppConfig()` in *TestKit.Mocks.cs*
- `CreateMockBotClient()` in *TestKit.Mocks.cs*
- `CreateMockBotClientWrapper()` in *TestKit.Mocks.cs*
- `CreateMockBotPermissionsService()` in *TestKit.Mocks.cs*
- `CreateMockCaptchaService()` in *TestKit.Mocks.cs*
- `CreateMockMessageService()` in *TestKit.Mocks.cs*
- `CreateMockModerationService()` in *TestKit.Mocks.cs*
- `CreateMockServiceProvider()` in *TestKit.Mocks.cs*
- `CreateMockSpamHamClassifier()` in *TestKit.Mocks.cs*
- `CreateMockStatisticsService()` in *TestKit.Mocks.cs*
- `CreateMockUserBanService()` in *TestKit.Mocks.cs*
- `CreateMockUserManager()` in *TestKit.Mocks.cs*
- `CreateMockViolationTracker()` in *TestKit.Mocks.cs*
- `CreateModerationResult()` in *TestKit.Builders.cs*
- `CreateModerationResult()` in *TestKitBuilders.cs*
- `CreateModerationService()` in *TestKit.AutoFixture.cs*
- `CreateModerationService()` in *TestKitAutoFixture.cs*
- `CreateModerationServiceFactory()` in *TestKit.Facade.cs*
- `CreateModerationServiceMock()` in *TestKitMockBuilders.cs*
- `CreateNewUserJoinMessage()` in *TestKit.Main.cs*
- `CreateNormalAiChecks()` in *TestKit.Mocks.cs*
- `CreateNotificationServiceBuilder()` in *TestKit.Facade.cs*
- `CreateNullMessageBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreateNullTextMessage()` in *TestKit.Main.cs*
- `CreatePermanentBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreatePrivateChat()` in *TestKit.Main.cs*
- `CreatePrivateChatBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreateRandomDate()` in *TestKit.Bogus.cs*
- `CreateRandomDate()` in *TestKitBogus.cs*
- `CreateRandomUrl()` in *TestKit.Bogus.cs*
- `CreateRandomUrl()` in *TestKitBogus.cs*
- `CreateRealisticBot()` in *TestKit.Bogus.cs*
- `CreateRealisticBot()` in *TestKitBogus.cs*
- `CreateRealisticChannel()` in *TestKit.Bogus.cs*
- `CreateRealisticChannel()` in *TestKitBogus.cs*
- `CreateRealisticGroup()` in *TestKit.Bogus.cs*
- `CreateRealisticGroup()` in *TestKitBogus.cs*
- `CreateRealisticMessage()` in *TestKit.Bogus.cs*
- `CreateRealisticMessage()` in *TestKitBogus.cs*
- `CreateRealisticMessages()` in *TestKit.AutoFixture.cs*
- `CreateRealisticMessages()` in *TestKitAutoFixture.cs*
- `CreateRealisticPrivateChat()` in *TestKit.Bogus.cs*
- `CreateRealisticPrivateChat()` in *TestKitBogus.cs*
- `CreateRealisticSpamMessage()` in *TestKit.Bogus.cs*
- `CreateRealisticSpamMessage()` in *TestKitBogus.cs*
- `CreateRealisticSupergroup()` in *TestKit.Bogus.cs*
- `CreateRealisticSupergroup()` in *TestKitBogus.cs*
- `CreateRealisticUser()` in *TestKit.Bogus.cs*
- `CreateRealisticUser()` in *TestKitBogus.cs*
- `CreateRealisticUsers()` in *TestKit.AutoFixture.cs*
- `CreateRealisticUsers()` in *TestKitAutoFixture.cs*
- `CreateRussianText()` in *TestKit.Bogus.cs*
- `CreateRussianText()` in *TestKitBogus.cs*
- `CreateScenarioSet()` in *TestKit.GoldenMaster.cs*
- `CreateServiceChatDispatcherFactory()` in *TestKit.Facade.cs*
- `CreateSpamAiChecks()` in *TestKit.Mocks.cs*
- `CreateSpamHamClassifier()` in *TestKit.Mocks.cs*
- `CreateSpamHamClassifierFactory()` in *TestKit.Facade.cs*
- `CreateSpamMessage()` in *TestKit.Main.cs*
- `CreateSpamMessage()` in *TestKit.Bogus.cs*
- `CreateSpamMessage()` in *TestKitBogus.cs*
- `CreateStatisticsServiceFactory()` in *TestKit.Facade.cs*
- `CreateStatsCommandMessage()` in *TestKit.Main.cs*
- `CreateSuccessfulCaptchaService()` in *TestKit.Mocks.cs*
- `CreateSupergroupChat()` in *TestKit.Main.cs*
- `CreateSuspiciousUser()` in *TestKit.Bogus.cs*
- `CreateSuspiciousUser()` in *TestKitBogus.cs*
- `CreateSuspiciousUserAiChecks()` in *TestKit.Mocks.cs*
- `CreateSuspiciousUserMessage()` in *TestKit.Main.cs*
- `CreateTelegramBotMock()` in *TestKitMockBuilders.cs*
- `CreateTemporaryBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreateTestBotClient()` in *TestKit.Mocks.cs*
- `CreateTestBotClient_ReturnsValidClient()` in *TestKit.BuilderTests.cs*
- `CreateTestChat()` in *TestKit.Mocks.cs*
- `CreateTestMessage()` in *TestKit.Mocks.cs*
- `CreateTestToken()` in *TestKit.Mocks.cs*
- `CreateTestToken_ReturnsConsistentToken()` in *TestKit.BuilderTests.cs*
- `CreateTestUpdate()` in *TestKit.Mocks.cs*
- `CreateTestUser()` in *TestKit.Mocks.cs*
- `CreateTextMessage()` in *TestKit.Main.cs*
- `CreateUnapprovedUserManager()` in *TestKit.Mocks.cs*
- `CreateUpdateFromEnvelope()` in *TestKit.Telegram.cs*
- `CreateUser()` in *TestKit.Builders.cs*
- `CreateUser()` in *TestKitBuilders.cs*
- `CreateUserJoinServiceBuilder()` in *TestKit.Facade.cs*
- `CreateUserList()` in *TestKit.Bogus.cs*
- `CreateUserList()` in *TestKitBogus.cs*
- `CreateUserManager()` in *TestKit.AutoFixture.cs*
- `CreateUserManager()` in *TestKitAutoFixture.cs*
- `CreateUserManagerMock()` in *TestKitMockBuilders.cs*
- `CreateValidCallbackQuery()` in *TestKit.Main.cs*
- `CreateValidCaptchaInfo()` in *TestKit.Main.cs*
- `CreateValidMessage()` in *TestKit.Main.cs*
- `CreateValidMessageWithId()` in *TestKit.Main.cs*
- `CreateValidUser()` in *TestKit.Main.cs*
- `MessageHandlerBuilder_ComplexScenario_CreatesValidHandler()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithBanMocks_CreatesHandlerWithBanConfiguration()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithModerationService_CreatesHandlerWithCustomModeration()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithStandardMocks_CreatesValidHandler()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithUserManager_CreatesHandlerWithCustomUserManager()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatAllowsMessages_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatBansUsers_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `SetupGoldenMasterMocks()` in *TestKit.GoldenMaster.cs*
- `TelegramBotMockBuilder_ThatSendsMessageSuccessfully_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `UserManagerMockBuilder_ThatApprovesUser_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*

### 🏷️ fake
- `CreateFakeClient()` in *TestKit.Telegram.cs*
- `CreateFakeClient()` in *TestKit.Facade.cs*
- `CreateMessageHandlerWithFake()` in *TestKit.Facade.cs*
- `GetFaker()` in *TestKit.Bogus.cs*
- `GetFaker()` in *TestKitBogus.cs*

### 🏷️ golden-master
- `SetupGoldenMasterMocks()` in *TestKit.GoldenMaster.cs*

### 🏷️ invalid
- `CreateInvalidCallbackQuery()` in *TestKit.Main.cs*
- `Invalid()` in *TestKit.Specialized.cs*

### 🏷️ message
- `AsSpam()` in *TestKit.Builders.cs*
- `AsSpam()` in *MessageBuilder.cs*
- `AsValid()` in *TestKit.Builders.cs*
- `AsValid()` in *MessageBuilder.cs*
- `Build()` in *TestKit.MessageHandlerBuilder.cs*
- `Build()` in *TestKit.Builders.cs*
- `Build()` in *MessageBuilder.cs*
- `Build()` in *MessageHandlerMockBuilder.cs*
- `Build()` in *MessageServiceMockBuilder.cs*
- `BuildMock()` in *TestKit.MessageHandlerBuilder.cs*
- `BuildObject()` in *MessageHandlerMockBuilder.cs*
- `BuildObject()` in *MessageServiceMockBuilder.cs*
- `ChannelMessage()` in *TestKit.Specialized.cs*
- `CreateAdminNotificationMessage()` in *TestKit.Main.cs*
- `CreateChannelMessage()` in *TestKit.Main.cs*
- `CreateConversation()` in *TestKit.Bogus.cs*
- `CreateConversation()` in *TestKitBogus.cs*
- `CreateEmptyMessage()` in *TestKit.Main.cs*
- `CreateHelpCommandMessage()` in *TestKit.Main.cs*
- `CreateLongMessage()` in *TestKit.Main.cs*
- `CreateManyMessages()` in *TestKit.AutoFixture.cs*
- `CreateManyMessages()` in *TestKitAutoFixture.cs*
- `CreateManySpamMessages()` in *TestKit.AutoFixture.cs*
- `CreateManySpamMessages()` in *TestKitAutoFixture.cs*
- `CreateMediaMessage()` in *TestKit.Bogus.cs*
- `CreateMediaMessage()` in *TestKitBogus.cs*
- `CreateMessage()` in *TestKit.Builders.cs*
- `CreateMessage()` in *TestKitBuilders.cs*
- `CreateMessageFromEnvelope()` in *TestKit.Telegram.cs*
- `CreateMessageHandler()` in *TestKit.AutoFixture.cs*
- `CreateMessageHandler()` in *TestKitAutoFixture.cs*
- `CreateMessageHandlerBuilder()` in *TestKit.Facade.cs*
- `CreateMessageHandlerFactory()` in *TestKit.Facade.cs*
- `CreateMessageHandlerMock()` in *TestKitMockBuilders.cs*
- `CreateMessageHandlerWithDefaults()` in *TestKit.Facade.cs*
- `CreateMessageHandlerWithFake()` in *TestKit.Facade.cs*
- `CreateMessageServiceMock()` in *TestKitMockBuilders.cs*
- `CreateMessageUpdate()` in *TestKit.Main.cs*
- `CreateMockMessageService()` in *TestKit.Mocks.cs*
- `CreateNewUserJoinMessage()` in *TestKit.Main.cs*
- `CreateNullMessageBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreateNullTextMessage()` in *TestKit.Main.cs*
- `CreateRealisticMessage()` in *TestKit.Bogus.cs*
- `CreateRealisticMessage()` in *TestKitBogus.cs*
- `CreateRealisticMessages()` in *TestKit.AutoFixture.cs*
- `CreateRealisticMessages()` in *TestKitAutoFixture.cs*
- `CreateRealisticSpamMessage()` in *TestKit.Bogus.cs*
- `CreateRealisticSpamMessage()` in *TestKitBogus.cs*
- `CreateSpamMessage()` in *TestKit.Main.cs*
- `CreateSpamMessage()` in *TestKit.Bogus.cs*
- `CreateSpamMessage()` in *TestKitBogus.cs*
- `CreateStatsCommandMessage()` in *TestKit.Main.cs*
- `CreateSuspiciousUserMessage()` in *TestKit.Main.cs*
- `CreateTestMessage()` in *TestKit.Mocks.cs*
- `CreateTextMessage()` in *TestKit.Main.cs*
- `CreateValidMessage()` in *TestKit.Main.cs*
- `CreateValidMessageWithId()` in *TestKit.Main.cs*
- `FromUser()` in *TestKit.Builders.cs*
- `FromUser()` in *TestKit.Builders.cs*
- `FromUser()` in *MessageBuilder.cs*
- `FromUser()` in *MessageBuilder.cs*
- `HelpCommand()` in *TestKit.Specialized.cs*
- `InChat()` in *TestKit.Builders.cs*
- `InChat()` in *TestKit.Builders.cs*
- `InChat()` in *MessageBuilder.cs*
- `InChat()` in *MessageBuilder.cs*
- `MessageHandlerBuilder_ComplexScenario_CreatesValidHandler()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithBanMocks_CreatesHandlerWithBanConfiguration()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithModerationService_CreatesHandlerWithCustomModeration()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithStandardMocks_CreatesValidHandler()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithUserManager_CreatesHandlerWithCustomUserManager()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatAllowsMessages_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `NewUserJoin()` in *TestKit.Specialized.cs*
- `Notification()` in *TestKit.Specialized.cs*
- `ResetMessageIdCounter()` in *TestKit.Telegram.cs*
- `ResetMessageIdCounter()` in *TestKit.Facade.cs*
- `SetNextMessageId()` in *TestKit.Telegram.cs*
- `SetupGoldenMasterMocks()` in *TestKit.GoldenMaster.cs*
- `SpamMessage()` in *TestKit.Specialized.cs*
- `StatsCommand()` in *TestKit.Specialized.cs*
- `SuspiciousUser()` in *TestKit.Specialized.cs*
- `TelegramBotMockBuilder_ThatSendsMessageSuccessfully_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `ThatAllowsMessages()` in *ModerationServiceMockBuilder.cs*
- `ThatDeletesMessages()` in *MessageHandlerMockBuilder.cs*
- `ThatDeletesMessages()` in *ModerationServiceMockBuilder.cs*
- `ThatDeletesMessagesSuccessfully()` in *TelegramBotMockBuilder.cs*
- `ThatForwardsMessages()` in *TelegramBotMockBuilder.cs*
- `ThatReportsToLogChat()` in *MessageHandlerMockBuilder.cs*
- `ThatReportsWithoutDeleting()` in *MessageHandlerMockBuilder.cs*
- `ThatSendsAdminNotification()` in *MessageServiceMockBuilder.cs*
- `ThatSendsMessageSuccessfully()` in *TelegramBotMockBuilder.cs*
- `ThatSendsSuspiciousMessageWithButtons()` in *MessageHandlerMockBuilder.cs*
- `ThatSendsUserNotification()` in *MessageServiceMockBuilder.cs*
- `ThatSendsUserNotificationWithReply()` in *MessageServiceMockBuilder.cs*
- `ThatThrowsException()` in *MessageHandlerMockBuilder.cs*
- `ThatThrowsException()` in *MessageServiceMockBuilder.cs*
- `WithAiChecks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithAiMlMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithBanMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithCaptchaService()` in *TestKit.MessageHandlerBuilder.cs*
- `WithChannelMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithMessageId()` in *TestKit.Builders.cs*
- `WithModerationMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithModerationService()` in *TestKit.MessageHandlerBuilder.cs*
- `WithStandardMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithTelegramBot()` in *TestKit.MessageHandlerBuilder.cs*
- `WithText()` in *TestKit.Builders.cs*
- `WithText()` in *MessageBuilder.cs*
- `WithUserManager()` in *TestKit.MessageHandlerBuilder.cs*

### 🏷️ mock
- `AiChecksMockBuilder_ThatApprovesPhoto_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `Build()` in *TelegramBotMockBuilder.cs*
- `Build()` in *MessageHandlerMockBuilder.cs*
- `Build()` in *CaptchaServiceMockBuilder.cs*
- `Build()` in *AiChecksMockBuilder.cs*
- `Build()` in *ModerationServiceMockBuilder.cs*
- `Build()` in *MessageServiceMockBuilder.cs*
- `Build()` in *UserManagerMockBuilder.cs*
- `BuildMock()` in *TestKit.MessageHandlerBuilder.cs*
- `CaptchaServiceMockBuilder_ThatSucceeds_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `CreateAiChecksMock()` in *TestKitMockBuilders.cs*
- `CreateApprovedUserManager()` in *TestKit.Mocks.cs*
- `CreateBanModerationService()` in *TestKit.Mocks.cs*
- `CreateBanTriggeringViolationTracker()` in *TestKit.Mocks.cs*
- `CreateBotPermissionsServiceMock()` in *TestKit.Mocks.cs*
- `CreateBotPermissionsServiceMockForChat()` in *TestKit.Mocks.cs*
- `CreateCaptchaServiceMock()` in *TestKitMockBuilders.cs*
- `CreateDeleteModerationService()` in *TestKit.Mocks.cs*
- `CreateErrorAiChecks()` in *TestKit.Mocks.cs*
- `CreateFailedCaptchaService()` in *TestKit.Mocks.cs*
- `CreateMessageHandlerMock()` in *TestKitMockBuilders.cs*
- `CreateMessageServiceMock()` in *TestKitMockBuilders.cs*
- `CreateMockAppConfig()` in *TestKit.Mocks.cs*
- `CreateMockBotClient()` in *TestKit.Mocks.cs*
- `CreateMockBotClientWrapper()` in *TestKit.Mocks.cs*
- `CreateMockBotPermissionsService()` in *TestKit.Mocks.cs*
- `CreateMockCaptchaService()` in *TestKit.Mocks.cs*
- `CreateMockMessageService()` in *TestKit.Mocks.cs*
- `CreateMockModerationService()` in *TestKit.Mocks.cs*
- `CreateMockServiceProvider()` in *TestKit.Mocks.cs*
- `CreateMockSpamHamClassifier()` in *TestKit.Mocks.cs*
- `CreateMockStatisticsService()` in *TestKit.Mocks.cs*
- `CreateMockUserBanService()` in *TestKit.Mocks.cs*
- `CreateMockUserManager()` in *TestKit.Mocks.cs*
- `CreateMockViolationTracker()` in *TestKit.Mocks.cs*
- `CreateModerationServiceMock()` in *TestKitMockBuilders.cs*
- `CreateNormalAiChecks()` in *TestKit.Mocks.cs*
- `CreateSpamAiChecks()` in *TestKit.Mocks.cs*
- `CreateSuccessfulCaptchaService()` in *TestKit.Mocks.cs*
- `CreateSuspiciousUserAiChecks()` in *TestKit.Mocks.cs*
- `CreateTelegramBotMock()` in *TestKitMockBuilders.cs*
- `CreateUnapprovedUserManager()` in *TestKit.Mocks.cs*
- `CreateUserManagerMock()` in *TestKitMockBuilders.cs*
- `MessageHandlerBuilder_WithBanMocks_CreatesHandlerWithBanConfiguration()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithStandardMocks_CreatesValidHandler()` in *TestKit.BuilderTests.cs*
- `ModerationScenarios_MockedSetup_WorksCorrectly()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatAllowsMessages_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatBansUsers_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `SetupGoldenMasterMocks()` in *TestKit.GoldenMaster.cs*
- `TelegramBotMockBuilder_ThatSendsMessageSuccessfully_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `ThatAllowsMessages()` in *ModerationServiceMockBuilder.cs*
- `ThatApprovesPhoto()` in *AiChecksMockBuilder.cs*
- `ThatApprovesUser()` in *UserManagerMockBuilder.cs*
- `ThatBansUsers()` in *ModerationServiceMockBuilder.cs*
- `ThatDeletesMessages()` in *MessageHandlerMockBuilder.cs*
- `ThatDeletesMessages()` in *ModerationServiceMockBuilder.cs*
- `ThatDeletesMessagesSuccessfully()` in *TelegramBotMockBuilder.cs*
- `ThatFails()` in *CaptchaServiceMockBuilder.cs*
- `ThatForwardsMessages()` in *TelegramBotMockBuilder.cs*
- `ThatIsInBanlist()` in *UserManagerMockBuilder.cs*
- `ThatIsNotInBanlist()` in *UserManagerMockBuilder.cs*
- `ThatRejectsPhoto()` in *AiChecksMockBuilder.cs*
- `ThatRejectsUser()` in *UserManagerMockBuilder.cs*
- `ThatReportsToLogChat()` in *MessageHandlerMockBuilder.cs*
- `ThatReportsWithoutDeleting()` in *MessageHandlerMockBuilder.cs*
- `ThatReturns()` in *ModerationServiceMockBuilder.cs*
- `ThatSendsAdminNotification()` in *MessageServiceMockBuilder.cs*
- `ThatSendsMessageSuccessfully()` in *TelegramBotMockBuilder.cs*
- `ThatSendsSuspiciousMessageWithButtons()` in *MessageHandlerMockBuilder.cs*
- `ThatSendsUserNotification()` in *MessageServiceMockBuilder.cs*
- `ThatSendsUserNotificationWithReply()` in *MessageServiceMockBuilder.cs*
- `ThatSucceeds()` in *CaptchaServiceMockBuilder.cs*
- `ThatThrowsException()` in *MessageHandlerMockBuilder.cs*
- `ThatThrowsException()` in *MessageServiceMockBuilder.cs*
- `ThatThrowsOnDelete()` in *TelegramBotMockBuilder.cs*
- `ThatThrowsOnSend()` in *TelegramBotMockBuilder.cs*
- `UserManagerMockBuilder_ThatApprovesUser_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `WithAiMlMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithBanMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithChannelMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithConfidence()` in *ModerationServiceMockBuilder.cs*
- `WithModerationMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithReason()` in *ModerationServiceMockBuilder.cs*
- `WithStandardMocks()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithStandardMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithStandardMocks()` in *TestKit.NotificationServiceBuilder.cs*

### 🏷️ moderation
- `CreateBanModerationService()` in *TestKit.Mocks.cs*
- `CreateDeleteModerationService()` in *TestKit.Mocks.cs*
- `CreateMockModerationService()` in *TestKit.Mocks.cs*
- `CreateModerationResult()` in *TestKit.Builders.cs*
- `CreateModerationResult()` in *TestKitBuilders.cs*
- `CreateModerationService()` in *TestKit.AutoFixture.cs*
- `CreateModerationService()` in *TestKitAutoFixture.cs*
- `CreateModerationServiceFactory()` in *TestKit.Facade.cs*
- `CreateModerationServiceMock()` in *TestKitMockBuilders.cs*
- `MessageHandlerBuilder_WithModerationService_CreatesHandlerWithCustomModeration()` in *TestKit.BuilderTests.cs*
- `ModerationScenarios_CompleteSetup_WorksCorrectly()` in *TestKit.BuilderTests.cs*
- `ModerationScenarios_MinimalSetup_WorksCorrectly()` in *TestKit.BuilderTests.cs*
- `ModerationScenarios_MockedSetup_WorksCorrectly()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatAllowsMessages_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatBansUsers_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `WithModerationMocks()` in *TestKit.MessageHandlerBuilder.cs*
- `WithModerationService()` in *TestKit.MessageHandlerBuilder.cs*

### 🏷️ scenario
- `CreateBotBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreateExceptionScenarioSet()` in *TestKit.GoldenMaster.cs*
- `CreateNullMessageBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreatePermanentBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreatePrivateChatBanScenario()` in *TestKit.GoldenMaster.cs*
- `CreateScenarioSet()` in *TestKit.GoldenMaster.cs*
- `CreateTemporaryBanScenario()` in *TestKit.GoldenMaster.cs*
- `MessageHandlerBuilder_ComplexScenario_CreatesValidHandler()` in *TestKit.BuilderTests.cs*
- `ModerationScenarios_CompleteSetup_WorksCorrectly()` in *TestKit.BuilderTests.cs*
- `ModerationScenarios_MinimalSetup_WorksCorrectly()` in *TestKit.BuilderTests.cs*
- `ModerationScenarios_MockedSetup_WorksCorrectly()` in *TestKit.BuilderTests.cs*
- `WithBlacklistedUserScenario()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithCaptchaScenario()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithSuccessfulJoinScenario()` in *TestKit.UserJoinServiceBuilder.cs*

### 🏷️ setup
- `CompleteSetup()` in *TestKit.Specialized.cs*
- `MinimalSetup()` in *TestKit.Specialized.cs*
- `ModerationScenarios_CompleteSetup_WorksCorrectly()` in *TestKit.BuilderTests.cs*
- `ModerationScenarios_MinimalSetup_WorksCorrectly()` in *TestKit.BuilderTests.cs*
- `ModerationScenarios_MockedSetup_WorksCorrectly()` in *TestKit.BuilderTests.cs*
- `SetupGoldenMasterMocks()` in *TestKit.GoldenMaster.cs*

### 🏷️ spam
- `AsSpam()` in *TestKit.Builders.cs*
- `AsSpam()` in *MessageBuilder.cs*
- `CreateManySpamMessages()` in *TestKit.AutoFixture.cs*
- `CreateManySpamMessages()` in *TestKitAutoFixture.cs*
- `CreateMockSpamHamClassifier()` in *TestKit.Mocks.cs*
- `CreateRealisticSpamMessage()` in *TestKit.Bogus.cs*
- `CreateRealisticSpamMessage()` in *TestKitBogus.cs*
- `CreateSpamAiChecks()` in *TestKit.Mocks.cs*
- `CreateSpamHamClassifier()` in *TestKit.Mocks.cs*
- `CreateSpamHamClassifierFactory()` in *TestKit.Facade.cs*
- `CreateSpamMessage()` in *TestKit.Main.cs*
- `CreateSpamMessage()` in *TestKit.Bogus.cs*
- `CreateSpamMessage()` in *TestKitBogus.cs*
- `IsSpamText()` in *TestKit.Bogus.cs*
- `IsSpamText()` in *TestKitBogus.cs*
- `SpamMessage()` in *TestKit.Specialized.cs*

### 🏷️ telegram
- `CreateTelegramBotMock()` in *TestKitMockBuilders.cs*
- `TelegramBotMockBuilder_ThatSendsMessageSuccessfully_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `WithTelegramBot()` in *TestKit.MessageHandlerBuilder.cs*

### 🏷️ test-infrastructure
- `ChatForBanTest()` in *TestKit.Specialized.cs*
- `CreateTestBotClient()` in *TestKit.Mocks.cs*
- `CreateTestBotClient_ReturnsValidClient()` in *TestKit.BuilderTests.cs*
- `CreateTestChat()` in *TestKit.Mocks.cs*
- `CreateTestMessage()` in *TestKit.Mocks.cs*
- `CreateTestToken()` in *TestKit.Mocks.cs*
- `CreateTestToken_ReturnsConsistentToken()` in *TestKit.BuilderTests.cs*
- `CreateTestUpdate()` in *TestKit.Mocks.cs*
- `CreateTestUser()` in *TestKit.Mocks.cs*
- `ForBanTest()` in *TestKit.Specialized.cs*
- `ForChannelTest()` in *TestKit.Specialized.cs*

### 🏷️ user
- `AsBot()` in *TestKit.Builders.cs*
- `AsBot()` in *UserBuilder.cs*
- `AsRegularUser()` in *TestKit.Builders.cs*
- `AsRegularUser()` in *UserBuilder.cs*
- `Bait()` in *TestKit.Specialized.cs*
- `Build()` in *TestKit.UserJoinServiceBuilder.cs*
- `Build()` in *TestKit.Builders.cs*
- `Build()` in *UserBuilder.cs*
- `Build()` in *UserManagerMockBuilder.cs*
- `BuildObject()` in *UserManagerMockBuilder.cs*
- `CreateAnonymousUser()` in *TestKit.Main.cs*
- `CreateApprovedUserManager()` in *TestKit.Mocks.cs*
- `CreateBaitUser()` in *TestKit.Main.cs*
- `CreateBotUser()` in *TestKit.Main.cs*
- `CreateManyUsers()` in *TestKit.AutoFixture.cs*
- `CreateManyUsers()` in *TestKitAutoFixture.cs*
- `CreateMockUserBanService()` in *TestKit.Mocks.cs*
- `CreateMockUserManager()` in *TestKit.Mocks.cs*
- `CreateNewUserJoinMessage()` in *TestKit.Main.cs*
- `CreateRealisticBot()` in *TestKit.Bogus.cs*
- `CreateRealisticBot()` in *TestKitBogus.cs*
- `CreateRealisticUser()` in *TestKit.Bogus.cs*
- `CreateRealisticUser()` in *TestKitBogus.cs*
- `CreateRealisticUsers()` in *TestKit.AutoFixture.cs*
- `CreateRealisticUsers()` in *TestKitAutoFixture.cs*
- `CreateSuspiciousUser()` in *TestKit.Bogus.cs*
- `CreateSuspiciousUser()` in *TestKitBogus.cs*
- `CreateSuspiciousUserAiChecks()` in *TestKit.Mocks.cs*
- `CreateSuspiciousUserMessage()` in *TestKit.Main.cs*
- `CreateTestUser()` in *TestKit.Mocks.cs*
- `CreateUnapprovedUserManager()` in *TestKit.Mocks.cs*
- `CreateUser()` in *TestKit.Builders.cs*
- `CreateUser()` in *TestKitBuilders.cs*
- `CreateUserJoinServiceBuilder()` in *TestKit.Facade.cs*
- `CreateUserList()` in *TestKit.Bogus.cs*
- `CreateUserList()` in *TestKitBogus.cs*
- `CreateUserManager()` in *TestKit.AutoFixture.cs*
- `CreateUserManager()` in *TestKitAutoFixture.cs*
- `CreateUserManagerMock()` in *TestKitMockBuilders.cs*
- `CreateValidUser()` in *TestKit.Main.cs*
- `FromUser()` in *TestKit.Builders.cs*
- `FromUser()` in *TestKit.Builders.cs*
- `FromUser()` in *MessageBuilder.cs*
- `FromUser()` in *MessageBuilder.cs*
- `MessageHandlerBuilder_WithUserManager_CreatesHandlerWithCustomUserManager()` in *TestKit.BuilderTests.cs*
- `ModerationServiceMockBuilder_ThatBansUsers_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `NewUserJoin()` in *TestKit.Specialized.cs*
- `SuspiciousUser()` in *TestKit.Specialized.cs*
- `ThatApprovesUser()` in *UserManagerMockBuilder.cs*
- `ThatBansUsers()` in *ModerationServiceMockBuilder.cs*
- `ThatIsInBanlist()` in *UserManagerMockBuilder.cs*
- `ThatIsNotInBanlist()` in *UserManagerMockBuilder.cs*
- `ThatRejectsUser()` in *UserManagerMockBuilder.cs*
- `ThatSendsUserNotification()` in *MessageServiceMockBuilder.cs*
- `ThatSendsUserNotificationWithReply()` in *MessageServiceMockBuilder.cs*
- `UserForBan()` in *TestKit.Specialized.cs*
- `UserManagerMockBuilder_ThatApprovesUser_CreatesCorrectMock()` in *TestKit.BuilderTests.cs*
- `WithBlacklistedUserScenario()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithCaptchaScenario()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithFirstName()` in *TestKit.Builders.cs*
- `WithFirstName()` in *UserBuilder.cs*
- `WithId()` in *TestKit.Builders.cs*
- `WithId()` in *UserBuilder.cs*
- `WithStandardMocks()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithSuccessfulJoinScenario()` in *TestKit.UserJoinServiceBuilder.cs*
- `WithUserManager()` in *TestKit.MessageHandlerBuilder.cs*
- `WithUsername()` in *TestKit.Builders.cs*
- `WithUsername()` in *UserBuilder.cs*

### 🏷️ valid
- `AsValid()` in *TestKit.Builders.cs*
- `AsValid()` in *MessageBuilder.cs*
- `CreateTestBotClient_ReturnsValidClient()` in *TestKit.BuilderTests.cs*
- `CreateValidCallbackQuery()` in *TestKit.Main.cs*
- `CreateValidCaptchaInfo()` in *TestKit.Main.cs*
- `CreateValidMessage()` in *TestKit.Main.cs*
- `CreateValidMessageWithId()` in *TestKit.Main.cs*
- `CreateValidUser()` in *TestKit.Main.cs*
- `MessageHandlerBuilder_ComplexScenario_CreatesValidHandler()` in *TestKit.BuilderTests.cs*
- `MessageHandlerBuilder_WithStandardMocks_CreatesValidHandler()` in *TestKit.BuilderTests.cs*
- `Valid()` in *TestKit.Specialized.cs*
- `Valid()` in *TestKit.Specialized.cs*

