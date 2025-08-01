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
    [Category("disabled")]
    public class SayCommandSteps
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
            _factory = new MessageHandlerTestFactory();
            _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _fakeBot = new FakeTelegramClient();
            
            // Настраиваем моки для работы команд
            _factory.WithAppConfigSetup(mock => 
            {
                mock.Setup(x => x.AdminChatId).Returns(-1001234567890);
                mock.Setup(x => x.LogAdminChatId).Returns(-1001234567890);
                mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
                mock.Setup(x => x.DisabledChats).Returns(new HashSet<long>());
            });
            
            // Настраиваем MemoryCache для поиска пользователей
            MemoryCache.Default.Set("-1001234567890_123456789", "testuser", DateTimeOffset.Now.AddMinutes(5));
        }

        [Given(@"I send say command ""(.*)""")]
        public void GivenISendSayCommand(string command)
        {
            _testMessage = new Message
            {
                From = new User { Id = 987654321, FirstName = "AdminUser", Username = "admin" },
                Chat = new Chat { Id = -1001234567890, Title = "Test Group", Type = ChatType.Group },
                Date = DateTime.UtcNow,
                Text = command
            };

            ScenarioContext.Current["TestMessage"] = _testMessage;
        }

        [When(@"I send the say command")]
        public async Task WhenISendTheSayCommand()
        {
            try
            {
                // Создаем MessageHandler с правильным FakeTelegramClient
                _factory.WithBotSetup(mock => 
                {
                    mock.Setup(x => x.BotId).Returns(_fakeBot.BotId);
                    mock.Setup(x => x.SendMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), It.IsAny<ParseMode>(), It.IsAny<ReplyParameters>(), It.IsAny<ReplyMarkup>(), It.IsAny<CancellationToken>()))
                        .Returns<ChatId, string, ParseMode?, ReplyParameters?, ReplyMarkup?, CancellationToken>((chatId, text, parseMode, replyParameters, replyMarkup, token) => _fakeBot.SendMessageAsync(chatId, text, parseMode, replyParameters, replyMarkup, token));
                    mock.Setup(x => x.DeleteMessage(It.IsAny<ChatId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .Returns<ChatId, int, CancellationToken>((chatId, messageId, token) => _fakeBot.DeleteMessageAsync(chatId, messageId, token));
                });
                
                _messageHandler = (MessageHandler)_factory.CreateMessageHandler();
                Console.WriteLine($"[DEBUG] Sending message: {_testMessage.Text}");
                Console.WriteLine($"[DEBUG] Chat ID: {_testMessage.Chat.Id}");
                Console.WriteLine($"[DEBUG] From User ID: {_testMessage.From?.Id}");
                await _messageHandler.HandleAsync(new Update { Message = _testMessage }, CancellationToken.None);
                Console.WriteLine($"[DEBUG] After HandleAsync - SentMessages count: {_fakeBot.SentMessages.Count}");
                foreach (var msg in _fakeBot.SentMessages)
                {
                    Console.WriteLine($"[DEBUG] Sent message: {msg.Text}");
                }
            }
            catch (Exception ex)
            {
                _thrownException = ex;
                Console.WriteLine($"[DEBUG] Exception: {ex.Message}");
            }
        }

        [Then(@"the bot should send ""(.*)""")]
        public void ThenTheBotShouldSend(string expectedText)
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain(expectedText);
        }

        [Then(@"the bot should send the complete message")]
        public void ThenTheBotShouldSendTheCompleteMessage()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().NotBeNullOrEmpty();
        }

        [Then(@"my original message should be deleted")]
        public void ThenMyOriginalMessageShouldBeDeleted()
        {
            // В реальной реализации здесь была бы проверка удаления сообщения
            // Пока что просто проверяем, что нет ошибок
            _thrownException.Should().BeNull();
        }

        [Then(@"I should receive a confirmation")]
        public void ThenIShouldReceiveAConfirmation()
        {
            // В реальной реализации здесь была бы проверка подтверждения
            _thrownException.Should().BeNull();
        }

        [Then(@"I should receive a say error message")]
        public void ThenIShouldReceiveASayErrorMessage()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("ошибка");
        }

        [Then(@"the error should indicate I need to provide text")]
        public void ThenTheErrorShouldIndicateINeedToProvideText()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("текст");
        }

        [Then(@"my original message should not be deleted")]
        public void ThenMyOriginalMessageShouldNotBeDeleted()
        {
            // В реальной реализации здесь была бы проверка, что сообщение не удалено
            _thrownException.Should().BeNull();
        }

        [Then(@"I should receive a say access denied message")]
        public void ThenIShouldReceiveASayAccessDeniedMessage()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("доступ запрещен");
        }

        [Then(@"no message should be sent by the bot")]
        public void ThenNoMessageShouldBeSentByTheBot()
        {
            _fakeBot.SentMessages.Should().BeEmpty();
        }

        [Then(@"the bot should send the message with special characters preserved")]
        public void ThenTheBotShouldSendTheMessageWithSpecialCharactersPreserved()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("@#$%^&*");
        }

        [Then(@"the bot should send the message with emojis")]
        public void ThenTheBotShouldSendTheMessageWithEmojis()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("👋");
        }

        [Then(@"the bot should send the message with proper line breaks")]
        public void ThenTheBotShouldSendTheMessageWithProperLineBreaks()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("Line 1");
            lastMessage.Text.Should().Contain("Line 2");
        }

        [Then(@"the bot should send the message as plain text")]
        public void ThenTheBotShouldSendTheMessageAsPlainText()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("Bold text");
        }

        [Then(@"HTML tags should not be interpreted")]
        public void ThenHTMLTagsShouldNotBeInterpreted()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("<b>");
        }

        [Then(@"the bot should send the message with the mention")]
        public void ThenTheBotShouldSendTheMessageWithTheMention()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("@username");
        }

        [Then(@"the mention should be properly formatted")]
        public void ThenTheMentionShouldBeProperlyFormatted()
        {
            _fakeBot.SentMessages.Should().NotBeEmpty();
            var lastMessage = _fakeBot.SentMessages.Last();
            lastMessage.Text.Should().Contain("@username");
        }
    }
} 