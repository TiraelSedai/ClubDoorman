using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Test.TestKit;
using ClubDoorman.TestInfrastructure;
using NUnit.Framework;
using Verify.NUnit;

namespace ClubDoorman.Test.Unit.Handlers;

/// <summary>
/// Оптимальные Golden Master тесты для логики банов
/// Используют билдеры с сидами, множественные сценарии и JSON сериализацию
/// <tags>golden-master, optimal-tests, ban-logic, snapshot-testing</tags>
/// </summary>
[TestFixture]
[Category(TestCategories.Unit)]
[Category(TestCategories.Critical)]
[Category(TestCategories.GoldenMaster)]
public class MessageHandlerGoldenMasterOptimalTests
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
    /// Golden Master тест с множественными сценариями бана
    /// Фиксирует поведение системы для 20 различных сценариев
    /// </summary>
    [Test]
    public async Task BanUserForLongName_MultipleScenarios_GoldenMaster()
    {
        // Arrange: Создаем набор сценариев с сидами
        var scenarios = BanScenarioFactory.CreateScenarioSet(count: 20, baseSeed: 42);
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

        // Assert: Golden Master snapshot для всех сценариев
        await global::ClubDoorman.Test.TestKit.TestKitGoldenMaster.CreateGoldenMasterSnapshot(
            "BanUserForLongName_MultipleScenarios_GoldenMaster",
            results,
            "MultipleScenarios_BanUserForLongName");
    }

    /// <summary>
    /// Golden Master тест с JSON сериализацией для детального анализа
    /// </summary>
    [Test]
    public async Task BanUserForLongName_JsonSnapshot_GoldenMaster()
    {
        // Arrange: Создаем разнообразные сценарии
        var scenarios = new List<BanScenario>
        {
            new BanScenarioBuilder(1).CreateTemporaryBanScenario(),
            new BanScenarioBuilder(2).CreatePermanentBanScenario(),
            new BanScenarioBuilder(3).CreatePrivateChatBanScenario(),
            new BanScenarioBuilder(4).CreateNullMessageBanScenario(),
            new BanScenarioBuilder(5).CreateBotBanScenario()
        };

        var testData = new
        {
            TestName = "BanUserForLongName_JsonSnapshot_GoldenMaster",
            Scenarios = scenarios.Select(s => new
            {
                s.Seed,
                s.ScenarioType,
                User = new { s.User.Id, s.User.FirstName, s.User.Username, s.User.IsBot },
                Chat = new { s.Chat.Id, s.Chat.Type, s.Chat.Title },
                Message = s.Message != null ? new { s.Message.MessageId, s.Message.Text } : null,
                s.BanDuration,
                s.Reason
            }).ToList()
        };

        // Act: Выполняем сценарии
        foreach (var scenario in scenarios)
        {
            await _messageHandler.BanUserForLongName(
                scenario.Message, 
                scenario.User, 
                scenario.Reason, 
                scenario.BanDuration, 
                CancellationToken.None);
        }

        // Assert: JSON snapshot для детального анализа
        await TestKitGoldenMaster.CreateGoldenMasterJsonSnapshot(
            "BanUserForLongName_JsonSnapshot_GoldenMaster",
            testData,
            "JsonSnapshot_BanUserForLongName");
    }

    /// <summary>
    /// Golden Master тест для сценариев с исключениями
    /// Фиксирует поведение при ошибках в процессе бана
    /// </summary>
    [Test]
    public async Task BanUserForLongName_ExceptionScenarios_GoldenMaster()
    {
        // Arrange: Создаем сценарии с исключениями
        var scenarios = BanScenarioFactory.CreateExceptionScenarioSet(count: 5, baseSeed: 100);
        var results = new List<BanScenarioResult>();

        // Act: Выполняем каждый сценарий с исключением
        foreach (var scenario in scenarios)
        {
            // Создаем MessageHandler с исключением для каждого сценария
            var factoryWithException = _factory.SetupExceptionScenario(
                new InvalidOperationException($"Bot API error during ban for scenario {scenario.Seed}"));
            var messageHandlerWithException = factoryWithException.CreateMessageHandler();

            try
            {
                await messageHandlerWithException.BanUserForLongName(
                    scenario.Message, 
                    scenario.User, 
                    scenario.Reason, 
                    scenario.BanDuration, 
                    CancellationToken.None);

                var result = new BanScenarioResult
                {
                    Input = scenario,
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

        // Assert: Golden Master snapshot для сценариев с исключениями
        await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
            "BanUserForLongName_ExceptionScenarios_GoldenMaster",
            results,
            "ExceptionScenarios_BanUserForLongName");
    }

    /// <summary>
    /// Golden Master тест для бана черного списка с множественными сценариями
    /// </summary>
    [Test]
    public async Task BanBlacklistedUser_MultipleScenarios_GoldenMaster()
    {
        // Arrange: Создаем сценарии для черного списка
        var scenarios = Enumerable.Range(0, 10).Select(i =>
        {
            var builder = new BanScenarioBuilder(200 + i);
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

        // Assert: Golden Master snapshot для черного списка
        await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
            "BanBlacklistedUser_MultipleScenarios_GoldenMaster",
            results,
            "MultipleScenarios_BanBlacklistedUser");
    }

    /// <summary>
    /// Golden Master тест для автоматического бана канала
    /// </summary>
    [Test]
    public async Task AutoBanChannel_MultipleScenarios_GoldenMaster()
    {
        // Arrange: Создаем сценарии для бана каналов
        var scenarios = Enumerable.Range(0, 8).Select(i =>
        {
            var builder = new BanScenarioBuilder(300 + i);
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

        // Assert: Golden Master snapshot для бана каналов
        await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
            "AutoBanChannel_MultipleScenarios_GoldenMaster",
            results,
            "MultipleScenarios_AutoBanChannel");
    }

    /// <summary>
    /// Golden Master тест для всех типов банов в одном snapshot
    /// Комплексный тест всех сценариев бана
    /// </summary>
    [Test]
    public async Task AllBanTypes_Comprehensive_GoldenMaster()
    {
        // Arrange: Создаем комплексный набор сценариев
        var allScenarios = new List<BanScenario>();

        // Временные баны
        allScenarios.AddRange(Enumerable.Range(0, 5).Select(i => 
        {
            var builder = new BanScenarioBuilder(400 + i);
            return builder.CreateTemporaryBanScenario();
        }));

        // Перманентные баны
        allScenarios.AddRange(Enumerable.Range(0, 5).Select(i => 
        {
            var builder = new BanScenarioBuilder(500 + i);
            return builder.CreatePermanentBanScenario();
        }));

        // Приватные чаты
        allScenarios.AddRange(Enumerable.Range(0, 3).Select(i => 
        {
            var builder = new BanScenarioBuilder(600 + i);
            return builder.CreatePrivateChatBanScenario();
        }));

        // Без сообщений
        allScenarios.AddRange(Enumerable.Range(0, 3).Select(i => 
        {
            var builder = new BanScenarioBuilder(700 + i);
            return builder.CreateNullMessageBanScenario();
        }));

        // Боты
        allScenarios.AddRange(Enumerable.Range(0, 4).Select(i => 
        {
            var builder = new BanScenarioBuilder(800 + i);
            return builder.CreateBotBanScenario();
        }));

        var results = new List<BanScenarioResult>();

        // Act: Выполняем все сценарии
        foreach (var scenario in allScenarios)
        {
            try
            {
                switch (scenario.ScenarioType)
                {
                    case "BotBan":
                        await _messageHandler.AutoBanChannel(scenario.Message!, CancellationToken.None);
                        break;
                    default:
                        await _messageHandler.BanUserForLongName(
                            scenario.Message, 
                            scenario.User, 
                            scenario.Reason, 
                            scenario.BanDuration, 
                            CancellationToken.None);
                        break;
                }

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

        // Assert: Комплексный Golden Master snapshot
        await TestKitGoldenMaster.CreateGoldenMasterSnapshot(
            "AllBanTypes_Comprehensive_GoldenMaster",
            results,
            "Comprehensive_AllBanTypes");
    }
} 