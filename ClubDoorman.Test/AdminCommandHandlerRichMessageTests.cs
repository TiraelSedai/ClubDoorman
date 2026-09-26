using System.Net;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ClubDoorman.Test;

[NonParallelizable]
public sealed class AdminCommandHandlerRichMessageTests
{
    [Test]
    public async Task HamReplyToRichMessage_SavesTextAndHiddenLinksAndConfirms()
    {
        var environment = new Dictionary<string, string?>
        {
            ["DOORMAN_BOT_API"] = "123456:TEST_TOKEN",
            ["DOORMAN_ADMIN_CHAT"] = "1",
            ["DOORMAN_ADMIN_CHAT_MAP"] = null,
            ["DOORMAN_CLUB_SERVICE_TOKEN"] = null,
        };
        var previous = environment.Keys.ToDictionary(key => key, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var (key, value) in environment)
                Environment.SetEnvironmentVariable(key, value);
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            using var handler = new TelegramHandler();
            using var http = new HttpClient(handler);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHybridCache();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
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
            await using var provider = services.BuildServiceProvider();
            using (var scope = provider.CreateScope())
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();

            await provider
                .GetRequiredService<AdminCommandHandler>()
                .AdminChatMessage(
                    new Message
                    {
                        Id = 2,
                        Chat = new Chat { Id = 1 },
                        Text = "/ham",
                        ReplyToMessage = RichMessageTextTests.BookReview(),
                    }
                );

            var records = await provider.GetRequiredService<SpamHamClassifier>().GetLatestSpamHamRecords(10);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    records.Select(r => r.Text),
                    Is.EqualTo(
                        new[]
                        {
                            "День опричника Первая строка Вторая строка Читать https://example.org/book?a=1&b=2 книгу <целиком> & обсудить",
                        }
                    )
                );
                Assert.That(records.All(r => !r.IsSpam), Is.True);
                Assert.That(handler.SentMessages, Is.EqualTo(1));
            }
        }
        finally
        {
            foreach (var (key, value) in previous)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private sealed class TelegramHandler : HttpMessageHandler
    {
        public int SentMessages { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var method = request.RequestUri!.Segments.Last();
            if (method == "sendMessage")
                SentMessages++;
            var json = method switch
            {
                "getMe" => """{"ok":true,"result":{"id":123456,"is_bot":true,"first_name":"Bot"}}""",
                "sendMessage" => """{"ok":true,"result":{"message_id":3,"date":0,"chat":{"id":1,"type":"private"}}}""",
                _ => throw new InvalidOperationException($"Unexpected Telegram method: {method}"),
            };
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") }
            );
        }
    }
}
