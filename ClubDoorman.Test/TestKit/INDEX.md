# TestKit Index - Тегированный индекс генераторов и моков

## 📚 Примеры использования

### 🔧 Фасадные методы
```csharp
// Создание фабрик для всех сервисов
var messageHandlerFactory = TK.CreateMessageHandlerFactory();
var moderationFactory = TK.CreateModerationServiceFactory();
var captchaFactory = TK.CreateCaptchaServiceFactory();

// Создание конфигурации
var config = TK.CreateAppConfig();
var configWithoutAi = TK.CreateAppConfigWithoutAi();
```

### 🤖 AutoFixture
```csharp
// Автоматическое создание объектов с зависимостями
var handler = TK.Create<MessageHandler>();
var users = TK.CreateMany<User>(5);
var (sut, fixture) = TK.CreateWithFixture<MessageHandler>();

// Создание специализированных сервисов
var moderationService = TestKitAutoFixture.CreateModerationService();
var captchaService = TestKitAutoFixture.CreateCaptchaService();
```

### 🏗️ Builders (Fluent API)
```csharp
// Читаемое создание сообщений
var message = TestKitBuilders.CreateMessage()
    .WithText("Hello, world!")
    .FromUser(12345)
    .InChat(67890)
    .AsSpam()
    .Build();

// Создание пользователей
var user = TestKitBuilders.CreateUser()
    .WithId(12345)
    .WithUsername("testuser")
    .WithFirstName("Test")
    .AsRegularUser()
    .Build();

// Создание результатов модерации
var result = TestKitBuilders.CreateModerationResult()
    .WithAction(ModerationAction.Ban)
    .WithReason("Spam detected")
    .WithConfidence(0.95)
    .Build();
```

### 🎭 Bogus (Реалистичные данные)
```csharp
// Реалистичные пользователи и боты
var user = TestKitBogus.CreateRealisticUser();
var bot = TestKitBogus.CreateRealisticBot();

// Реалистичные чаты
var group = TestKitBogus.CreateRealisticGroup();
var supergroup = TestKitBogus.CreateRealisticSupergroup();
var channel = TestKitBogus.CreateRealisticChannel();

// Реалистичные сообщения
var message = TestKitBogus.CreateRealisticMessage();
var spamMessage = TestKitBogus.CreateRealisticSpamMessage();
var longMessage = TestKitBogus.CreateLongMessage();

// Проверка на спам
bool isSpam = TestKitBogus.IsSpamText("🔥💰🎁 Make money fast!");
```

### 📱 Telegram Helpers
```csharp
// Создание fake клиента
var fakeClient = TestKitTelegram.CreateFakeClient();

// Создание envelope с автоматическим MessageId
var envelope = TestKitTelegram.CreateEnvelope(
    userId: 12345,
    chatId: 67890,
    text: "Test message"
);

// Готовые сценарии
var (fakeClient, envelope, message, update) = TestKitTelegram.CreateSpamScenario();
var (fakeClient, envelope, message, update) = TestKitTelegram.CreateNewUserScenario();

// Создание callback query
var callbackQuery = TestKitTelegram.CreateCallbackQuery(
    userId: 12345,
    chatId: 67890,
    data: "test_callback"
);

// Создание обновлений участников
var memberUpdate = TestKitTelegram.CreateChatMemberUpdate(
    userId: 12345,
    chatId: 67890,
    oldStatus: ChatMemberStatus.Member,
    newStatus: ChatMemberStatus.Administrator
);
```

### 🔍 Быстрый поиск по тегам

#### Поиск по доменам
- **Бан и модерация:** `ban`, `moderation`, `spam`
- **Капча:** `captcha`, `verification`
- **AI/ML:** `ai`, `ml`, `spam-ham-classifier`
- **Админ:** `admin`, `callback-query`, `chat-member`

#### Поиск по типам
- **Фабрики:** `factory`, `test-infrastructure`
- **Автогенерация:** `autofixture`, `auto-generation`
- **Читаемый API:** `builders`, `fluent-api`
- **Реалистичные данные:** `bogus`, `realistic`, `faker`
- **Telegram:** `telegram`, `message-id`, `scenarios`

#### Поиск по объектам
- **Сообщения:** `message`, `text`, `spam`
- **Пользователи:** `user`, `bot`, `regular`
- **Чаты:** `chat`, `group`, `channel`, `supergroup`
- **Результаты:** `moderation-result`, `action`, `reason` 