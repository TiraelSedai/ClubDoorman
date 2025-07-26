using ClubDoorman.TestInfrastructure;
using FluentAssertions;
using NUnit.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ClubDoorman.Test.Integration;

/// <summary>
/// Тесты для расширенного FakeTelegramClient
/// Проверяет новые возможности: callback queries, фото, редактирование сообщений
/// </summary>
[TestFixture]
[Category("integration")]
[Category("e2e")]
[Category("infrastructure")]
public class FakeTelegramClientExtendedTests : TestBase
{
    private FakeTelegramClient _fakeBot = null!;

    [SetUp]
    public void SetUp()
    {
        _fakeBot = new FakeTelegramClient();
    }

    [Test]
    public async Task FakeTelegramClient_ShouldTrackCallbackQueryAnswers()
    {
        // Arrange
        var callbackQueryId = "test_callback_123";
        var answerText = "Операция выполнена!";
        
        // Act
        await _fakeBot.AnswerCallbackQuery(callbackQueryId, answerText, showAlert: true);
        
        // Assert
        _fakeBot.AnsweredCallbackQueries.Should().HaveCount(1);
        _fakeBot.AnsweredCallbackQueries.First().CallbackQueryId.Should().Be(callbackQueryId);
        _fakeBot.AnsweredCallbackQueries.First().Text.Should().Be(answerText);
        _fakeBot.AnsweredCallbackQueries.First().ShowAlert.Should().Be(true);
        
        _fakeBot.WasCallbackQueryAnswered(callbackQueryId).Should().BeTrue();
    }

    [Test]
    public async Task FakeTelegramClient_ShouldTrackMessageEdits()
    {
        // Arrange
        var chatId = new ChatId(123456789);
        var messageId = 42;
        var newText = "Обновленный текст сообщения";
        
        // Act
        await _fakeBot.EditMessageText(chatId, messageId, newText);
        
        // Assert
        _fakeBot.EditedMessages.Should().HaveCount(1);
        _fakeBot.EditedMessages.First().ChatId.Should().Be(chatId.Identifier ?? 0);
        _fakeBot.EditedMessages.First().MessageId.Should().Be(messageId);
        _fakeBot.EditedMessages.First().Text.Should().Be(newText);
        
        _fakeBot.WasMessageEdited(chatId.Identifier ?? 0, messageId).Should().BeTrue();
    }

    [Test]
    public async Task FakeTelegramClient_ShouldTrackReplyMarkupEdits()
    {
        // Arrange
        var chatId = new ChatId(123456789);
        var messageId = 42;
        var replyMarkup = new InlineKeyboardMarkup(new[]
        {
            new InlineKeyboardButton("Кнопка 1") { CallbackData = "btn1" },
            new InlineKeyboardButton("Кнопка 2") { CallbackData = "btn2" }
        });
        
        // Act
        await _fakeBot.EditMessageReplyMarkup(chatId, messageId, replyMarkup);
        
        // Assert
        _fakeBot.EditedMessages.Should().HaveCount(1);
        _fakeBot.EditedMessages.First().ChatId.Should().Be(chatId.Identifier ?? 0);
        _fakeBot.EditedMessages.First().MessageId.Should().Be(messageId);
        _fakeBot.EditedMessages.First().ReplyMarkup.Should().Be(replyMarkup);
    }

    [Test]
    public async Task FakeTelegramClient_ShouldTrackPhotoSending()
    {
        // Arrange
        var chatId = new ChatId(123456789);
        var photo = "test_photo.jpg";
        var caption = "Тестовое фото";
        
        // Act
        await _fakeBot.SendPhoto(chatId, photo, caption);
        
        // Assert
        _fakeBot.SentPhotos.Should().HaveCount(1);
        _fakeBot.SentPhotos.First().ChatId.Should().Be(chatId.Identifier ?? 0);
        _fakeBot.SentPhotos.First().Photo.Should().Be(photo);
        _fakeBot.SentPhotos.First().Caption.Should().Be(caption);
        
        _fakeBot.WasPhotoSent(chatId.Identifier ?? 0, caption).Should().BeTrue();
        _fakeBot.WasPhotoSent(chatId.Identifier ?? 0, "фото").Should().BeTrue();
    }

    [Test]
    public async Task FakeTelegramClient_ShouldTrackUserRestrictions()
    {
        // Arrange
        var chatId = new ChatId(123456789);
        var userId = 987654321L;
        var permissions = new ChatPermissions
        {
            CanSendMessages = false,
            CanSendPolls = false
        };
        var untilDate = DateTime.UtcNow.AddHours(1);
        
        // Act
        await _fakeBot.RestrictChatMember(chatId, userId, permissions, untilDate);
        
        // Assert
        _fakeBot.RestrictedUsers.Should().HaveCount(1);
        _fakeBot.RestrictedUsers.First().ChatId.Should().Be(chatId.Identifier ?? 0);
        _fakeBot.RestrictedUsers.First().UserId.Should().Be(userId);
        _fakeBot.RestrictedUsers.First().Permissions.Should().Be(permissions);
        _fakeBot.RestrictedUsers.First().UntilDate.Should().Be(untilDate);
        
        _fakeBot.WasUserRestricted(chatId.Identifier ?? 0, userId).Should().BeTrue();
    }

    [Test]
    public async Task FakeTelegramClient_ShouldTrackOperationLog()
    {
        // Arrange
        var chatId = new ChatId(123456789);
        var messageId = 42;
        var callbackQueryId = "test_callback";
        
        // Act - выполняем несколько операций
        await _fakeBot.SendMessageAsync(chatId, "Тестовое сообщение");
        await _fakeBot.EditMessageText(chatId, messageId, "Обновленный текст");
        await _fakeBot.AnswerCallbackQuery(callbackQueryId, "Ответ");
        
        // Assert
        var operationLog = _fakeBot.GetOperationLog();
        operationLog.Should().HaveCount(3);
        operationLog.Should().Contain(log => log.Contains("SendMessageAsync"));
        operationLog.Should().Contain(log => log.Contains("EditMessageText"));
        operationLog.Should().Contain(log => log.Contains("AnswerCallbackQuery"));
    }

    [Test]
    public async Task FakeTelegramClient_ShouldClearOperationLog()
    {
        // Arrange
        var chatId = new ChatId(123456789);
        await _fakeBot.SendMessageAsync(chatId, "Тестовое сообщение");
        
        // Act
        _fakeBot.ClearOperationLog();
        
        // Assert
        _fakeBot.GetOperationLog().Should().BeEmpty();
        _fakeBot.SentMessages.Should().HaveCount(1); // Сообщения остаются
    }

    [Test]
    public async Task FakeTelegramClient_ShouldResetAllCollections()
    {
        // Arrange
        var chatId = new ChatId(123456789);
        var callbackQueryId = "test_callback";
        
        // Выполняем различные операции
        await _fakeBot.SendMessageAsync(chatId, "Тестовое сообщение");
        await _fakeBot.SendPhoto(chatId, "photo.jpg", "Тестовое фото");
        await _fakeBot.EditMessageText(chatId, 42, "Обновленный текст");
        await _fakeBot.AnswerCallbackQuery(callbackQueryId, "Ответ");
        await _fakeBot.RestrictChatMember(chatId, 123L, new ChatPermissions());
        
        // Act
        _fakeBot.Reset();
        
        // Assert
        _fakeBot.SentMessages.Should().BeEmpty();
        _fakeBot.SentPhotos.Should().BeEmpty();
        _fakeBot.EditedMessages.Should().BeEmpty();
        _fakeBot.AnsweredCallbackQueries.Should().BeEmpty();
        _fakeBot.RestrictedUsers.Should().BeEmpty();
        _fakeBot.GetOperationLog().Should().BeEmpty();
    }

    [Test]
    public async Task FakeTelegramClient_ShouldHandleExceptions()
    {
        // Arrange
        _fakeBot.ShouldThrowException = true;
        _fakeBot.ExceptionToThrow = new InvalidOperationException("Test exception");
        var chatId = new ChatId(123456789);
        
        // Act & Assert
        var action = () => _fakeBot.SendMessageAsync(chatId, "Тестовое сообщение");
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }
} 