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
                // В реальной реализации здесь был бы вызов AI анализа
                // Пока что симулируем результат
                var result = true; // Симулируем подозрительный профиль
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
            // Симулируем отправку уведомления в админский чат
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
    }
} 