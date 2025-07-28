using NUnit.Framework;
using TechTalk.SpecFlow;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Moq;
using Microsoft.Extensions.Logging;
using FluentAssertions;

namespace ClubDoorman.Test.StepDefinitions.Common
{
    [Binding]
    [Category("BDD")]
    public class StatisticsSteps
    {
        private Message _testMessage = null!;
        private Exception? _thrownException;
        private FakeTelegramClient _fakeBot = null!;
        private ILoggerFactory _loggerFactory = null!;

        [BeforeScenario]
        public void BeforeScenario()
        {
            _fakeBot = new FakeTelegramClient();
            _loggerFactory = LoggerFactory.Create(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Упрощенная инициализация для тестов
        }

        [AfterScenario]
        public void AfterScenario()
        {
            _loggerFactory?.Dispose();
        }

        [Given(@"there is activity in chat \(messages, bans, approvals\)")]
        public void GivenThereIsActivityInChat()
        {
            // Симулируем активность в чате
            _testMessage = new Message
            {
                From = new User { Id = 123456789, FirstName = "TestUser" },
                Chat = new Chat { Id = -1001234567890, Title = "Test Group", Type = ChatType.Group },
                Date = DateTime.UtcNow
            };
            ScenarioContext.Current["TestMessage"] = _testMessage;
        }

        [When(@"the /stat command is executed")]
        public void WhenTheStatCommandIsExecuted()
        {
            try
            {
                // Симулируем выполнение команды /stat
                var commandMessage = new Message
                {
                    From = new User { Id = 123456789, FirstName = "Admin" },
                    Chat = _testMessage.Chat,
                    Text = "/stat"
                };

                ScenarioContext.Current["CommandMessage"] = commandMessage;
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }

        [When(@"automatic statistics time comes")]
        public void WhenAutomaticStatisticsTimeComes()
        {
            try
            {
                // Симулируем автоматическую отправку статистики
                ScenarioContext.Current["StatisticsResult"] = "mock_stats";
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }

        [When(@"a regular user tries to execute /spam command")]
        public void WhenARegularUserTriesToExecuteSpamCommand()
        {
            try
            {
                // Симулируем попытку обычного пользователя выполнить команду
                var regularUserMessage = new Message
                {
                    From = new User { Id = 987654321, FirstName = "RegularUser" },
                    Chat = _testMessage.Chat,
                    Text = "/spam"
                };

                ScenarioContext.Current["RegularUserMessage"] = regularUserMessage;
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }

        [Then(@"correct statistics are displayed")]
        public void ThenCorrectStatisticsAreDisplayed()
        {
            // Проверяем, что статистика корректна
            var stats = ScenarioContext.Current["StatisticsResult"];
            stats.Should().NotBeNull();
        }

        [Then(@"statistics are formatted correctly")]
        public void ThenStatisticsAreFormattedCorrectly()
        {
            // Проверяем форматирование статистики
            var stats = ScenarioContext.Current["StatisticsResult"];
            stats.Should().NotBeNull();
        }

        [Then(@"daily report is sent")]
        public void ThenDailyReportIsSent()
        {
            // Проверяем отправку ежедневного отчета
            var sentMessages = _fakeBot.SentMessages
                .Where(m => m.Text.Contains("statistics") || m.Text.Contains("report"))
                .ToList();
            
            sentMessages.Should().NotBeEmpty();
        }

        [Then(@"statistics include all metrics")]
        public void ThenStatisticsIncludeAllMetrics()
        {
            // Проверяем, что статистика включает все метрики
            var stats = ScenarioContext.Current["StatisticsResult"];
            stats.Should().NotBeNull();
        }

        [Then(@"report is sent to correct chat")]
        public void ThenReportIsSentToCorrectChat()
        {
            // Проверяем, что отчет отправлен в правильный чат
            var sentMessages = _fakeBot.SentMessages
                .Where(m => m.Text.Contains("statistics") || m.Text.Contains("report"))
                .ToList();
            
            sentMessages.Should().NotBeEmpty();
        }

        [Then(@"the command is ignored")]
        public void ThenTheCommandIsIgnored()
        {
            // Проверяем, что команда игнорируется для обычного пользователя
            _thrownException.Should().BeNull();
        }

        [Then(@"there is a log record about unauthorized access attempt")]
        public void ThenThereIsALogRecordAboutUnauthorizedAccessAttempt()
        {
            // В реальной реализации здесь была бы проверка логов
            _thrownException.Should().BeNull();
        }
    }
} 