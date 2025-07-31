using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using ClubDoorman.Handlers;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Golden Master инфраструктура для тестирования логики банов
/// Использует существующие билдеры, Bogus с сидами и простую JSON сериализацию
/// <tags>golden-master, snapshot-testing, ban-logic, test-infrastructure</tags>
/// </summary>
public static class TestKitGoldenMaster
{
    /// <summary>
    /// Создает Golden Master тест с множественными сценариями
    /// <tags>golden-master, multiple-scenarios, snapshot-testing</tags>
    /// </summary>
    /// <param name="testName">Название теста</param>
    /// <param name="scenarios">Список сценариев для тестирования</param>
    /// <param name="snapshotFileName">Имя файла для snapshot</param>
    public static async Task CreateGoldenMasterSnapshot(
        string testName,
        IEnumerable<BanScenarioResult> scenarios,
        string snapshotFileName)
    {
        // Создаем упрощенную версию для сериализации
        var simplifiedScenarios = scenarios.Select(s => new
        {
            Input = new
            {
                User = new { s.Input.User.Id, s.Input.User.FirstName, s.Input.User.Username, s.Input.User.IsBot },
                Chat = new { s.Input.Chat.Id, s.Input.Chat.Type, s.Input.Chat.Title },
                Message = s.Input.Message != null ? new { s.Input.Message.MessageId, s.Input.Message.Text } : null,
                s.Input.BanDuration,
                s.Input.Reason,
                s.Input.ScenarioType,
                s.Input.Seed
            },
            s.ShouldCallBanChatMember,
            s.ShouldCallDeleteMessage,
            s.ShouldCallForwardToLogWithNotification,
            s.ShouldCallSendLogNotification,
            s.BanType,
            s.ExpectedReason,
            s.HasException,
            s.ExceptionType,
            s.ExceptionMessage
        }).ToList();

        var goldenMasterData = new
        {
            TestName = testName,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            TotalScenarios = simplifiedScenarios.Count,
            Scenarios = simplifiedScenarios
        };

        // Пробуем восстановить Verify.NUnit с глобальным using
        await Verifier.Verify(goldenMasterData)
            .UseDirectory("GoldenMasterSnapshots")
            .UseFileName(snapshotFileName);
    }

    /// <summary>
    /// Создает Golden Master тест с JSON сериализацией
    /// <tags>golden-master, json-serialization, snapshot-testing</tags>
    /// </summary>
    /// <param name="testName">Название теста</param>
    /// <param name="data">Данные для сериализации</param>
    /// <param name="snapshotFileName">Имя файла для snapshot</param>
    public static async Task CreateGoldenMasterJsonSnapshot<T>(
        string testName,
        T data,
        string snapshotFileName)
    {
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        
        var goldenMasterData = new
        {
            TestName = testName,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            JsonData = json
        };

        // Пробуем восстановить Verify.NUnit с глобальным using
        await Verifier.Verify(goldenMasterData)
            .UseDirectory("GoldenMasterSnapshots")
            .UseFileName(snapshotFileName);
    }

    /// <summary>
    /// Создает стандартный набор моков для Golden Master тестов
    /// <tags>golden-master, mocks, test-setup</tags>
    /// </summary>
    /// <param name="factory">Фабрика тестов</param>
    /// <returns>Настроенная фабрика с базовыми моками</returns>
    public static MessageHandlerTestFactory SetupGoldenMasterMocks(this MessageHandlerTestFactory factory)
    {
        return factory
            .WithBotSetup(mock =>
            {
                mock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            })
            .WithMessageServiceSetup(mock =>
            {
                mock.Setup(x => x.SendAdminNotificationAsync(It.IsAny<AdminNotificationType>(), It.IsAny<ErrorNotificationData>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mock.Setup(x => x.ForwardToLogWithNotificationAsync(It.IsAny<Message>(), It.IsAny<LogNotificationType>(), It.IsAny<AutoBanNotificationData>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Message());
            });
    }

    /// <summary>
    /// Создает сценарий с исключением для тестирования
    /// <tags>golden-master, exception-testing, mocks</tags>
    /// </summary>
    /// <param name="factory">Фабрика тестов</param>
    /// <param name="exception">Исключение для эмуляции</param>
    /// <returns>Настроенная фабрика с исключением</returns>
    public static MessageHandlerTestFactory SetupExceptionScenario(
        this MessageHandlerTestFactory factory,
        Exception exception)
    {
        return factory
            .WithBotSetup(mock =>
            {
                mock.Setup(x => x.BanChatMember(It.IsAny<ChatId>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(exception);
            });
    }
}

/// <summary>
/// Сценарий для Golden Master тестов
/// <tags>golden-master, scenario, test-data</tags>
/// </summary>
public class BanScenario
{
    public User User { get; set; } = null!;
    public Chat Chat { get; set; } = null!;
    public Message? Message { get; set; }
    public TimeSpan? BanDuration { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ScenarioType { get; set; } = string.Empty;
    public int Seed { get; set; }
}

/// <summary>
/// Результат выполнения сценария
/// <tags>golden-master, result, test-data</tags>
/// </summary>
public class BanScenarioResult
{
    public BanScenario Input { get; set; } = null!;
    public bool ShouldCallBanChatMember { get; set; }
    public bool ShouldCallDeleteMessage { get; set; }
    public bool ShouldCallForwardToLogWithNotification { get; set; }
    public bool ShouldCallSendLogNotification { get; set; }
    public string BanType { get; set; } = string.Empty;
    public string ExpectedReason { get; set; } = string.Empty;
    public bool HasException { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
}

/// <summary>
/// Билдер сценариев с сидами для стабильности
/// <tags>golden-master, builder, seeded, stable</tags>
/// </summary>
public class BanScenarioBuilder
{
    private readonly int _seed;
    private readonly Faker _faker;

    public BanScenarioBuilder(int seed = 42)
    {
        _seed = seed;
        _faker = new Faker("ru");
        _faker.Random = new Randomizer(seed);
    }

    /// <summary>
    /// Создает сценарий временного бана
    /// <tags>golden-master, temporary-ban, scenario</tags>
    /// </summary>
    public BanScenario CreateTemporaryBanScenario()
    {
        var user = TestKitBuilders.CreateUser()
            .WithId(_faker.Random.Long(100000000, 999999999))
            .WithFirstName(_faker.Name.FirstName())
            .WithUsername(_faker.Internet.UserName())
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(_faker.Random.Long(-1000000000000, -100000000000))
            .WithType(ChatType.Supergroup)
            .WithTitle(_faker.Company.CompanyName())
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(chat)
            .WithMessageId(_faker.Random.Int(1, 99999))
            .WithText(_faker.Lorem.Sentence())
            .Build();

        return new BanScenario
        {
            User = user,
            Chat = chat,
            Message = message,
            BanDuration = TimeSpan.FromMinutes(_faker.Random.Int(5, 60)),
            Reason = "Временный бан за нарушение",
            ScenarioType = "TemporaryBan",
            Seed = _seed
        };
    }

    /// <summary>
    /// Создает сценарий перманентного бана
    /// <tags>golden-master, permanent-ban, scenario</tags>
    /// </summary>
    public BanScenario CreatePermanentBanScenario()
    {
        var user = TestKitBuilders.CreateUser()
            .WithId(_faker.Random.Long(100000000, 999999999))
            .WithFirstName(_faker.Name.FirstName())
            .WithUsername(_faker.Internet.UserName())
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(_faker.Random.Long(-1000000000000, -100000000000))
            .WithType(ChatType.Supergroup)
            .WithTitle(_faker.Company.CompanyName())
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(chat)
            .WithMessageId(_faker.Random.Int(1, 99999))
            .WithText(_faker.Lorem.Sentence())
            .Build();

        return new BanScenario
        {
            User = user,
            Chat = chat,
            Message = message,
            BanDuration = null, // Перманентный бан
            Reason = "Перманентный бан за серьезное нарушение",
            ScenarioType = "PermanentBan",
            Seed = _seed
        };
    }

    /// <summary>
    /// Создает сценарий бана в приватном чате
    /// <tags>golden-master, private-chat, scenario</tags>
    /// </summary>
    public BanScenario CreatePrivateChatBanScenario()
    {
        var user = TestKitBuilders.CreateUser()
            .WithId(_faker.Random.Long(100000000, 999999999))
            .WithFirstName(_faker.Name.FirstName())
            .WithUsername(_faker.Internet.UserName())
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(_faker.Random.Long(100000000, 999999999))
            .WithType(ChatType.Private)
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(chat)
            .WithMessageId(_faker.Random.Int(1, 99999))
            .WithText(_faker.Lorem.Sentence())
            .Build();

        return new BanScenario
        {
            User = user,
            Chat = chat,
            Message = message,
            BanDuration = TimeSpan.FromMinutes(30),
            Reason = "Бан в приватном чате",
            ScenarioType = "PrivateChatBan",
            Seed = _seed
        };
    }

    /// <summary>
    /// Создает сценарий бана без сообщения
    /// <tags>golden-master, null-message, scenario</tags>
    /// </summary>
    public BanScenario CreateNullMessageBanScenario()
    {
        var user = TestKitBuilders.CreateUser()
            .WithId(_faker.Random.Long(100000000, 999999999))
            .WithFirstName(_faker.Name.FirstName())
            .WithUsername(_faker.Internet.UserName())
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(_faker.Random.Long(-1000000000000, -100000000000))
            .WithType(ChatType.Supergroup)
            .WithTitle(_faker.Company.CompanyName())
            .Build();

        return new BanScenario
        {
            User = user,
            Chat = chat,
            Message = null,
            BanDuration = TimeSpan.FromMinutes(15),
            Reason = "Бан без сообщения",
            ScenarioType = "NullMessageBan",
            Seed = _seed
        };
    }

    /// <summary>
    /// Создает сценарий бана бота
    /// <tags>golden-master, bot-ban, scenario</tags>
    /// </summary>
    public BanScenario CreateBotBanScenario()
    {
        var user = TestKitBuilders.CreateUser()
            .WithId(_faker.Random.Long(100000000, 999999999))
            .WithFirstName(_faker.Name.FirstName())
            .WithUsername(_faker.Internet.UserName())
            .AsBot()
            .Build();

        var chat = TestKitBuilders.CreateChat()
            .WithId(_faker.Random.Long(-1000000000000, -100000000000))
            .WithType(ChatType.Supergroup)
            .WithTitle(_faker.Company.CompanyName())
            .Build();

        var message = TestKitBuilders.CreateMessage()
            .FromUser(user)
            .InChat(chat)
            .WithMessageId(_faker.Random.Int(1, 99999))
            .WithText(_faker.Lorem.Sentence())
            .Build();

        return new BanScenario
        {
            User = user,
            Chat = chat,
            Message = message,
            BanDuration = TimeSpan.FromMinutes(60),
            Reason = "Бан бота",
            ScenarioType = "BotBan",
            Seed = _seed
        };
    }
}

/// <summary>
/// Фабрика для создания множественных сценариев
/// <tags>golden-master, factory, multiple-scenarios</tags>
/// </summary>
public static class BanScenarioFactory
{
    /// <summary>
    /// Создает набор разнообразных сценариев
    /// <tags>golden-master, scenario-set, variety</tags>
    /// </summary>
    /// <param name="count">Количество сценариев</param>
    /// <param name="baseSeed">Базовый сид</param>
    /// <returns>Список сценариев</returns>
    public static List<BanScenario> CreateScenarioSet(int count = 20, int baseSeed = 42)
    {
        var scenarios = new List<BanScenario>();
        
        for (int i = 0; i < count; i++)
        {
            var seed = baseSeed + i;
            var builder = new BanScenarioBuilder(seed);
            
            // Чередуем типы сценариев
            var scenarioType = i % 5;
            BanScenario scenario = scenarioType switch
            {
                0 => builder.CreateTemporaryBanScenario(),
                1 => builder.CreatePermanentBanScenario(),
                2 => builder.CreatePrivateChatBanScenario(),
                3 => builder.CreateNullMessageBanScenario(),
                4 => builder.CreateBotBanScenario(),
                _ => builder.CreateTemporaryBanScenario()
            };
            
            scenarios.Add(scenario);
        }
        
        return scenarios;
    }

    /// <summary>
    /// Создает набор сценариев с исключениями
    /// <tags>golden-master, exception-scenarios, error-testing</tags>
    /// </summary>
    /// <param name="count">Количество сценариев</param>
    /// <param name="baseSeed">Базовый сид</param>
    /// <returns>Список сценариев</returns>
    public static List<BanScenario> CreateExceptionScenarioSet(int count = 5, int baseSeed = 100)
    {
        var scenarios = new List<BanScenario>();
        
        for (int i = 0; i < count; i++)
        {
            var seed = baseSeed + i;
            var builder = new BanScenarioBuilder(seed);
            var scenario = builder.CreateTemporaryBanScenario();
            scenario.ScenarioType = $"ExceptionScenario_{i}";
            scenarios.Add(scenario);
        }
        
        return scenarios;
    }
} 