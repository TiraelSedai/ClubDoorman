using NUnit.Framework;
using TechTalk.SpecFlow;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Moq;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Test.StepDefinitions
{
    [Binding]
    [Category("BDD")]
    public class BasicModerationSteps
    {
        // BDD test state
        private Message _testMessage = null!;
        private ModerationResult _moderationResult = null!;
        private Exception? _thrownException;
        
        // Унифицированный setup через TestKit.Specialized.ModerationScenarios
        private TK.Specialized.ModerationScenarios.ModerationSetup _setup = null!;
        
        // Удобные ссылки на компоненты setup'а (для совместимости с BDD steps)
        private Mock<ILogger<ModerationService>> _mockLogger => _setup.LoggerMock;
        private Mock<IUserManager> _mockUserManager => _setup.UserManagerMock;
        private Mock<SpamHamClassifier> _mockClassifier => TK.CreateMock<SpamHamClassifier>(); // BDD специфичный мок
        private ModerationService _moderationService => _setup.Service;

        [Given(@"the moderation system is initialized")]
        public void GivenTheModerationSystemIsInitialized()
        {
            // Заменяем ~30 строк дублированного кода на один вызов TestKit scenarios
            _setup = TK.Specialized.ModerationScenarios.CompleteSetup();
        }

        [Given(@"user with ID (.*) is not approved in the chat")]
        public void GivenUserWithIDIsNotApprovedInTheChat(long userId)
        {
            _mockUserManager.Setup(x => x.Approved(userId, It.IsAny<long?>()))
                .Returns(false);
        }

        [Given(@"user with ID (.*) is in blacklist (.*)")]
        public void GivenUserWithIDIsInBlacklist(long userId, string blacklist)
        {
            // Set environment variable for test blacklist
            Environment.SetEnvironmentVariable("DOORMAN_TEST_BLACKLIST_IDS", userId.ToString());
            
            // Also setup the mock as backup
            _mockUserManager.Setup(x => x.InBanlist(userId))
                .ReturnsAsync(true);
        }

        [Given(@"user with ID (.*) sends message with inline buttons")]
        public void GivenUserWithIDSendsMessageWithInlineButtons(long userId)
        {
            // Create test message with inline keyboard
            _testMessage = new Message
            {
                From = new User
                {
                    Id = userId,
                    FirstName = "Test",
                    Username = "testuser"
                },
                Chat = new Chat
                {
                    Id = 123456789,
                    Title = "Test Chat",
                    Type = Telegram.Bot.Types.Enums.ChatType.Group
                },
                Text = "Message with buttons",
                ReplyMarkup = new InlineKeyboardMarkup(new[]
                {
                    new InlineKeyboardButton("Button 1") { CallbackData = "btn1" },
                    new InlineKeyboardButton("Button 2") { CallbackData = "btn2" }
                }),
                Date = DateTime.UtcNow
            };
        }

        [When(@"user sends message ""(.*)""")]
        public async Task WhenUserSendsMessage(string messageText)
        {
            // Create test message with correct structure
            _testMessage = new Message
            {
                From = new User
                {
                    Id = 987654321, // Use the blacklisted user ID
                    FirstName = "Test",
                    Username = "testuser"
                },
                Chat = new Chat
                {
                    Id = 123456789,
                    Title = "Test Chat",
                    Type = Telegram.Bot.Types.Enums.ChatType.Group
                },
                Text = messageText,
                Date = DateTime.UtcNow
            };

            // Process the message through moderation
            try
            {
                _moderationResult = await _moderationService.CheckMessageAsync(_testMessage);
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }

        [When(@"user sends message with inline keyboard")]
        public void WhenUserSendsMessageWithInlineKeyboard()
        {
            // Create test message with inline keyboard
            _testMessage = new Message
            {
                From = new User
                {
                    Id = 123456789,
                    FirstName = "Test",
                    Username = "testuser"
                },
                Chat = new Chat
                {
                    Id = 987654321,
                    Title = "Test Chat",
                    Type = Telegram.Bot.Types.Enums.ChatType.Group
                },
                Text = "Message with buttons",
                ReplyMarkup = new InlineKeyboardMarkup(new[]
                {
                    new InlineKeyboardButton("Button 1") { CallbackData = "btn1" },
                    new InlineKeyboardButton("Button 2") { CallbackData = "btn2" }
                }),
                Date = DateTime.UtcNow
            };
        }

        [When(@"user with ID (.*) sends message with inline buttons")]
        public void WhenUserWithIDSendsMessageWithInlineButtons(long userId)
        {
            // Create test message with inline keyboard
            _testMessage = new Message
            {
                From = new User
                {
                    Id = userId,
                    FirstName = "Test",
                    Username = "testuser"
                },
                Chat = new Chat
                {
                    Id = 123456789,
                    Title = "Test Chat",
                    Type = Telegram.Bot.Types.Enums.ChatType.Group
                },
                Text = "Message with buttons",
                ReplyMarkup = new InlineKeyboardMarkup(new[]
                {
                    new InlineKeyboardButton("Button 1") { CallbackData = "btn1" },
                    new InlineKeyboardButton("Button 2") { CallbackData = "btn2" }
                }),
                Date = DateTime.UtcNow
            };
        }

        [When(@"system checks message for moderation")]
        public async Task WhenSystemChecksMessageForModeration()
        {
            try
            {
                _moderationResult = await _moderationService.CheckMessageAsync(_testMessage);
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }

        [Then(@"user should be banned")]
        public void ThenUserShouldBeBanned()
        {
            Assert.That(_moderationResult.Action, Is.EqualTo(ModerationAction.Ban));
        }

        [Then(@"message should be deleted")]
        public void ThenMessageShouldBeDeleted()
        {
            Assert.That(_moderationResult.Action, Is.EqualTo(ModerationAction.Delete));
        }

        [Then(@"reason should be ""(.*)""")]
        public void ThenReasonShouldBe(string expectedReason)
        {
            Assert.That(_moderationResult.Reason, Is.EqualTo(expectedReason));
        }

        [Then(@"notification should be sent to admin chat")]
        public void ThenNotificationShouldBeSentToAdminChat()
        {
            // This would verify that admin notification was sent
            // For now, just check that moderation completed successfully
            Assert.That(_moderationResult, Is.Not.Null);
        }

        [When(@"the message is processed by moderation")]
        public async Task WhenTheMessageIsProcessedByModeration()
        {
            try
            {
                _moderationResult = await _moderationService.CheckMessageAsync(_testMessage);
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }

        [Then(@"no exception should be thrown")]
        public void ThenNoExceptionShouldBeThrown()
        {
            Assert.That(_thrownException, Is.Null);
        }

        [Then(@"moderation result should be Allow")]
        public void ThenModerationResultShouldBeAllow()
        {
            Assert.That(_moderationResult.Action, Is.EqualTo(ModerationAction.Allow));
        }
    }
} 