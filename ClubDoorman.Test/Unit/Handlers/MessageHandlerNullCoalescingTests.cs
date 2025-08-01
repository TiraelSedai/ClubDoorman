using System;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Handlers;
using ClubDoorman.Models;
using ClubDoorman.Services;
using ClubDoorman.Test.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Telegram.Bot.Types;

namespace ClubDoorman.Test.Unit.Handlers
{
    /// <summary>
    /// Тесты для покрытия null coalescing мутантов в MessageHandler
    /// <tags>unit, handlers, null-coalescing, mutation-coverage, critical</tags>
    /// </summary>
    [TestFixture]
    [Category("unit")]
    [Category("handlers")]
    [Category("null-coalescing")]
    [Category("mutation-coverage")]
    public class MessageHandlerNullCoalescingTests
    {
        private MessageHandlerTestFactory _factory = null!;
        private MessageHandler _messageHandler = null!;
        private Mock<ILogger<MessageHandler>> _loggerMock = null!;

        [SetUp]
        public void Setup()
        {
            _factory = new MessageHandlerTestFactory();
            
            // Настраиваем фабрику для тестов null coalescing
            _factory.WithStandardMocks();
            
            _messageHandler = _factory.CreateMessageHandler();
            _loggerMock = _factory.LoggerMock;
        }

        #region Null Coalescing Mutation Coverage Tests

        /// <summary>
        /// Тест для покрытия null coalescing мутантов - сообщение только с Text
        /// <tags>null-coalescing, text-only, mutation-coverage, production</tags>
        /// </summary>
        [Test]
        public async Task HandleAsync_TextOnlyMessage_CoversNullCoalescingMutations()
        {
            // Arrange
            var (user, chat, message) = TK.Specialized.Messages.TextOnlyScenario();
            var update = new Update { Message = message };

            // Act
            await _messageHandler.HandleAsync(update);

            // Assert
            // Проверяем, что логирование произошло с правильным текстом
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Обычное текстовое сообщение")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// Тест для покрытия null coalescing мутантов - сообщение только с Caption
        /// <tags>null-coalescing, caption-only, mutation-coverage, production</tags>
        /// </summary>
        [Test]
        public async Task HandleAsync_CaptionOnlyMessage_CoversNullCoalescingMutations()
        {
            // Arrange
            var (user, chat, message) = TK.Specialized.Messages.CaptionOnlyScenario();
            var update = new Update { Message = message };

            // Act
            await _messageHandler.HandleAsync(update);

            // Assert
            // Проверяем, что логирование произошло с правильной подписью
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Подпись к медиа")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// Тест для покрытия null coalescing мутантов - сообщение без Text и Caption
        /// <tags>null-coalescing, no-text, mutation-coverage, production</tags>
        /// </summary>
        [Test]
        public async Task HandleAsync_NoTextMessage_CoversNullCoalescingMutations()
        {
            // Arrange
            var (user, chat, message) = TK.Specialized.Messages.NoTextScenario();
            var update = new Update { Message = message };

            // Act
            await _messageHandler.HandleAsync(update);

            // Assert
            // Проверяем, что логирование произошло с fallback значением "[медиа]"
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[медиа]")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// Тест для покрытия null coalescing мутантов - длинный текст (>100 символов)
        /// <tags>null-coalescing, long-text, truncation, mutation-coverage, production</tags>
        /// </summary>
        [Test]
        public async Task HandleAsync_LongTextMessage_CoversNullCoalescingMutations()
        {
            // Arrange
            var (user, chat, message) = TK.Specialized.Messages.LongTextScenario();
            var update = new Update { Message = message };
            var expected = message.Text.Substring(0, 100);

            // Act
            await _messageHandler.HandleAsync(update);

            // Assert
            // Проверяем, что логирование произошло с обрезанным текстом (ровно 100 символов)
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expected)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// Тест для покрытия null coalescing мутантов - длинная подпись (>100 символов)
        /// <tags>null-coalescing, long-caption, truncation, mutation-coverage, production</tags>
        /// </summary>
        [Test]
        public async Task HandleAsync_LongCaptionMessage_CoversNullCoalescingMutations()
        {
            // Arrange
            var (user, chat, message) = TK.Specialized.Messages.LongCaptionScenario();
            var update = new Update { Message = message };
            var expected = message.Caption.Substring(0, 100);

            // Act
            await _messageHandler.HandleAsync(update);

            // Assert
            // Проверяем, что логирование произошло с обрезанной подписью (ровно 100 символов)
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expected)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// Тест для покрытия null coalescing мутантов - пустой текст
        /// <tags>null-coalescing, empty-text, mutation-coverage, production</tags>
        /// </summary>
        [Test]
        public async Task HandleAsync_EmptyTextMessage_CoversNullCoalescingMutations()
        {
            // Arrange
            var user = TK.CreateRealisticUser(userId: 11111);
            var chat = TK.CreateGroupChat();
            var message = TK.CreateValidMessage();
            message.From = user;
            message.Chat = chat;
            message.Text = "";
            message.Caption = null;
            
            var update = new Update { Message = message };

            // Act
            await _messageHandler.HandleAsync(update);

            // Assert
            // Проверяем, что логирование произошло с пустым текстом
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Сообщение:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// Тест для покрытия null coalescing мутантов - пробельный текст
        /// <tags>null-coalescing, whitespace-text, mutation-coverage, production</tags>
        /// </summary>
        [Test]
        public async Task HandleAsync_WhitespaceTextMessage_CoversNullCoalescingMutations()
        {
            // Arrange
            var user = TK.CreateRealisticUser(userId: 22222);
            var chat = TK.CreateGroupChat();
            var message = TK.CreateValidMessage();
            message.From = user;
            message.Chat = chat;
            message.Text = "   ";
            message.Caption = null;
            
            var update = new Update { Message = message };

            // Act
            await _messageHandler.HandleAsync(update);

            // Assert
            // Проверяем, что логирование произошло с пробельным текстом
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("   ")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region Edge Cases for Null Coalescing

        /// <summary>
        /// Тест для покрытия null coalescing мутантов - текст ровно 100 символов
        /// <tags>null-coalescing, exact-length, mutation-coverage, production</tags>
        /// </summary>
        [Test]
        public async Task HandleAsync_ExactLengthTextMessage_CoversNullCoalescingMutations()
        {
            // Arrange
            var user = TK.CreateRealisticUser(userId: 33333);
            var chat = TK.CreateGroupChat();
            var message = TK.CreateValidMessage();
            message.From = user;
            message.Chat = chat;
            message.Text = new string('A', 100); // Ровно 100 символов
            message.Caption = null;
            
            var update = new Update { Message = message };

            // Act
            await _messageHandler.HandleAsync(update);

            // Assert
            // Проверяем, что логирование произошло без обрезки
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(new string('A', 100))),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// Тест для покрытия null coalescing мутантов - текст 99 символов
        /// <tags>null-coalescing, near-limit, mutation-coverage, production</tags>
        /// </summary>
        [Test]
        public async Task HandleAsync_NearLimitTextMessage_CoversNullCoalescingMutations()
        {
            // Arrange
            var user = TK.CreateRealisticUser(userId: 44444);
            var chat = TK.CreateGroupChat();
            var message = TK.CreateValidMessage();
            message.From = user;
            message.Chat = chat;
            message.Text = new string('B', 99); // 99 символов
            message.Caption = null;
            
            var update = new Update { Message = message };

            // Act
            await _messageHandler.HandleAsync(update);

            // Assert
            // Проверяем, что логирование произошло без обрезки
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(new string('B', 99))),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// Тест для покрытия null coalescing мутантов - текст 101 символ
        /// <tags>null-coalescing, over-limit, mutation-coverage, production</tags>
        /// </summary>
        [Test]
        public async Task HandleAsync_OverLimitTextMessage_CoversNullCoalescingMutations()
        {
            // Arrange
            var user = TK.CreateRealisticUser(userId: 55555);
            var chat = TK.CreateGroupChat();
            var message = TK.CreateValidMessage();
            message.From = user;
            message.Chat = chat;
            message.Text = new string('C', 101); // 101 символ
            message.Caption = null;
            
            var update = new Update { Message = message };

            // Act
            await _messageHandler.HandleAsync(update);

            // Assert
            // Проверяем, что логирование произошло с обрезкой (ровно 100 символов)
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString()!.Contains(new string('C', 100))),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion
    }
} 