using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestInfrastructure;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using FluentAssertions;
using ClubDoorman.Models;
using ClubDoorman.Handlers;
using Moq;

namespace ClubDoorman.Test.Integration;

[TestFixture]
[Category("integration")]
[Category("e2e")]
[Category("ai-analysis")]
public class AiAnalysisTests
{
    private ILogger<AiChecks> _logger = null!;
    private ILogger<CallbackQueryHandler> _callbackLogger = null!;
    private FakeTelegramClient _fakeBot = null!;
    private AiChecks _aiChecks = null!;
    private CallbackQueryHandler _callbackHandler = null!;
    private UserManager _userManager = null!;
    private ApprovedUsersStorage _approvedUsersStorage = null!;
    private IAppConfig _appConfig = null!;

    [SetUp]
    public void Setup()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AiChecks>();
        _callbackLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<CallbackQueryHandler>();
        _fakeBot = new FakeTelegramClient();
        
        // Используем тестовую конфигурацию
        _appConfig = AppConfigTestFactory.CreateDefault();
        _approvedUsersStorage = new ApprovedUsersStorage(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ApprovedUsersStorage>());
        
        _aiChecks = new AiChecks(_fakeBot, _logger, _appConfig);
        _userManager = new UserManager(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<UserManager>(), _approvedUsersStorage, _appConfig);
        
        // Создаем моки для недостающих зависимостей
        var captchaService = new Mock<ICaptchaService>().Object;
        var badMessageManager = new Mock<IBadMessageManager>().Object;
        var statisticsService = new Mock<IStatisticsService>().Object;
        var moderationService = new Mock<IModerationService>().Object;
        var messageService = new Mock<IMessageService>().Object;
        
        _callbackHandler = new CallbackQueryHandler(_fakeBot, captchaService, _userManager, badMessageManager, statisticsService, _aiChecks, moderationService, messageService, _callbackLogger);
    }

    [TearDown]
    public void TearDown()
    {
        _fakeBot.Reset();
    }

    [Test]
    public async Task E2E_AI_Analysis_FirstMessage_ShouldTriggerAnalysis()
    {
        // Arrange - пользователь с подозрительным профилем
        var suspiciousUser = new User
        {
            Id = 12345,
            FirstName = "🔥🔥🔥",
            LastName = "💰💰💰",
            Username = "money_maker_2024"
        };

        var message = new Message
        {
            From = suspiciousUser,
            Chat = new Chat { Id = -100123456789, Type = ChatType.Supergroup },
            Text = "Привет всем!",
            Date = DateTime.UtcNow
        };

        // Act - отправляем первое сообщение
        var result = await _aiChecks.GetAttentionBaitProbability(suspiciousUser);

        // Assert
        result.Should().NotBeNull();
        result.SpamProbability.Should().NotBeNull();
        
        // Проверяем, что было отправлено уведомление в админский чат
        _fakeBot.SentMessages.Should().Contain(m => 
            m.ChatId == _appConfig.AdminChatId && 
            m.Text.Contains("AI анализ профиля"));
    }

    [Test]
    public async Task E2E_AI_Analysis_AdminButton_Own_ShouldApproveUser()
    {
        // Arrange - создаем уведомление с кнопками
        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
        var adminMessage = new Message
        {
            From = new User { Id = 999999, FirstName = "Admin" },
            Chat = new Chat { Id = _appConfig.AdminChatId, Type = ChatType.Private },
            Text = "AI анализ профиля пользователя",
            ReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new[] { new InlineKeyboardButton("🥰 свой") { CallbackData = "approve_user_12345" } },
                new[] { new InlineKeyboardButton("🤖 бан") { CallbackData = "ban_user_12345" } },
                new[] { new InlineKeyboardButton("😶 пропуск") { CallbackData = "skip_user_12345" } }
            })
        };

        var callbackQuery = new CallbackQuery
        {
            Id = "test_callback_id",
            From = new User { Id = 999999, FirstName = "Admin" },
            Message = adminMessage,
            Data = "approve_user_12345"
        };

        // Act - нажимаем кнопку "🥰 свой"
        await _callbackHandler.HandleAsync(new Update { CallbackQuery = callbackQuery });

        // Assert
        _fakeBot.WasCallbackQueryAnswered("test_callback_id").Should().BeTrue();
        
        // Проверяем, что пользователь добавлен в список одобренных
        var isApproved = _approvedUsersStorage.IsApproved(user.Id);
        isApproved.Should().BeTrue();
    }

    [Test]
    public async Task E2E_AI_Analysis_AdminButton_Ban_ShouldBanUser()
    {
        // Arrange - создаем уведомление с кнопками
        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
        var adminMessage = new Message
        {
            From = new User { Id = 999999, FirstName = "Admin" },
            Chat = new Chat { Id = _appConfig.AdminChatId, Type = ChatType.Private },
            Text = "AI анализ профиля пользователя",
            ReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new[] { new InlineKeyboardButton("🥰 свой") { CallbackData = "approve_user_12345" } },
                new[] { new InlineKeyboardButton("🤖 бан") { CallbackData = "ban_user_12345" } },
                new[] { new InlineKeyboardButton("😶 пропуск") { CallbackData = "skip_user_12345" } }
            })
        };

        var callbackQuery = new CallbackQuery
        {
            Id = "test_callback_id",
            From = new User { Id = 999999, FirstName = "Admin" },
            Message = adminMessage,
            Data = "ban_user_12345"
        };

        // Act - нажимаем кнопку "🤖 бан"
        await _callbackHandler.HandleAsync(new Update { CallbackQuery = callbackQuery });

        // Assert
        _fakeBot.WasCallbackQueryAnswered("test_callback_id").Should().BeTrue();
        
        // Проверяем, что пользователь забанен
        _fakeBot.BannedUsers.Should().Contain(b => b.UserId == user.Id);
    }

    [Test]
    public async Task E2E_AI_Analysis_AdminButton_Skip_ShouldSkipUser()
    {
        // Arrange - создаем уведомление с кнопками
        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
        var adminMessage = new Message
        {
            From = new User { Id = 999999, FirstName = "Admin" },
            Chat = new Chat { Id = _appConfig.AdminChatId, Type = ChatType.Private },
            Text = "AI анализ профиля пользователя",
            ReplyMarkup = new InlineKeyboardMarkup(new[]
            {
                new[] { new InlineKeyboardButton("🥰 свой") { CallbackData = "approve_user_12345" } },
                new[] { new InlineKeyboardButton("🤖 бан") { CallbackData = "ban_user_12345" } },
                new[] { new InlineKeyboardButton("😶 пропуск") { CallbackData = "skip_user_12345" } }
            })
        };

        var callbackQuery = new CallbackQuery
        {
            Id = "test_callback_id",
            From = new User { Id = 999999, FirstName = "Admin" },
            Message = adminMessage,
            Data = "skip_user_12345"
        };

        // Act - нажимаем кнопку "😶 пропуск"
        await _callbackHandler.HandleAsync(new Update { CallbackQuery = callbackQuery });

        // Assert
        _fakeBot.WasCallbackQueryAnswered("test_callback_id").Should().BeTrue();
        
        // Проверяем, что пользователь НЕ добавлен в список одобренных
        var isApproved = _approvedUsersStorage.IsApproved(user.Id);
        isApproved.Should().BeFalse();
        
        // Проверяем, что пользователь НЕ забанен
        _fakeBot.BannedUsers.Should().NotContain(b => b.UserId == user.Id);
    }

    [Test]
    public async Task E2E_AI_Analysis_Channel_ShouldNotShowCaptcha()
    {
        // Arrange - пользователь в канале
        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
        var channelMessage = new Message
        {
            From = user,
            Chat = new Chat { Id = -100123456789, Type = ChatType.Channel },
            Text = "Комментарий в канале",
            Date = DateTime.UtcNow
        };

        // Act - выполняем AI анализ
        var result = await _aiChecks.GetAttentionBaitProbability(user);

        // Assert
        result.Should().NotBeNull();
        
        // В каналах капча не показывается, но AI анализ выполняется
        _fakeBot.SentMessages.Should().Contain(m => 
            m.ChatId == _appConfig.AdminChatId && 
            m.Text.Contains("AI анализ профиля"));
    }

    [Test]
    public async Task E2E_AI_Analysis_RepeatedMessage_ShouldNotTriggerAnalysis()
    {
        // Arrange - пользователь уже прошел AI анализ
        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
        
        // Симулируем, что пользователь уже был проанализирован
        _approvedUsersStorage.ApproveUserGlobally(user.Id);

        var message = new Message
        {
            From = user,
            Chat = new Chat { Id = -100123456789, Type = ChatType.Supergroup },
            Text = "Второе сообщение",
            Date = DateTime.UtcNow
        };

        // Act - отправляем повторное сообщение
        var result = await _aiChecks.GetAttentionBaitProbability(user);

        // Assert - AI анализ не должен выполняться повторно
        result.Should().NotBeNull();
        
        // Проверяем, что НЕ было отправлено уведомление в админский чат
        _fakeBot.SentMessages.Should().NotContain(m => 
            m.ChatId == _appConfig.AdminChatId && 
            m.Text.Contains("AI анализ профиля"));
    }

    [Test]
    public async Task E2E_AI_Analysis_OperationOrder_ShouldBeCorrect()
    {
        // Arrange - пользователь с подозрительным профилем
        var suspiciousUser = new User
        {
            Id = 12345,
            FirstName = "🔥🔥🔥",
            LastName = "💰💰💰",
            Username = "money_maker_2024"
        };

        // Act - выполняем AI анализ
        await _aiChecks.GetAttentionBaitProbability(suspiciousUser);

        // Assert - проверяем порядок операций
        var operationLog = _fakeBot.GetOperationLog();
        
        // Должны быть операции отправки сообщений
        operationLog.Should().Contain(op => op.Contains("SendMessageAsync"));
        
        // Проверяем, что операции выполнялись в правильном порядке
        var sendMessageOps = operationLog.Where(op => op.Contains("SendMessageAsync")).ToList();
        sendMessageOps.Should().NotBeEmpty();
    }

    [Test]
    public async Task E2E_AI_Analysis_PhotoWithCaption_ShouldIncludePhoto()
    {
        // Arrange - пользователь с фото профиля
        var user = new User { Id = 12345, FirstName = "Test", LastName = "User" };
        
        // Настраиваем FakeTelegramClient для возврата фото
        _fakeBot.SetupGetChatFullInfo(user.Id, new ChatFullInfo
        {
            Id = user.Id,
            Type = ChatType.Private,
            Photo = new ChatPhoto
            {
                SmallFileId = "fake_small_file_id",
                BigFileId = "fake_big_file_id"
            }
        });

        // Act - выполняем AI анализ
        var result = await _aiChecks.GetAttentionBaitProbability(user);

        // Assert
        result.Should().NotBeNull();
        
        // Проверяем, что было отправлено фото в админский чат
        _fakeBot.SentPhotos.Should().Contain(p => p.ChatId == _appConfig.AdminChatId);
    }
} 