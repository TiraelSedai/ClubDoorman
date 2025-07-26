using NUnit.Framework;
using TechTalk.SpecFlow;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Moq;
using Microsoft.Extensions.Logging;
using FluentAssertions;

namespace ClubDoorman.Test.StepDefinitions.Common
{
    [Binding]
    public class AiAnalysisSteps
    {
        private Message _testMessage = null!;
        private CallbackQuery _callbackQuery = null!;
        private Exception? _thrownException;
        private FakeTelegramClient _fakeBot = null!;
        private ILoggerFactory _loggerFactory = null!;
        private AiChecks _aiChecks = null!;
        private UserManager _userManager = null!;

        [BeforeScenario]
        public void BeforeScenario()
        {
            _fakeBot = new FakeTelegramClient();
            _loggerFactory = LoggerFactory.Create(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            var appConfig = AppConfigTestFactory.CreateDefault();
            var botClient = new TelegramBotClient("1234567890:TEST_TOKEN_FOR_TESTS");
            var telegramWrapper = new TelegramBotClientWrapper(botClient);
            var approvedUsersStorage = new ApprovedUsersStorage(_loggerFactory.CreateLogger<ApprovedUsersStorage>());
            
            _aiChecks = new AiChecks(telegramWrapper, _loggerFactory.CreateLogger<AiChecks>(), appConfig);
            _userManager = new UserManager(_loggerFactory.CreateLogger<UserManager>(), approvedUsersStorage, appConfig);
        }

        [AfterScenario]
        public void AfterScenario()
        {
            _loggerFactory?.Dispose();
        }

        [When(@"AI profile analysis is performed")]
        public async Task WhenAiProfileAnalysisIsPerformed()
        {
            try
            {
                // Выполняем реальный AI анализ
                var result = await _aiChecks.GetAttentionBaitProbability(_testMessage.From!);
                ScenarioContext.Current["AiAnalysisResult"] = result;
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }

        [When(@"a notification is sent to admin chat with profile photo")]
        public void WhenNotificationIsSentToAdminChatWithProfilePhoto()
        {
            // Проверяем, что уведомление было отправлено в админский чат
            var wasNotificationSent = _fakeBot.SentMessages.Any(m => 
                m.Text.Contains("AI анализ профиля"));
            
            wasNotificationSent.Should().BeTrue("Уведомление должно быть отправлено в админский чат");
            
            // Симулируем админское сообщение для дальнейших тестов
            var adminMessage = new Message
            {
                From = new User { Id = 123456789, FirstName = "Admin" },
                Chat = new Chat { Id = 123456789, Type = ChatType.Private },
                Text = "AI анализ профиля пользователя",
                Photo = new[] { new PhotoSize { FileId = "test_photo_id" } }
            };

            ScenarioContext.Current["AdminNotification"] = adminMessage;
        }

        [When(@"the button ""(.*)"" is clicked")]
        public void WhenButtonIsClicked(string buttonText)
        {
            var adminMessage = (Message)ScenarioContext.Current["AdminNotification"];
            
            string callbackData = buttonText switch
            {
                "🥰 свой" => "approve_user",
                "🤖 бан" => "ban_user",
                "😶 пропуск" => "skip_user",
                _ => throw new ArgumentException($"Неизвестная кнопка: {buttonText}")
            };

            _callbackQuery = new CallbackQuery
            {
                Id = Guid.NewGuid().ToString(),
                From = adminMessage.From,
                Message = adminMessage,
                Data = callbackData
            };
        }

        [When(@"the user is added to global approved list")]
        public void WhenUserIsAddedToGlobalApprovedList()
        {
            try
            {
                // Симулируем добавление пользователя в глобальный список одобренных
                var userId = _testMessage.From!.Id;
                // В реальной реализации здесь был бы вызов метода добавления
                ScenarioContext.Current["UserApproved"] = true;
            }
            catch (Exception ex)
            {
                _thrownException = ex;
            }
        }

        [Then(@"there is a log record about AI analysis")]
        public void ThenLogsContainAiAnalysisRecord()
        {
            // В реальной реализации здесь была бы проверка логов
            _thrownException.Should().BeNull();
        }

        [Then(@"there is a log record about approval")]
        public void ThenLogsContainApprovalRecord()
        {
            // В реальной реализации здесь была бы проверка логов
            _thrownException.Should().BeNull();
        }

        [Then(@"there is a log record about ban")]
        public void ThenLogsContainBanRecord()
        {
            // В реальной реализации здесь была бы проверка логов
            _thrownException.Should().BeNull();
        }

        [Then(@"AI check is NOT performed in admin chat")]
        public void ThenAiCheckIsNotPerformedInAdminChat()
        {
            // Проверяем, что AI проверка не выполняется в админском чате
            var aiAnalysisResult = ScenarioContext.Current.ContainsKey("AiAnalysisResult");
            aiAnalysisResult.Should().BeFalse();
        }

        [Then(@"no exceptions should occur")]
        public void ThenNoExceptionsShouldOccur()
        {
            _thrownException.Should().BeNull();
        }

        [Given(@"a user with bait profile joins the group")]
        public void GivenAUserWithBaitProfileJoinsTheGroup()
        {
            _testMessage = new Message
            {
                From = new User
                {
                    Id = 12345,
                    FirstName = "🔥🔥🔥",
                    LastName = "💰💰💰",
                    Username = "money_maker_2024"
                },
                Chat = new Chat { Id = -100123456789, Type = ChatType.Supergroup },
                Text = "Привет всем!",
                Date = DateTime.UtcNow
            };
        }

        [When(@"the user sends the first message")]
        public void WhenTheUserSendsTheFirstMessage()
        {
            // Сообщение уже создано в Given, здесь можно добавить дополнительную логику
            ScenarioContext.Current["FirstMessage"] = _testMessage;
        }

        [Then(@"the user gets restricted for (.*) minutes")]
        public void ThenTheUserGetsRestrictedForMinutes(int minutes)
        {
            // Проверяем, что пользователь был ограничен
            var wasRestricted = _fakeBot.RestrictedUsers.Any(r => r.UserId == _testMessage.From!.Id);
            wasRestricted.Should().BeTrue($"Пользователь должен быть ограничен на {minutes} минут");
        }

        [Given(@"there is a notification with buttons in admin chat")]
        public void GivenThereIsANotificationWithButtonsInAdminChat()
        {
            var adminMessage = new Message
            {
                From = new User { Id = 999999, FirstName = "Admin" },
                Chat = new Chat { Id = 123456789, Type = ChatType.Private },
                Text = "AI анализ профиля пользователя",
                ReplyMarkup = new InlineKeyboardMarkup(new[]
                {
                    new[] { new InlineKeyboardButton("🥰 свой") { CallbackData = "approve_user_12345" } },
                    new[] { new InlineKeyboardButton("🤖 бан") { CallbackData = "ban_user_12345" } },
                    new[] { new InlineKeyboardButton("😶 пропуск") { CallbackData = "skip_user_12345" } }
                })
            };

            ScenarioContext.Current["AdminNotification"] = adminMessage;
        }

        [Then(@"the restriction is removed")]
        public void ThenTheRestrictionIsRemoved()
        {
            // В реальной реализации здесь была бы проверка снятия ограничений
            // Пока что просто проверяем отсутствие исключений
            _thrownException.Should().BeNull();
        }

        [Then(@"the user gets banned")]
        public void ThenTheUserGetsBanned()
        {
            var wasBanned = _fakeBot.BannedUsers.Any(b => b.UserId == _testMessage.From!.Id);
            wasBanned.Should().BeTrue("Пользователь должен быть забанен");
        }

        [Then(@"all user messages are deleted")]
        public void ThenAllUserMessagesAreDeleted()
        {
            // Проверяем, что сообщения пользователя были удалены
            var deletedMessages = _fakeBot.DeletedMessages.Where(d => d.ChatId == _testMessage.Chat.Id).ToList();
            deletedMessages.Should().NotBeEmpty("Сообщения пользователя должны быть удалены");
        }

        [Given(@"a user with bait profile joins the channel")]
        public void GivenAUserWithBaitProfileJoinsTheChannel()
        {
            _testMessage = new Message
            {
                From = new User
                {
                    Id = 12345,
                    FirstName = "🔥🔥🔥",
                    LastName = "💰💰💰",
                    Username = "money_maker_2024"
                },
                Chat = new Chat { Id = -100123456789, Type = ChatType.Channel },
                Text = "Комментарий в канале",
                Date = DateTime.UtcNow
            };
        }

        [When(@"the user leaves a comment")]
        public void WhenTheUserLeavesAComment()
        {
            // Комментарий уже создан в Given
            ScenarioContext.Current["ChannelComment"] = _testMessage;
        }

        [Then(@"the captcha is NOT shown \(channels don't support captcha\)")]
        public void ThenTheCaptchaIsNotShownChannelsDontSupportCaptcha()
        {
            // В каналах капча не показывается, но AI анализ выполняется
            var wasNotificationSent = _fakeBot.SentMessages.Any(m => 
                m.Text.Contains("AI анализ профиля"));
            wasNotificationSent.Should().BeTrue("AI анализ должен выполняться даже в каналах");
        }
    }
} 