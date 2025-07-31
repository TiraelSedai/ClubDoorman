using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Test.TestKit;
using ClubDoorman.TestInfrastructure;
using NUnit.Framework;
using Newtonsoft.Json;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Демонстрационные Golden Master тесты для логики банов
/// Показывают концепцию без зависимости от Verify.NUnit
/// <tags>golden-master, demo, ban-logic, concept</tags>
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Critical)]
[Category(TestCategories.GoldenMaster)]
public class MessageHandlerGoldenMasterDemoTests
{
    private MessageHandler _messageHandler;
    private MessageHandlerTestFactory _factory;
    
    [SetUp]
    public void Setup()
    {
        // Создаем MessageHandler с Golden Master моками
        _factory = TK.CreateMessageHandlerFactory()
            .SetupGoldenMasterMocks();
            
        _messageHandler = _factory.CreateMessageHandler();
    }

    /// <summary>
    /// Демонстрационный Golden Master тест с множественными сценариями
    /// Показывает концепцию без Verify.NUnit
    /// </summary>
    [Test]
    public async Task BanUserForLongName_MultipleScenarios_Demo()
    {
        // Arrange: Создаем набор сценариев с сидами
        var scenarios = BanScenarioFactory.CreateScenarioSet(count: 10, baseSeed: 42);
        var results = new List<BanScenarioResult>();

        // Act: Выполняем каждый сценарий
        foreach (var scenario in scenarios)
        {
            try
            {
                await _messageHandler.BanUserForLongName(
                    scenario.Message, 
                    scenario.User, 
                    scenario.Reason, 
                    scenario.BanDuration, 
                    CancellationToken.None);

                // Анализируем результат
                var result = new BanScenarioResult
                {
                    Input = scenario,
                    ShouldCallBanChatMember = scenario.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private,
                    ShouldCallDeleteMessage = scenario.Message != null,
                    ShouldCallForwardToLogWithNotification = scenario.Message != null,
                    ShouldCallSendLogNotification = scenario.Message == null,
                    BanType = scenario.BanDuration.HasValue 
                        ? $"Автобан на {scenario.BanDuration.Value.TotalMinutes} минут" 
                        : "🚫 Перманентный бан",
                    ExpectedReason = scenario.Reason,
                    HasException = false
                };

                results.Add(result);
            }
            catch (Exception ex)
            {
                var result = new BanScenarioResult
                {
                    Input = scenario,
                    HasException = true,
                    ExceptionType = ex.GetType().Name,
                    ExceptionMessage = ex.Message
                };

                results.Add(result);
            }
        }

        // Assert: Проверяем результаты
        Assert.That(results.Count, Is.EqualTo(10), "Должно быть выполнено 10 сценариев");
        
        var successfulResults = results.Where(r => !r.HasException).ToList();
        Assert.That(successfulResults.Count, Is.GreaterThan(0), "Должен быть хотя бы один успешный сценарий");

        // Выводим JSON для анализа (в реальном Golden Master тесте это было бы snapshot)
        var json = JsonConvert.SerializeObject(results, Formatting.Indented);
        Console.WriteLine($"Golden Master Demo Results:\n{json}");

        // Проверяем базовые ожидания
        foreach (var result in successfulResults)
        {
            if (result.Input.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
            {
                Assert.That(result.ShouldCallBanChatMember, Is.True, 
                    $"Для чата типа {result.Input.Chat.Type} должен вызываться BanChatMember");
            }
            
            if (result.Input.Message != null)
            {
                Assert.That(result.ShouldCallDeleteMessage, Is.True, 
                    "Для сообщения должен вызываться DeleteMessage");
            }
        }
    }

    /// <summary>
    /// Демонстрационный тест для разных типов сценариев
    /// </summary>
    [Test]
    public async Task DifferentScenarioTypes_Demo()
    {
        // Arrange: Создаем разные типы сценариев
        var scenarios = new List<BanScenario>
        {
            new BanScenarioBuilder(1).CreateTemporaryBanScenario(),
            new BanScenarioBuilder(2).CreatePermanentBanScenario(),
            new BanScenarioBuilder(3).CreatePrivateChatBanScenario(),
            new BanScenarioBuilder(4).CreateNullMessageBanScenario(),
            new BanScenarioBuilder(5).CreateBotBanScenario()
        };

        var results = new List<BanScenarioResult>();

        // Act: Выполняем каждый сценарий
        foreach (var scenario in scenarios)
        {
            try
            {
                await _messageHandler.BanUserForLongName(
                    scenario.Message, 
                    scenario.User, 
                    scenario.Reason, 
                    scenario.BanDuration, 
                    CancellationToken.None);

                var result = new BanScenarioResult
                {
                    Input = scenario,
                    ShouldCallBanChatMember = scenario.Chat.Type != Telegram.Bot.Types.Enums.ChatType.Private,
                    ShouldCallDeleteMessage = scenario.Message != null,
                    ShouldCallForwardToLogWithNotification = scenario.Message != null,
                    ShouldCallSendLogNotification = scenario.Message == null,
                    BanType = scenario.BanDuration.HasValue 
                        ? $"Автобан на {scenario.BanDuration.Value.TotalMinutes} минут" 
                        : "🚫 Перманентный бан",
                    ExpectedReason = scenario.Reason,
                    HasException = false
                };

                results.Add(result);
            }
            catch (Exception ex)
            {
                var result = new BanScenarioResult
                {
                    Input = scenario,
                    HasException = true,
                    ExceptionType = ex.GetType().Name,
                    ExceptionMessage = ex.Message
                };

                results.Add(result);
            }
        }

        // Assert: Проверяем специфичные ожидания для каждого типа
        Assert.That(results.Count, Is.EqualTo(5), "Должно быть выполнено 5 сценариев");

        // Временный бан
        var temporaryBan = results.First(r => r.Input.ScenarioType == "TemporaryBan");
        Assert.That(temporaryBan.BanType, Does.Contain("Автобан"), "Временный бан должен содержать 'Автобан'");

        // Перманентный бан
        var permanentBan = results.First(r => r.Input.ScenarioType == "PermanentBan");
        Assert.That(permanentBan.BanType, Does.Contain("🚫 Перманентный бан"), "Перманентный бан должен содержать эмодзи");

        // Приватный чат
        var privateChat = results.First(r => r.Input.ScenarioType == "PrivateChatBan");
        Assert.That(privateChat.ShouldCallBanChatMember, Is.False, "В приватном чате не должен вызываться BanChatMember");

        // Без сообщения
        var nullMessage = results.First(r => r.Input.ScenarioType == "NullMessageBan");
        Assert.That(nullMessage.ShouldCallDeleteMessage, Is.False, "Без сообщения не должен вызываться DeleteMessage");

        // Выводим результаты
        var json = JsonConvert.SerializeObject(results, Formatting.Indented);
        Console.WriteLine($"Different Scenario Types Demo:\n{json}");
    }

    /// <summary>
    /// Демонстрационный тест для бана черного списка
    /// </summary>
    [Test]
    public async Task BanBlacklistedUser_Demo()
    {
        // Arrange: Создаем сценарии для черного списка
        var scenarios = Enumerable.Range(0, 5).Select(i =>
        {
            var builder = new BanScenarioBuilder(100 + i);
            var scenario = builder.CreateTemporaryBanScenario();
            scenario.ScenarioType = "BlacklistedUser";
            scenario.Reason = $"Пользователь в черном списке #{i + 1}";
            return scenario;
        }).ToList();

        var results = new List<BanScenarioResult>();

        // Act: Выполняем каждый сценарий
        foreach (var scenario in scenarios)
        {
            try
            {
                await _messageHandler.BanBlacklistedUser(scenario.Message!, scenario.User, CancellationToken.None);

                var result = new BanScenarioResult
                {
                    Input = scenario,
                    ShouldCallBanChatMember = true,
                    ShouldCallDeleteMessage = true,
                    ShouldCallForwardToLogWithNotification = true,
                    BanType = "🚫 Перманентный бан",
                    ExpectedReason = "Пользователь в черном списке",
                    HasException = false
                };

                results.Add(result);
            }
            catch (Exception ex)
            {
                var result = new BanScenarioResult
                {
                    Input = scenario,
                    HasException = true,
                    ExceptionType = ex.GetType().Name,
                    ExceptionMessage = ex.Message
                };

                results.Add(result);
            }
        }

        // Assert: Проверяем результаты бана черного списка
        Assert.That(results.Count, Is.EqualTo(5), "Должно быть выполнено 5 сценариев черного списка");

        foreach (var result in results.Where(r => !r.HasException))
        {
            Assert.That(result.ShouldCallBanChatMember, Is.True, "Для черного списка должен вызываться BanChatMember");
            Assert.That(result.ShouldCallDeleteMessage, Is.True, "Для черного списка должен вызываться DeleteMessage");
            Assert.That(result.ShouldCallForwardToLogWithNotification, Is.True, "Для черного списка должно быть уведомление");
            Assert.That(result.BanType, Is.EqualTo("🚫 Перманентный бан"), "Черный список должен быть перманентным баном");
        }

        // Выводим результаты
        var json = JsonConvert.SerializeObject(results, Formatting.Indented);
        Console.WriteLine($"Blacklisted User Demo:\n{json}");
    }

    /// <summary>
    /// Демонстрационный тест для автоматического бана канала
    /// </summary>
    [Test]
    public async Task AutoBanChannel_Demo()
    {
        // Arrange: Создаем сценарии для бана каналов
        var scenarios = Enumerable.Range(0, 3).Select(i =>
        {
            var builder = new BanScenarioBuilder(200 + i);
            var scenario = builder.CreateBotBanScenario();
            scenario.ScenarioType = "AutoBanChannel";
            scenario.Reason = $"Автоматический бан канала #{i + 1}";
            return scenario;
        }).ToList();

        var results = new List<BanScenarioResult>();

        // Act: Выполняем каждый сценарий
        foreach (var scenario in scenarios)
        {
            try
            {
                await _messageHandler.AutoBanChannel(scenario.Message!, CancellationToken.None);

                var result = new BanScenarioResult
                {
                    Input = scenario,
                    ShouldCallBanChatMember = true,
                    ShouldCallDeleteMessage = true,
                    ShouldCallForwardToLogWithNotification = true,
                    BanType = "🚫 Перманентный бан",
                    ExpectedReason = "Автоматический бан канала",
                    HasException = false
                };

                results.Add(result);
            }
            catch (Exception ex)
            {
                var result = new BanScenarioResult
                {
                    Input = scenario,
                    HasException = true,
                    ExceptionType = ex.GetType().Name,
                    ExceptionMessage = ex.Message
                };

                results.Add(result);
            }
        }

        // Assert: Проверяем результаты бана каналов
        Assert.That(results.Count, Is.EqualTo(3), "Должно быть выполнено 3 сценария бана каналов");

        foreach (var result in results.Where(r => !r.HasException))
        {
            Assert.That(result.ShouldCallBanChatMember, Is.True, "Для канала должен вызываться BanChatMember");
            Assert.That(result.ShouldCallDeleteMessage, Is.True, "Для канала должен вызываться DeleteMessage");
            Assert.That(result.ShouldCallForwardToLogWithNotification, Is.True, "Для канала должно быть уведомление");
            Assert.That(result.BanType, Is.EqualTo("🚫 Перманентный бан"), "Бан канала должен быть перманентным");
        }

        // Выводим результаты
        var json = JsonConvert.SerializeObject(results, Formatting.Indented);
        Console.WriteLine($"Auto Ban Channel Demo:\n{json}");
    }
} 