using System.Collections.Frozen;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test;

[NonParallelizable]
public sealed class AdminCommandHandlerAutomaticExampleTests
{
    private const long AdminChat = -1001111111111;
    private const long MappedAdminChat = -1002222222222;
    private const long SourceChat = -1003333333333;
    private const string VisibleText = "Ссылка на длинное рекламное предложение тут";
    private const string ExpandedText = "Ссылка на длинное рекламное предложение https://example.org/campaign тут";

    [TestCase(true)]
    [TestCase(false)]
    public async Task AutomaticExample_PersistsOriginalTextAndLabel_ReportsSavedIdToCentralAdmin_AndUndoRemovesIt(bool spam)
    {
        await using var fixture = await Fixture.Create();
        var message = SourceMessage();
        var badMessageManager = fixture.Services.GetRequiredService<BadMessageManager>();
        var handler = fixture.Services.GetRequiredService<AdminCommandHandler>();

        await handler.AddAutomaticExample(message, spam, "Jev и Luna согласны");

        var records = await fixture.Services.GetRequiredService<SpamHamClassifier>().GetLatestSpamHamRecords(10);
        var calls = fixture.Calls.Where(call => call.Method is "forwardMessage" or "sendMessage").ToArray();
        var report = Body(calls[1]).GetProperty("text").GetString()!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(records, Has.Count.EqualTo(2));
            Assert.That(records[0].Id, Is.EqualTo(2));
            Assert.That(records[0].Text, Is.EqualTo($"Цитата {ExpandedText}"));
            Assert.That(records[0].IsSpam, Is.EqualTo(spam));
            Assert.That(calls.Select(call => Body(call).GetProperty("chat_id").GetInt64()), Is.All.EqualTo(AdminChat));
            Assert.That(calls.Select(call => call.Method), Is.EqualTo(new[] { "forwardMessage", "sendMessage" }));
            Assert.That(Body(calls[0]).GetProperty("from_chat_id").GetInt64(), Is.EqualTo(SourceChat));
            Assert.That(Body(calls[0]).GetProperty("message_id").GetInt32(), Is.EqualTo(73));
            Assert.That(Body(calls[1]).GetProperty("reply_parameters").GetProperty("message_id").GetInt32(), Is.EqualTo(301));
            Assert.That(report, Does.Contain("Запись #2"));
            Assert.That(report, Does.Contain("/undo 2"));
            Assert.That(report, Does.Contain($"чат {SourceChat}, сообщение #73"));
            Assert.That(badMessageManager.KnownBadMessage(VisibleText), Is.False);
            Assert.That(badMessageManager.KnownBadMessage($"Цитата {ExpandedText}"), Is.False);
        }

        await handler.AdminChatMessage(
            new Message
            {
                Chat = new Chat { Id = AdminChat },
                Text = "/undo 2",
                Id = 74,
            }
        );

        records = await fixture.Services.GetRequiredService<SpamHamClassifier>().GetLatestSpamHamRecords(10);
        var undoConfirmation = Body(fixture.Calls.Last()).GetProperty("text").GetString()!;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(records.Select(record => record.Text), Is.EqualTo(new[] { "older ham" }));
            Assert.That(undoConfirmation, Does.Contain("Запись #2"));
            Assert.That(badMessageManager.KnownBadMessage(VisibleText), Is.False);
        }
    }

    [Test]
    public async Task ForwardFailure_SendsExpandedOriginalSnapshotAndReviewReplyWithoutLosingDatasetRecord()
    {
        await using var fixture = await Fixture.Create(failForward: true);

        await fixture.Services.GetRequiredService<AdminCommandHandler>().AddAutomaticExample(SourceMessage(), true, "Jev и Luna: spam");

        var record = (await fixture.Services.GetRequiredService<SpamHamClassifier>().GetLatestSpamHamRecords(10))[0];
        var calls = fixture.Calls.Where(call => call.Method is "forwardMessage" or "sendMessage").ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(record.Id, Is.EqualTo(2));
            Assert.That(record.Text, Is.EqualTo($"Цитата {ExpandedText}"));
            Assert.That(record.IsSpam, Is.True);
            Assert.That(calls.Select(call => call.Method), Is.EqualTo(new[] { "forwardMessage", "sendMessage", "sendMessage" }));
            Assert.That(calls.Select(call => Body(call).GetProperty("chat_id").GetInt64()), Is.All.EqualTo(AdminChat));
            Assert.That(Body(calls[1]).GetProperty("text").GetString(), Is.EqualTo($"Цитата {ExpandedText}"));
            Assert.That(Body(calls[2]).GetProperty("reply_parameters").GetProperty("message_id").GetInt32(), Is.EqualTo(302));
            Assert.That(Body(calls[2]).GetProperty("text").GetString(), Does.Contain("/undo 2"));
        }
    }

    [Test]
    public async Task ForwardFailure_WithLongSnapshotAndReason_KeepsMessagesWithinTelegramLimitAndUndoVisible()
    {
        await using var fixture = await Fixture.Create(failForward: true);
        var source = new Message
        {
            Id = 73,
            Chat = new Chat { Id = SourceChat },
            Text = $"{VisibleText} {new string('x', 4060)}",
            Entities =
            [
                new MessageEntity
                {
                    Type = MessageEntityType.TextLink,
                    Offset = 40,
                    Length = 3,
                    Url = "https://example.org/campaign",
                },
            ],
        };

        await fixture.Services.GetRequiredService<AdminCommandHandler>().AddAutomaticExample(source, false, new string('r', 9000));

        var sends = fixture
            .Calls.Where(call => call.Method == "sendMessage")
            .Select(call => Body(call).GetProperty("text").GetString()!)
            .ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(sends, Has.Length.EqualTo(2));
            Assert.That(sends.Select(text => text.Length), Is.All.LessThanOrEqualTo(4096));
            Assert.That(sends[0], Does.Contain("https://example.org/campaign"));
            Assert.That(sends[1], Does.Contain("/undo 2"));
        }
    }

    private static Message SourceMessage()
    {
        var source = JsonSerializer.Deserialize<Message>(
            """
            {"message_id":73,"date":0,"chat":{"id":-1003333333333,"type":"supergroup"},"text":"Ссылка на длинное рекламное предложение тут","quote":{"text":"Цитата"}}
            """,
            Telegram.Bot.JsonBotAPI.Options
        )!;
        source.Entities =
        [
            new MessageEntity
            {
                Type = MessageEntityType.TextLink,
                Offset = 40,
                Length = 3,
                Url = "https://example.org/campaign",
            },
        ];
        return source;
    }

    private static JsonElement Body((string Method, string Json) call)
    {
        using var document = JsonDocument.Parse(call.Json);
        return document.RootElement.Clone();
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly HttpClient _http;
        private readonly Dictionary<string, string?> _previousEnvironment;

        private Fixture(
            ServiceProvider services,
            SqliteConnection connection,
            HttpClient http,
            Dictionary<string, string?> previousEnvironment
        )
        {
            Services = services;
            _connection = connection;
            _http = http;
            _previousEnvironment = previousEnvironment;
        }

        public ServiceProvider Services { get; }
        public List<(string Method, string Json)> Calls { get; } = [];

        public static async Task<Fixture> Create(bool failForward = false)
        {
            var environment = new Dictionary<string, string?>
            {
                ["DOORMAN_BOT_API"] = "123456:TEST_TOKEN",
                ["DOORMAN_ADMIN_CHAT"] = AdminChat.ToString(),
                ["DOORMAN_ADMIN_CHAT_MAP"] = null,
                ["DOORMAN_CLUB_SERVICE_TOKEN"] = null,
            };
            var previous = environment.Keys.ToDictionary(key => key, Environment.GetEnvironmentVariable);
            foreach (var (key, value) in environment)
                Environment.SetEnvironmentVariable(key, value);

            var db = new SqliteConnection("Data Source=:memory:");
            await db.OpenAsync();
            Fixture? fixture = null;
            var http = new HttpClient(
                new Handler(
                    async (request, ct) =>
                    {
                        var method = request.RequestUri!.Segments.Last();
                        var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct);
                        fixture!.Calls.Add((method, body));
                        if (method == "forwardMessage" && failForward)
                            return JsonResponse(
                                new
                                {
                                    ok = false,
                                    error_code = 400,
                                    description = "Bad Request: message to forward not found",
                                },
                                HttpStatusCode.BadRequest
                            );
                        var result = method switch
                        {
                            "forwardMessage" => new
                            {
                                message_id = 301,
                                date = 0,
                                chat = new { id = AdminChat, type = "supergroup" },
                            },
                            "sendMessage" => new
                            {
                                message_id = 302,
                                date = 0,
                                chat = new { id = AdminChat, type = "supergroup" },
                            },
                            _ => throw new InvalidOperationException($"Unexpected Telegram method: {method}"),
                        };
                        return JsonResponse(new { ok = true, result });
                    }
                )
            );
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHybridCache();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(db));
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient("123456:TEST_TOKEN", http));
            services.AddSingleton<Config>();
            services.AddSingleton<UserManager>();
            services.AddSingleton<BadMessageManager>();
            services.AddSingleton<RecentMessagesStorage>();
            services.AddSingleton<SpamDeduplicationCache>();
            services.AddSingleton(provider => new SpamHamClassifier(
                NullLogger<SpamHamClassifier>.Instance,
                provider.GetRequiredService<IServiceScopeFactory>(),
                SpamHamClassifierStartupMode.SkipBackgroundTraining
            ));
            services.AddSingleton<AdminCommandHandler>();
            var provider = services.BuildServiceProvider();
            fixture = new Fixture(provider, db, http, previous);
            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await context.Database.EnsureCreatedAsync();
                context.SpamHamRecords.Add(new SpamHamRecord { Text = "older ham", IsSpam = false });
                await context.SaveChangesAsync();
            }
            var config = provider.GetRequiredService<Config>();
            typeof(Config)
                .GetProperty(nameof(Config.MultiAdminChatMap))!
                .SetValue(config, new Dictionary<long, long> { [SourceChat] = MappedAdminChat }.ToFrozenDictionary());
            Assert.That(config.GetAdminChat(SourceChat), Is.EqualTo(MappedAdminChat));
            return fixture;
        }

        private static HttpResponseMessage JsonResponse(object response, HttpStatusCode status = HttpStatusCode.OK) =>
            new(status) { Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json") };

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            _http.Dispose();
            await _connection.DisposeAsync();
            foreach (var (key, value) in _previousEnvironment)
                Environment.SetEnvironmentVariable(key, value);
        }

        private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                respond(request, cancellationToken);
        }
    }
}
