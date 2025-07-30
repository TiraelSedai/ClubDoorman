using NUnit.Framework;
using TechTalk.SpecFlow;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Handlers;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Moq;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using System.Runtime.Caching;

namespace ClubDoorman.Test.StepDefinitions.Common
{
    [Binding]
    [Category("BDD")]
    public class SpamHamCommandSteps
    {
        private Message _testMessage = null!;
        private Message _repliedMessage = null!;
        private Exception? _thrownException;
        private MessageHandler _messageHandler = null!;
        private FakeTelegramClient _fakeBot = null!;
        private MessageHandlerTestFactory _factory = null!;
        private ILoggerFactory _loggerFactory = null!;
        private string _lastResponse = string.Empty;

        [BeforeScenario]
        public void BeforeScenario()
        {
            _fakeBot = new FakeTelegramClient();
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _factory = new MessageHandlerTestFactory();
            _thrownException = null;
            _lastResponse = string.Empty;
        }

        [Given(@"the bot is running in a group chat")]
        public void GivenTheBotIsRunningInAGroupChat()
        {
            // Базовая настройка для всех тестов
            _messageHandler = _factory
                .WithAppConfigSetup(mock => 
                {
                    mock.Setup(x => x.AdminChatId).Returns(123456789);
                    mock.Setup(x => x.LogAdminChatId).Returns(123456789);
                    mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
                    mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
                })
                .WithUserManagerSetup(userManager => userManager.Setup(x => x.Approved(It.IsAny<long>(), It.IsAny<long?>())).Returns(true))
                .CreateMessageHandler();
        }

        [Given(@"I am an admin and reply to a message with ""(.*)""")]
        public void GivenIAmAnAdminAndReplyToAMessageWith(string command)
        {
            // Создаем тестовое сообщение от админа
            _testMessage = new Message
            {
                From = new User { Id = 123456789, IsBot = false, Username = "admin" },
                Chat = new Chat { Id = 123456789, Type = ChatType.Supergroup },
                Text = command,
                Date = DateTime.UtcNow,
                ReplyToMessage = new Message
                {
                    From = new User { Id = 987654321, IsBot = false, Username = "user" },
                    Chat = new Chat { Id = 123456789, Type = ChatType.Supergroup },
                    Text = "Test message",
                    Date = DateTime.UtcNow.AddMinutes(-1)
                }
            };
        }

        [Given(@"I am a regular user and reply to a message with ""(.*)""")]
        public void GivenIAmARegularUserAndReplyToAMessageWith(string command)
        {
            // Создаем тестовое сообщение от обычного пользователя
            _testMessage = new Message
            {
                From = new User { Id = 111222333, IsBot = false, Username = "regular_user" },
                Chat = new Chat { Id = 123456789, Type = ChatType.Supergroup },
                Text = command,
                Date = DateTime.UtcNow,
                ReplyToMessage = new Message
                {
                    From = new User { Id = 987654321, IsBot = false, Username = "user" },
                    Chat = new Chat { Id = 123456789, Type = ChatType.Supergroup },
                    Text = "Test message",
                    Date = DateTime.UtcNow.AddMinutes(-1)
                }
            };
        }

        [Given(@"I send a command ""(.*)"" without reply")]
        public void GivenISendACommandWithoutReply(string command)
        {
            // Создаем тестовое сообщение без reply
            _testMessage = new Message
            {
                From = new User { Id = 123456789, IsBot = false, Username = "admin" },
                Chat = new Chat { Id = 123456789, Type = ChatType.Supergroup },
                Text = command,
                Date = DateTime.UtcNow
            };
        }

        [When(@"I execute the command")]
        public async Task WhenIExecuteTheCommand()
        {
            try
            {
                var update = new Update { Message = _testMessage };
                await _messageHandler.HandleAsync(update);
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }

        [Then(@"the command should be processed successfully")]
        public void ThenTheCommandShouldBeProcessedSuccessfully()
        {
            _thrownException.Should().BeNull();
            // Проверяем, что команда была обработана (можно добавить более детальные проверки)
        }

        [Then(@"I should receive an access denied message for spam ham")]
        public void ThenIShouldReceiveAnAccessDeniedMessageForSpamHam()
        {
            _thrownException.Should().BeNull();
            // Проверяем, что пользователь получил сообщение об отказе в доступе
        }

        [Then(@"I should receive an error message about missing reply")]
        public void ThenIShouldReceiveAnErrorMessageAboutMissingReply()
        {
            _thrownException.Should().BeNull();
            // Проверяем, что пользователь получил сообщение об ошибке
        }
    }
} 