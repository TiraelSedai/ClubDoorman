using ClubDoorman.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Moq;

namespace ClubDoorman.Test.TestKit.Builders.MockBuilders;

/// <summary>
/// Билдер для мока ITelegramBotClientWrapper
/// <tags>builders, telegram-bot, mocks, fluent-api</tags>
/// </summary>
public class TelegramBotMockBuilder
{
    private readonly Mock<ITelegramBotClientWrapper> _mock = new();

    /// <summary>
    /// Настраивает мок для успешной отправки сообщений
    /// <tags>builders, telegram-bot, send-message, fluent-api</tags>
    /// </summary>
    public TelegramBotMockBuilder ThatSendsMessageSuccessfully()
    {
        _mock.Setup(x => x.SendMessage(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<ParseMode?>(),
                It.IsAny<ReplyParameters?>(),
                It.IsAny<ReplyMarkup?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Text = "Test message" });
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для успешного удаления сообщений
    /// <tags>builders, telegram-bot, delete-message, fluent-api</tags>
    /// </summary>
    public TelegramBotMockBuilder ThatDeletesMessagesSuccessfully()
    {
        _mock.Setup(x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для пересылки сообщений
    /// <tags>builders, telegram-bot, forward-message, fluent-api</tags>
    /// </summary>
    public TelegramBotMockBuilder ThatForwardsMessages()
    {
        _mock.Setup(x => x.ForwardMessage(
                It.IsAny<ChatId>(),
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Message { Text = "Forwarded message" });
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для выброса исключения при удалении
    /// <tags>builders, telegram-bot, throw-on-delete, fluent-api</tags>
    /// </summary>
    public TelegramBotMockBuilder ThatThrowsOnDelete()
    {
        _mock.Setup(x => x.DeleteMessage(
                It.IsAny<ChatId>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Delete failed"));
        
        return this;
    }

    /// <summary>
    /// Настраивает мок для выброса исключения при отправке
    /// <tags>builders, telegram-bot, throw-on-send, fluent-api</tags>
    /// </summary>
    public TelegramBotMockBuilder ThatThrowsOnSend()
    {
        _mock.Setup(x => x.SendMessage(
                It.IsAny<ChatId>(),
                It.IsAny<string>(),
                It.IsAny<ParseMode?>(),
                It.IsAny<ReplyParameters?>(),
                It.IsAny<ReplyMarkup?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Send failed"));
        
        return this;
    }

    /// <summary>
    /// Строит мок
    /// <tags>builders, telegram-bot, build, fluent-api</tags>
    /// </summary>
    public Mock<ITelegramBotClientWrapper> Build() => _mock;

    /// <summary>
    /// Строит объект мока
    /// <tags>builders, telegram-bot, build-object, fluent-api</tags>
    /// </summary>
    public ITelegramBotClientWrapper BuildObject() => Build().Object;
} 