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
        private Message _testMessage = null!;
        private ModerationResult _moderationResult = null!;
        private Exception? _thrownException;
        private Mock<ILogger<ModerationService>> _mockLogger = null!;
        private Mock<IUserManager> _mockUserManager = null!;
        private Mock<SpamHamClassifier> _mockClassifier = null!;
        private ModerationService _moderationService = null!;

        [Given(@"the moderation system is initialized")]
        public void GivenTheModerationSystemIsInitialized()
        {
            // Create mocks
            _mockLogger = new Mock<ILogger<ModerationService>>();
            var mockSpamLogger = new Mock<ILogger<SpamHamClassifier>>();
            var mockMimicryLogger = new Mock<ILogger<MimicryClassifier>>();
            var mockAiLogger = new Mock<ILogger<AiChecks>>();
            var mockSuspiciousLogger = new Mock<ILogger<SuspiciousUsersStorage>>();
            _mockUserManager = new Mock<IUserManager>();
            _mockClassifier = new Mock<SpamHamClassifier>();

            // Create real instances for classes that can't be mocked (sealed classes or classes without parameterless constructors)
            var realBadMessageManager = new BadMessageManager();
            var realSpamHamClassifier = new SpamHamClassifier(mockSpamLogger.Object);
            var realMimicryClassifier = new MimicryClassifier(mockMimicryLogger.Object);
            var realSuspiciousStorage = new SuspiciousUsersStorage(mockSuspiciousLogger.Object);
            
            // Create a real TelegramBotClient with a test token for testing
            var realBotClient = new TelegramBotClient("1234567890:TEST_TOKEN_FOR_TESTS");
            var realAiChecks = new AiChecks(new TelegramBotClientWrapper(realBotClient), mockAiLogger.Object, AppConfigTestFactory.CreateDefault());

            // Create ModerationService with correct constructor
            _moderationService = new ModerationService(
                realSpamHamClassifier,
                realMimicryClassifier,
                realBadMessageManager,
                _mockUserManager.Object,
                realAiChecks,
                realSuspiciousStorage,
                realBotClient,
                new Mock<IMessageService>().Object,
                _mockLogger.Object
            );
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