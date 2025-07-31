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
using Verify.NUnit;

namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Golden Master инфраструктура для тестирования логики банов
/// Использует существующие билдеры, Bogus с сидами и Verify.NUnit
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
    public static async Task CreateGoldenMasterSnapshot<TScenario>(
        string testName,
        IEnumerable<TScenario> scenarios,
        string snapshotFileName)
    {
        var goldenMasterData = new
        {
            TestName = testName,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            TotalScenarios = scenarios.Count(),
            Scenarios = scenarios.ToList()
        };

        await global::Verify.Verifier.Verify(goldenMasterData)
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

        await global::Verify.Verifier.Verify(goldenMasterData)
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
                mock.Setup(x => x.BanChatMember(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mock.Setup(x => x.DeleteMessage(It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            })
            .WithMessageServiceSetup(mock =>
            {
                mock.Setup(x => x.SendAdminNotificationAsync(It.IsAny<AdminNotificationType>(), It.IsAny<ErrorNotificationData>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mock.Setup(x => x.ForwardToLogWithNotificationAsync(It.IsAny<Message>(), It.IsAny<LogNotificationType>(), It.IsAny<AutoBanNotificationData>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                mock.Setup(x => x.SendLogNotificationAsync(It.IsAny<LogNotificationType>(), It.IsAny<AutoBanNotificationData>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            })
            .WithLoggerSetup(mock =>
            {
                mock.Setup(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()));
            })
            .WithUserFlowLoggerSetup(mock =>
            {
                mock.Setup(x => x.LogUserBanned(It.IsAny<User>(), It.IsAny<Chat>(), It.IsAny<string>()));
            });
    }

    /// <summary>
    /// Создает сценарий бана с исключением для Golden Master тестов
    /// <tags>golden-master, exception-scenario, test-setup</tags>
    /// </summary>
    /// <param name="exception">Исключение для симуляции</param>
    /// <param name="factory">Фабрика тестов</param>
    /// <returns>Настроенная фабрика с исключением</returns>
    public static MessageHandlerTestFactory SetupExceptionScenario(
        this MessageHandlerTestFactory factory,
        Exception exception)
    {
        return factory.WithBotSetup(mock =>
        {
            mock.Setup(x => x.BanChatMember(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<DateTime?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
        });
    }
}

/// <summary>
/// Сценарий бана для Golden Master тестов
/// <tags>golden-master, ban-scenario, test-data</tags>
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
/// Результат выполнения сценария бана
/// <tags>golden-master, ban-result, test-data</tags>
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
/// Билдер для создания сценариев бана с сидами
/// <tags>golden-master, ban-scenario-builder, seeded-data</tags>
/// </summary>
public class BanScenarioBuilder
{
    private readonly int _seed;
    private readonly Faker _faker;
    
    public BanScenarioBuilder(int seed = 42)
    {
        _seed = seed;
        _faker = new Faker("ru").UseSeed(seed);
    }

    /// <summary>
    /// Создает сценарий временного бана
    /// <tags>golden-master, temporary-ban, scenario-builder</tags>
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
    /// <tags>golden-master, permanent-ban, scenario-builder</tags>
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
            BanDuration = null,
            Reason = "Перманентный бан за серьезное нарушение",
            ScenarioType = "PermanentBan",
            Seed = _seed
        };
    }

    /// <summary>
    /// Создает сценарий бана в приватном чате
    /// <tags>golden-master, private-chat-ban, scenario-builder</tags>
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
            .WithTitle("Private Chat")
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
            BanDuration = TimeSpan.FromMinutes(10),
            Reason = "Попытка бана в приватном чате",
            ScenarioType = "PrivateChatBan",
            Seed = _seed
        };
    }

    /// <summary>
    /// Создает сценарий бана без сообщения
    /// <tags>golden-master, null-message-ban, scenario-builder</tags>
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
            Reason = "Бан без исходного сообщения",
            ScenarioType = "NullMessageBan",
            Seed = _seed
        };
    }

    /// <summary>
    /// Создает сценарий бана бота
    /// <tags>golden-master, bot-ban, scenario-builder</tags>
    /// </summary>
    public BanScenario CreateBotBanScenario()
    {
        var user = TestKitBuilders.CreateUser()
            .WithId(_faker.Random.Long(100000000, 999999999))
            .WithFirstName(_faker.PickRandom("TestBot", "HelperBot", "ServiceBot", "AdminBot"))
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
            BanDuration = null,
            Reason = "Автоматический бан канала",
            ScenarioType = "BotBan",
            Seed = _seed
        };
    }
}

/// <summary>
/// Фабрика для создания множественных сценариев бана
/// <tags>golden-master, scenario-factory, multiple-scenarios</tags>
/// </summary>
public static class BanScenarioFactory
{
    /// <summary>
    /// Создает набор сценариев для Golden Master тестирования
    /// <tags>golden-master, scenario-set, multiple-scenarios</tags>
    /// </summary>
    /// <param name="count">Количество сценариев</param>
    /// <param name="baseSeed">Базовый сид</param>
    /// <returns>Список сценариев</returns>
    public static List<BanScenario> CreateScenarioSet(int count = 20, int baseSeed = 42)
    {
        var scenarios = new List<BanScenario>();
        var scenarioTypes = new[] { "TemporaryBan", "PermanentBan", "PrivateChatBan", "NullMessageBan", "BotBan" };

        for (int i = 0; i < count; i++)
        {
            var seed = baseSeed + i;
            var builder = new BanScenarioBuilder(seed);
            var scenarioType = scenarioTypes[i % scenarioTypes.Length];

            var scenario = scenarioType switch
            {
                "TemporaryBan" => builder.CreateTemporaryBanScenario(),
                "PermanentBan" => builder.CreatePermanentBanScenario(),
                "PrivateChatBan" => builder.CreatePrivateChatBanScenario(),
                "NullMessageBan" => builder.CreateNullMessageBanScenario(),
                "BotBan" => builder.CreateBotBanScenario(),
                _ => builder.CreateTemporaryBanScenario()
            };

            scenarios.Add(scenario);
        }

        return scenarios;
    }

    /// <summary>
    /// Создает сценарии с исключениями для тестирования обработки ошибок
    /// <tags>golden-master, exception-scenarios, error-handling</tags>
    /// </summary>
    /// <param name="count">Количество сценариев</param>
    /// <param name="baseSeed">Базовый сид</param>
    /// <returns>Список сценариев с исключениями</returns>
    public static List<BanScenario> CreateExceptionScenarioSet(int count = 5, int baseSeed = 100)
    {
        var scenarios = new List<BanScenario>();

        for (int i = 0; i < count; i++)
        {
            var seed = baseSeed + i;
            var builder = new BanScenarioBuilder(seed);
            var scenario = builder.CreateTemporaryBanScenario();
            scenario.ScenarioType = "ExceptionScenario";
            scenario.Reason = $"Тест обработки исключений #{i + 1}";
            scenarios.Add(scenario);
        }

        return scenarios;
    }
} 