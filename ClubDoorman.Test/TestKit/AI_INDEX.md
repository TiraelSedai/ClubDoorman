# TestKit AI-Friendly Index

## Quick Reference for AI Assistants

### Most Common Factory Methods:

- `TK.AiChecksMockBuilder_ThatApprovesPhoto_CreatesCorrectMock()` → `void`
- `TK.CaptchaServiceMockBuilder_ThatSucceeds_CreatesCorrectMock()` → `void`
- `TK.CreateAdminApproveCallback()` → `CallbackQuery` - Создает невалидный callback query <tags>callback, invalid, interaction, telegram</tags>
- `TK.CreateAdminBanCallback()` → `CallbackQuery` - Создает callback для одобрения админом <tags>callback, admin, approve, moderation, telegram</tags>
- `TK.CreateAdminNotificationMessage()` → `Message` - Создает сообщение от подозрительного пользователя <tags>message, suspicious, user, ai-analysis, telegram</tags>
- `TK.CreateAdminSkipCallback()` → `CallbackQuery` - Создает callback для бана админом <tags>callback, admin, ban, moderation, telegram</tags>
- `TK.CreateAiChecksFactory()` → `AiChecksTestFactory` - Создает конфигурацию приложения без AI <tags>config, app-config, no-ai, test-infrastructure</tags>
- `TK.CreateAiChecksMock()` → `AiChecksMockBuilder` - Создает билдер для мока ICaptchaService <tags>builders, captcha-service, mocks, fluent-api</tags>
- `TK.CreateAllowResult()` → `ModerationResult` - Создает приманку-капчу <tags>captcha, bait, user-verification, moderation</tags>
- `TK.CreateAnonymousUser()` → `User` - Создает бота-пользователя <tags>user, bot, basic, telegram</tags>
- `TK.CreateAppConfig()` → `IAppConfig` - Создает фабрику для ServiceChatDispatcher <tags>factory, service-chat-dispatcher, test-infrastructure</tags>
- `TK.CreateAppConfigWithoutAi()` → `IAppConfig` - Создает базовую конфигурацию приложения <tags>config, app-config, test-infrastructure</tags>
- `TK.CreateApprovedUserManager()` → `Mock<IUserManager>` - Создает мок IUserManager с предустановленным одобренным пользователем
- `TK.CreateBaitCaptchaInfo()` → `CaptchaInfo` - Создает update с изменением участника чата <tags>update, chat-member, member-management, telegram</tags>
- `TK.CreateBaitUser()` → `User` - Создает неправильный результат капчи <tags>captcha, incorrect, result, user-verification</tags>
- `TK.CreateBanModerationService()` → `Mock<IModerationService>` - Создает мок IModerationService с предустановленным действием бана
- `TK.CreateBanResult()` → `ModerationResult` - Создает результат модерации: удалить <tags>moderation, delete, result, ml, ai</tags>
- `TK.CreateBanTriggeringViolationTracker()` → `Mock<IViolationTracker>` - Создает мок IViolationTracker с предустановленным триггером бана
- `TK.CreateBotBanScenario()` → `BanScenario` - Создает сценарий бана бота <tags>golden-master, bot-ban, scenario</tags>
- `TK.CreateBotPermissionsServiceMock()` → `Mock<IBotPermissionsService>` - Создает мок IBotPermissionsService с заданным значением isAdmin <tags>mock, bot-permissions, admin, test-infrastructure</tags>
- `TK.CreateBotPermissionsServiceMockForChat()` → `Mock<IBotPermissionsService>` - Создает мок IBotPermissionsService, который возвращает true только для заданного chatId <tags>mock, bot-permissions, admin, test-infrastructure, chat-specific</tags>
- `TK.CreateBotUser()` → `User` - Создает валидного пользователя <tags>user, valid, basic, telegram</tags>
- `TK.CreateCallbackQueryHandlerFactory()` → `CallbackQueryHandlerTestFactory` - Создает фабрику для CaptchaService <tags>factory, captcha-service, test-infrastructure</tags>
- `TK.CreateCallbackQueryUpdate()` → `Update` - Создает update с сообщением <tags>update, message, basic, telegram</tags>
- `TK.CreateCaptchaService()` → `ICaptchaService` - Создает CaptchaService с автозависимостями <tags>autofixture, captcha-service, dependencies, test-infrastructure</tags>
- `TK.CreateCaptchaService()` → `ICaptchaService` - Создает CaptchaService с автозависимостями <tags>autofixture, captcha-service, dependencies, test-infrastructure</tags>
- `TK.CreateCaptchaServiceFactory()` → `CaptchaServiceTestFactory` - Создает фабрику для ModerationService <tags>factory, moderation-service, test-infrastructure</tags>
- `TK.CreateCaptchaServiceMock()` → `CaptchaServiceMockBuilder` - Создает билдер для мока IUserManager <tags>builders, user-manager, mocks, fluent-api</tags>
- `TK.CreateChannel()` → `Chat` - Создает супергруппу <tags>chat, supergroup, basic, telegram</tags>
- `TK.CreateChannelMessage()` → `Message` - Создает текстовое сообщение <tags>message, text, basic, telegram</tags>

### Tags Dictionary:
- `ai`: 10 methods
- `autofixture`: 4 methods
- `ban`: 26 methods
- `bogus`: 20 methods
- `builder`: 147 methods
- `captcha`: 15 methods
- `chat`: 54 methods
- `collection`: 16 methods
- `factory`: 185 methods
- `fake`: 5 methods
- `golden-master`: 1 methods
- `invalid`: 2 methods
- `message`: 109 methods
- `mock`: 86 methods
- `moderation`: 17 methods
- `scenario`: 14 methods
- `setup`: 6 methods
- `spam`: 16 methods
- `telegram`: 3 methods
- `test-infrastructure`: 11 methods
- `user`: 68 methods
- `valid`: 12 methods

### Quick Search Guide:
- **Create objects:** Look for `factory` tag
- **Build complex objects:** Look for `builder` tag
- **Mock dependencies:** Look for `mock` tag
- **Test scenarios:** Look for `scenario` tag
- **Realistic data:** Look for `bogus` tag
