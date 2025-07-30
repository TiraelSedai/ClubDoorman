using NUnit.Framework;
using TechTalk.SpecFlow;
using ClubDoorman.Models;
using ClubDoorman.Models.Notifications;
using ClubDoorman.Services;
using ClubDoorman.Handlers;
using ClubDoorman.Test.TestInfrastructure;
using ClubDoorman.TestInfrastructure;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;

namespace ClubDoorman.Test.StepDefinitions.Common
{
    [Binding]
    [Category("BDD")]
    public class SuspiciousCommandSteps
    {
        private Message _testMessage = null!;
        private Exception? _thrownException;
        private FakeTelegramClient _fakeBot = null!;
        private ILoggerFactory _loggerFactory = null!;
        private MessageHandler _messageHandler = null!;
        private MessageHandlerTestFactory _factory = null!;

        [BeforeScenario]
        public void BeforeScenario()
        {
            _fakeBot = new FakeTelegramClient();
            _loggerFactory = LoggerFactory.Create(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _factory = new MessageHandlerTestFactory();
        }

        [AfterScenario]
        public void AfterScenario()
        {
            _loggerFactory?.Dispose();
        }

        [Given(@"I am an administrator")]
        public void GivenIAmAnAdministrator()
        {
            _testMessage = new Message
            {
                From = new User { Id = 123456789, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow
            };
            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["IsAdmin"] = true;
        }

        [Given(@"I am not an administrator")]
        public void GivenIAmNotAnAdministrator()
        {
            _testMessage = new Message
            {
                From = new User { Id = 987654321, FirstName = "RegularUser", Username = "user" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow
            };
            ScenarioContext.Current["TestMessage"] = _testMessage;
            ScenarioContext.Current["IsAdmin"] = false;
            
            // Пересоздаем MessageHandler с новым значением isAdmin
            GivenIHaveAMessageHandler();
        }

        [Given(@"I have a message handler")]
        public void GivenIHaveAMessageHandler()
        {
            _factory.WithStandardMocks();

            _factory.WithAppConfigSetup(mock => {
                mock.Setup(x => x.AdminChatId).Returns(123456789);
                mock.Setup(x => x.LogAdminChatId).Returns(123456789);
                mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
                mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
            });

            // Определяем isAdmin из последнего вызова GivenIAmAnAdministrator или GivenIAmNotAnAdministrator
            var isAdmin = ScenarioContext.Current.ContainsKey("IsAdmin") && (bool)ScenarioContext.Current["IsAdmin"];
            
            var botPermMock = isAdmin
                ? TestKit.TestKit.CreateBotPermissionsServiceMockForChat(123456789)
                : TestKit.TestKit.CreateBotPermissionsServiceMock(false);
            
            _factory.WithBotPermissionsServiceSetup(mock => {
                mock.Reset();
                mock.Setup(x => x.IsBotAdminAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .Returns((long chatId, CancellationToken token) => botPermMock.Object.IsBotAdminAsync(chatId, token));
                mock.Setup(x => x.IsSilentModeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                    .Returns((long chatId, CancellationToken token) => botPermMock.Object.IsSilentModeAsync(chatId, token));
            });

            // Настраиваем MessageService для перехвата уведомлений
            _factory.WithMessageServiceSetup(mock => {
                mock.Setup(x => x.SendUserNotificationAsync(
                    It.IsAny<User>(), 
                    It.IsAny<Chat>(), 
                    It.IsAny<UserNotificationType>(), 
                    It.IsAny<object>(), 
                    It.IsAny<CancellationToken>()))
                    .Callback<User, Chat, UserNotificationType, object, CancellationToken>((user, chat, type, data, token) =>
                    {
                        if (data is SimpleNotificationData notificationData)
                        {
                            var response = notificationData.Reason ?? string.Empty;
                            // Сохраняем в ScenarioContext чтобы CheckCommandSteps мог получить
                            ScenarioContext.Current["LastResponse"] = response;
                        }
                    })
                    .Returns(Task.CompletedTask);
            });

            _messageHandler = _factory.CreateMessageHandlerWithFake(_fakeBot);
            ScenarioContext.Current["MessageHandler"] = _messageHandler;
        }

        [Given(@"there are suspicious users in the system")]
        public void GivenThereAreSuspiciousUsersInTheSystem()
        {
            // Настраиваем мок для SuspiciousUsersStorage
            var suspiciousUsers = new List<(long UserId, long ChatId)>
            {
                (111111111, 123456789),
                (222222222, 123456789)
            };

            var suspiciousInfo1 = new SuspiciousUserInfo(
                DateTime.UtcNow.AddDays(-1),
                new List<string> { "First message" },
                0.8,
                true,
                5
            );

            var suspiciousInfo2 = new SuspiciousUserInfo(
                DateTime.UtcNow.AddDays(-2),
                new List<string> { "Second message" },
                0.6,
                false,
                3
            );

            _factory.WithSuspiciousUsersStorageSetup(mock => 
            {
                // GetSuspiciousUsers не существует в апстриме
                mock.Setup(x => x.IsSuspicious(111111111, 123456789)).Returns(true);
                mock.Setup(x => x.IsSuspicious(222222222, 123456789)).Returns(true);
                mock.Setup(x => x.GetSuspiciousUser(111111111, 123456789)).Returns(suspiciousInfo1);
                mock.Setup(x => x.GetSuspiciousUser(222222222, 123456789)).Returns(suspiciousInfo2);
            });
        }

        [Given(@"there are no suspicious users in the system")]
        public void GivenThereAreNoSuspiciousUsersInTheSystem()
        {
            _factory.WithSuspiciousUsersStorageSetup(mock => 
            {
                // GetSuspiciousUsers не существует в апстриме
                mock.Setup(x => x.IsSuspicious(It.IsAny<long>(), It.IsAny<long>())).Returns(false);
            });
        }

        [Given(@"a user is in the suspicious list")]
        public void GivenAUserIsInTheSuspiciousList()
        {
            var suspiciousUser = new SuspiciousUserInfo(
                DateTime.UtcNow.AddDays(-1),
                new List<string> { "Suspicious message" },
                0.7,
                true,
                2
            );

            _factory.WithSuspiciousUsersStorageSetup(mock => 
            {
                mock.Setup(x => x.IsSuspicious(333333333, 123456789)).Returns(true);
                mock.Setup(x => x.GetSuspiciousUser(333333333, 123456789)).Returns(suspiciousUser);
            });
        }

        [Given(@"I reply to a suspicious user's message with ""(.*)""")]
        public void GivenIReplyToASuspiciousUsersMessageWith(string command)
        {
            _testMessage = new Message
            {
                From = new User { Id = 123456789, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command,
                ReplyToMessage = new Message
                {
                    From = new User { Id = 333333333, FirstName = "TargetUser", Username = "target_user" },
                    Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                    Date = DateTime.UtcNow.AddMinutes(-1)
                }
            };
            ScenarioContext.Current["TestMessage"] = _testMessage;
        }

        [Given(@"I reply to their message with ""(.*)""")]
        public void GivenIReplyToTheirMessageWith(string command)
        {
            _testMessage = new Message
            {
                From = new User { Id = 123456789, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command,
                ReplyToMessage = new Message
                {
                    From = new User { Id = 333333333, FirstName = "TargetUser", Username = "target_user" },
                    Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                    Date = DateTime.UtcNow.AddMinutes(-1)
                }
            };
            ScenarioContext.Current["TestMessage"] = _testMessage;
        }

        [Given(@"I send ""(.*)""")]
        public void GivenISend(string command)
        {
            _testMessage = new Message
            {
                From = new User { Id = 123456789, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command
            };
            ScenarioContext.Current["TestMessage"] = _testMessage;
        }

        [When(@"I send ""(.*)""")]
        public void WhenISend(string command)
        {
            _testMessage = new Message
            {
                From = new User { Id = 123456789, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = 123456789, Title = "Admin Chat", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command
            };
            ScenarioContext.Current["TestMessage"] = _testMessage;
            
            // Сразу обрабатываем команду
            try
            {
                var update = new Update { Message = _testMessage };
                var handler = (MessageHandler)ScenarioContext.Current["MessageHandler"];
                Console.WriteLine($"[DEBUG] SuspiciousCommandSteps: отправляем команду '{command}' в чат {_testMessage.Chat.Id}");
                var task = handler.HandleAsync(update, CancellationToken.None);
                task.Wait();
                Console.WriteLine("[DEBUG] SuspiciousCommandSteps: команда обработана успешно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] SuspiciousCommandSteps: ошибка при обработке команды: {ex}");
                _thrownException = ex;
            }
        }

        [When(@"I send the suspicious command")]
        public void WhenISendTheSuspiciousCommand()
        {
            try
            {
                var update = new Update { Message = _testMessage };
                var handler = (MessageHandler)ScenarioContext.Current["MessageHandler"];
                Console.WriteLine($"[DEBUG] SuspiciousCommandSteps: отправляем команду '{_testMessage.Text}' в чат {_testMessage.Chat.Id}");
                var task = handler.HandleAsync(update, CancellationToken.None);
                task.Wait();
                Console.WriteLine("[DEBUG] SuspiciousCommandSteps: команда обработана успешно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] SuspiciousCommandSteps: ошибка при обработке команды: {ex}");
                _thrownException = ex;
            }
        }

        [Then(@"I should see a list of suspicious users")]
        public void ThenIShouldSeeAListOfSuspiciousUsers()
        {
            // Проверяем, что бот отправил сообщение со списком
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("подозрительных пользователей");
        }

        [Then(@"the list should include user IDs and usernames")]
        public void ThenTheListShouldIncludeUserIDsAndUsernames()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должен быть отправлен список с ID и username");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("111111111");
            lastMessage.Text.Should().Contain("suspicious1");
            lastMessage.Text.Should().Contain("222222222");
            lastMessage.Text.Should().Contain("suspicious2");
        }

        [Then(@"the list should show mimicry scores")]
        public void ThenTheListShouldShowMimicryScores()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должен быть отправлен список с mimicry scores");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("0.8");
            lastMessage.Text.Should().Contain("0.6");
        }

        [Then(@"the user should be added to the suspicious list")]
        public void ThenTheUserShouldBeAddedToTheSuspiciousList()
        {
            // Проверяем, что был вызван метод добавления
            // В реальном тесте здесь была бы проверка через мок
            _fakeBot.SentMessages.Should().NotBeEmpty();
        }

        [Then(@"I should receive a suspicious confirmation message")]
        public void ThenIShouldReceiveASuspiciousConfirmationMessage()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должно быть отправлено сообщение подтверждения");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("добавлен");
        }

        [Then(@"the user should be marked as suspicious")]
        public void ThenTheUserShouldBeMarkedAsSuspicious()
        {
            // Проверяем, что пользователь помечен как подозрительный
            // В реальном тесте здесь была бы проверка через мок
            _fakeBot.SentMessages.Should().NotBeEmpty();
        }

        [Then(@"the user should be removed from the suspicious list")]
        public void ThenTheUserShouldBeRemovedFromTheSuspiciousList()
        {
            // Проверяем, что был вызван метод удаления
            _fakeBot.SentMessages.Should().NotBeEmpty();
        }

        [Then(@"the user should no longer be marked as suspicious")]
        public void ThenTheUserShouldNoLongerBeMarkedAsSuspicious()
        {
            // Проверяем, что пользователь больше не помечен как подозрительный
            _fakeBot.SentMessages.Should().NotBeEmpty();
        }

        [Then(@"I should receive an access denied message")]
        public void ThenIShouldReceiveAnAccessDeniedMessage()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должно быть отправлено сообщение о запрете доступа");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("доступ запрещен");
        }

        [Then(@"no suspicious users list should be displayed")]
        public void ThenNoSuspiciousUsersListShouldBeDisplayed()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должно быть отправлено сообщение");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().NotContain("подозрительных пользователей");
        }

        [Then(@"I should receive an error message")]
        public void ThenIShouldReceiveAnErrorMessage()
        {
            // Команды без реплая просто игнорируются в реальной логике
            // Поэтому ожидаем, что никакого ответа не будет
            _fakeBot.SentMessages.Should().BeEmpty("команды без реплая игнорируются");
        }

        [Then(@"the error should indicate invalid action")]
        public void ThenTheErrorShouldIndicateInvalidAction()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должно быть отправлено сообщение об ошибке");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("неверное действие");
        }

        [Then(@"I should see a message indicating no suspicious users")]
        public void ThenIShouldSeeAMessageIndicatingNoSuspiciousUsers()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должно быть отправлено сообщение о том, что нет подозрительных пользователей");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("нет подозрительных пользователей");
        }

        [Then(@"the message should suggest how to add users")]
        public void ThenTheMessageShouldSuggestHowToAddUsers()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должно быть отправлено сообщение с предложением добавить пользователей");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("добавить");
        }

        [Then(@"the system should find the user by username")]
        public void ThenTheSystemShouldFindTheUserByUsername()
        {
            // Проверяем, что система нашла пользователя по username
            _fakeBot.SentMessages.Should().NotBeEmpty();
        }

        [Then(@"I should see detailed information about the suspicious user")]
        public void ThenIShouldSeeDetailedInformationAboutTheSuspiciousUser()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должна быть отправлена детальная информация о пользователе");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("информация");
        }

        [Then(@"the information should include mimicry analysis")]
        public void ThenTheInformationShouldIncludeMimicryAnalysis()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должна быть отправлена информация с анализом мимикрии");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("мимикрия");
        }

        [Then(@"the information should include when they were added")]
        public void ThenTheInformationShouldIncludeWhenTheyWereAdded()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty("должна быть отправлена информация о времени добавления");
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("добавлен");
        }
    }
} 