using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test;

public class AiChecksMentionedChannelTests
{
    [Test]
    public async Task MentionedChannelNotFound_IsRoutineAndProfileCollectionContinues()
    {
        using var httpClient = TelegramHttpClient(_ =>
            TelegramResponse(HttpStatusCode.BadRequest, """{"ok":false,"error_code":400,"description":"Bad Request: chat not found"}""")
        );
        var bot = new TelegramBotClient("123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghi", httpClient);
        var logger = new RecordingLogger<AiChecks>();

        var collector = new ProfileInputCollector(bot, logger);

        var inputs = await collector.Collect(User(), UserChat("Mentioned channel: @missing_channel"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(inputs.MentionedChannels, Is.Empty);
            Assert.That(logger.Entries, Has.Count.EqualTo(1));
            Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Information));
            Assert.That(logger.Entries[0].Exception, Is.Null);
            Assert.That(logger.Entries[0].Message, Is.EqualTo("Unable to fetch mentioned channel @missing_channel: chat not found"));
        }
    }

    [Test]
    public async Task OtherMentionedChannelFailure_RemainsAWarningWithException()
    {
        using var httpClient = TelegramHttpClient(_ =>
            TelegramResponse(
                HttpStatusCode.BadRequest,
                """{"ok":false,"error_code":400,"description":"Bad Request: group chat was upgraded to a supergroup chat"}"""
            )
        );
        var bot = new TelegramBotClient("123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghi", httpClient);
        var logger = new RecordingLogger<AiChecks>();

        var collector = new ProfileInputCollector(bot, logger);

        var inputs = await collector.Collect(User(), UserChat("Mentioned channel: @moved_channel"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(inputs.MentionedChannels, Is.Empty);
            Assert.That(logger.Entries, Has.Count.EqualTo(1));
            Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Warning));
            Assert.That(logger.Entries[0].Exception, Is.TypeOf<ApiRequestException>());
            Assert.That(logger.Entries[0].Message, Is.EqualTo("Unable to fetch mentioned channel @moved_channel"));
        }
    }

    [Test]
    public async Task MultipleMentions_RetainSuccessfulChannelAndSkipOnlyMissingChannel()
    {
        var responses = new Queue<HttpResponseMessage>([
            TelegramResponse(HttpStatusCode.BadRequest, """{"ok":false,"error_code":400,"description":"Bad Request: chat not found"}"""),
            TelegramResponse(
                HttpStatusCode.OK,
                """{"ok":true,"result":{"id":-100123,"type":"channel","title":"Working channel","username":"working_channel","description":"Useful details"}}"""
            ),
        ]);
        using var httpClient = TelegramHttpClient(_ => responses.Dequeue());
        var bot = new TelegramBotClient("123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghi", httpClient);
        var logger = new RecordingLogger<AiChecks>();

        var collector = new ProfileInputCollector(bot, logger);

        var inputs = await collector.Collect(User(), UserChat("Missing @missing_channel, working @working_channel"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(inputs.MentionedChannels, Has.Count.EqualTo(1));
            Assert.That(
                inputs.MentionedChannels[0].Text,
                Is.EqualTo(
                    "Информация об упомянутом канале:\nНазвание: Working channel\nЮзернейм: @working_channel\nОписание: Useful details"
                )
            );
            Assert.That(logger.Entries, Has.Count.EqualTo(1));
            Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Information));
        }
    }

    [Test]
    public void CancellationDuringMentionLookup_IsPropagated()
    {
        using var httpClient = TelegramHttpClient((_, cancellationToken) => Task.FromCanceled<HttpResponseMessage>(cancellationToken));
        var bot = new TelegramBotClient("123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghi", httpClient);
        var logger = new RecordingLogger<AiChecks>();
        var collector = new ProfileInputCollector(bot, logger);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(
            async () => await collector.Collect(User(), UserChat("Mentioned channel: @missing_channel"), cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>()
        );
        Assert.That(logger.Entries, Is.Empty);
    }

    private static Telegram.Bot.Types.User User() => new() { Id = 42, FirstName = "Test" };

    private static ChatFullInfo UserChat(string bio) =>
        new()
        {
            Id = 42,
            Type = ChatType.Private,
            FirstName = "Test",
            Bio = bio,
        };

    private static HttpClient TelegramHttpClient(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new DelegateHandler(respond));

    private static HttpClient TelegramHttpClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) =>
        new(new AsyncDelegateHandler(respond));

    private static HttpResponseMessage TelegramResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class AsyncDelegateHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) => Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
