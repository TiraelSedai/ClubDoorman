using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using tryAGI.OpenAI;
using Message = Telegram.Bot.Types.Message;
using User = Telegram.Bot.Types.User;

namespace ClubDoorman.Test;

[NonParallelizable]
public sealed class EroticProfileReviewTests
{
    private const long PaidChat = -1001234567890;
    private const long FreeChat = -1009876543210;
    private const long AdminChat = -1001111111111;
    private const string PrimaryModel = "openai/gpt-6-luna:floor";
    private const string MiMo = "xiaomi/mimo-v2.6-flash:floor";
    private const string Glm = "z-ai/glm-5.3-flash:floor";
    private static long _nextUserId = 1000;
    private readonly Dictionary<string, string?> _previousEnvironment = [];
    private readonly ConcurrentQueue<JsonElement> _llmRequests = new();
    private readonly List<(string Method, string Body)> _telegramRequests = [];
    private readonly Dictionary<string, double> _scores = [];
    private double _gamblingScore;
    private double _nonPersonScore;
    private double _selfPromotionScore;
    private SqliteConnection _db = null!;
    private ServiceProvider _services = null!;
    private HttpClient _telegramHttp = null!;
    private HttpClient _llmHttp = null!;
    private OpenAiClient _api = null!;
    private AiChecks _checks = null!;
    private User _user = null!;
    private ChatFullInfo _profile = null!;
    private string? _failedModel;
    private TaskCompletionSource? _reviewBarrier;
    private int _reviewsStarted;

    [SetUp]
    public async Task SetUp()
    {
        var variables = new Dictionary<string, string?>
        {
            ["DOORMAN_BOT_API"] = "123456:TEST_TOKEN",
            ["DOORMAN_ADMIN_CHAT"] = AdminChat.ToString(),
            ["DOORMAN_ADMIN_CHAT_MAP"] = $"{PaidChat}={AdminChat}",
            ["DOORMAN_OPENROUTER_API"] = "sk-test",
            ["DOORMAN_EROTIC_AUTOBAN_ENABLE"] = "1",
            ["DOORMAN_FREE_LLM_URL"] = "https://llm.test/v1",
            ["DOORMAN_FREE_LLM_MODEL"] = PrimaryModel,
            ["DOORMAN_FREE_LLM_API"] = "sk-test",
            ["DOORMAN_CLUB_SERVICE_TOKEN"] = null,
        };
        foreach (var (key, value) in variables)
        {
            _previousEnvironment[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }
        _llmRequests.Clear();
        _telegramRequests.Clear();
        _scores[PrimaryModel] = 0.75;
        _scores[MiMo] = 0.85;
        _scores[Glm] = 0.85;
        _gamblingScore = 0;
        _nonPersonScore = 0;
        _selfPromotionScore = 0;
        _failedModel = null;
        _reviewBarrier = null;
        _reviewsStarted = 0;
        _user = new User
        {
            Id = Interlocked.Increment(ref _nextUserId),
            FirstName = "Test",
            Username = "test_user",
        };
        _profile = new ChatFullInfo
        {
            Id = _user.Id,
            Type = ChatType.Private,
            FirstName = "Test",
            Bio = "Profile bio",
        };

        _db = new SqliteConnection("Data Source=:memory:");
        await _db.OpenAsync();
        _telegramHttp = new HttpClient(new Handler(RespondTelegram));
        _llmHttp = new HttpClient(new Handler(RespondLlm));
        _api = new OpenAiClient("sk-test", _llmHttp, baseUri: new Uri("https://llm.test/v1"));
        _api.Options.Retry = new AutoSDKRetryOptions { MaxAttempts = 1 };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_db));
        services.AddSingleton<Config>();
        services.AddSingleton<ITelegramBotClient>(new TelegramBotClient("123456:TEST_TOKEN", _telegramHttp));
        services.AddSingleton<UserManager>();
        services.AddSingleton(_telegramHttp);
        services.AddSingleton<TelegramInvitePreviews>();
        services.AddSingleton<JevChecks>();
        services.AddSingleton(provider => new AiChecks(
            provider.GetRequiredService<ITelegramBotClient>(),
            provider.GetRequiredService<Config>(),
            provider.GetRequiredService<HybridCache>(),
            provider.GetRequiredService<UserManager>(),
            NullLogger<AiChecks>.Instance,
            provider.GetRequiredService<TelegramInvitePreviews>(),
            _api,
            _api
        ));
        services.AddSingleton<ReactionHandler>();
        services.AddSingleton<MessageProcessor>();
        services.AddSingleton<CaptchaManager>();
        services.AddSingleton<StatisticsReporter>();
        services.AddSingleton(provider => new SpamHamClassifier(
            NullLogger<SpamHamClassifier>.Instance,
            provider.GetRequiredService<IServiceScopeFactory>(),
            SpamHamClassifierStartupMode.SkipBackgroundTraining
        ));
        services.AddSingleton<AdminCommandHandler>();
        services.AddSingleton<BadMessageManager>();
        services.AddSingleton<RecentMessagesStorage>();
        services.AddSingleton<SpamDeduplicationCache>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<BioInviteTracker>();
        _services = services.BuildServiceProvider();
        using var scope = _services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        var cache = _services.GetRequiredService<HybridCache>();
        await cache.SetAsync($"full_chan:{PaidChat}", new Config.ChatInfo(PaidChat, "Paid"));
        await cache.SetAsync($"full_chan:{AdminChat}", new Config.ChatInfo(AdminChat, "Admin"));
        await cache.SetAsync($"user:banned:{_user.Id}", false);
        _checks = _services.GetRequiredService<AiChecks>();
    }

    [TearDown]
    public void TearDown()
    {
        _services.Dispose();
        _api.Dispose();
        _llmHttp.Dispose();
        _telegramHttp.Dispose();
        _db.Dispose();
        foreach (var (key, value) in _previousEnvironment)
            Environment.SetEnvironmentVariable(key, value);
    }

    [TestCase(0.749, 1, false)]
    [TestCase(0.75, 3, true)]
    [TestCase(0.9, 3, true)]
    public async Task PaidProfile_ReviewsAtLowProbabilityBoundary(double initial, int requests, bool confirmed)
    {
        _scores[PrimaryModel] = initial;
        var verdict = await Check();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_llmRequests, Has.Count.EqualTo(requests));
            Assert.That(verdict.Review?.Confirmed == true, Is.EqualTo(confirmed));
            Assert.That(verdict.Probability.EroticProbability, Is.EqualTo(initial));
        }
    }

    [TestCase(0.85, 0.85, true)]
    [TestCase(0.849, 1, false)]
    [TestCase(1, 0.849, false)]
    [TestCase(0.75, 0.75, false)]
    public async Task BothReviewersMustReachReviewProbability(double mimo, double glm, bool confirmed)
    {
        _scores[MiMo] = mimo;
        _scores[Glm] = glm;
        var verdict = await Check();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(verdict.Review, Is.Not.Null);
            Assert.That(verdict.Review!.Confirmed, Is.EqualTo(confirmed));
            Assert.That(_llmRequests.Select(x => x.GetProperty("model").GetString()), Is.EquivalentTo(new[] { PrimaryModel, MiMo, Glm }));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ReviewersReceiveIdenticalPromptImagesAndSchema_AndRunConcurrently(bool eroticOnly)
    {
        _profile.Bio = eroticOnly ? null : "Profile bio";
        _profile.LinkedChatId = eroticOnly ? null : -1002222222222;
        _profile.Photo = new ChatPhoto
        {
            BigFileId = "avatar",
            BigFileUniqueId = "avatar-unique",
            SmallFileId = "small",
            SmallFileUniqueId = "small-unique",
        };
        _reviewBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var verdict = await Check();
        var requests = _llmRequests.ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(verdict.Review?.Confirmed, Is.True);
            Assert.That(_reviewsStarted, Is.EqualTo(2));
            Assert.That(requests, Has.Length.EqualTo(3));
            foreach (var request in requests)
                Assert.That(request.GetProperty("max_completion_tokens").GetInt32(), Is.EqualTo(2048));
            foreach (var request in requests.Skip(1))
            {
                Assert.That(request.GetProperty("messages").GetRawText(), Is.EqualTo(requests[0].GetProperty("messages").GetRawText()));
                Assert.That(
                    request.GetProperty("response_format").GetRawText(),
                    Is.EqualTo(requests[0].GetProperty("response_format").GetRawText())
                );
            }
            Assert.That(requests[0].GetProperty("messages").GetRawText(), Does.Contain("data:image/jpeg;base64,AQIDBA=="));
            Assert.That(verdict.Photo, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            if (!eroticOnly)
                Assert.That(requests[0].GetProperty("messages").GetRawText(), Does.Contain("Channel bio"));
        }
    }

    [Test]
    public async Task FreeProfileRequestLimitsCompletionTokens()
    {
        var verdict = await _checks.GetAttentionBaitProbability(FreeChat, _user, _profile);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(verdict.Probability.EroticProbability, Is.EqualTo(0.75));
            Assert.That(_llmRequests, Has.Count.EqualTo(1));
            Assert.That(_llmRequests.Single().GetProperty("max_completion_tokens").GetInt32(), Is.EqualTo(2048));
        }
    }

    [TestCase(PaidChat)]
    [TestCase(FreeChat)]
    public async Task SpamRequestLimitsCompletionTokens(long chatId)
    {
        var message = new Message
        {
            Id = 123,
            From = _user,
            Chat = new Chat
            {
                Id = chatId,
                Type = ChatType.Supergroup,
                Title = "Test",
            },
            Text = "ordinary message with enough text",
        };
        var verdict = await _checks.GetSpamProbability(message);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(verdict.IsAvailable, Is.True);
            Assert.That(_llmRequests, Has.Count.EqualTo(1));
            Assert.That(_llmRequests.Single().GetProperty("max_completion_tokens").GetInt32(), Is.EqualTo(2048));
        }
    }

    [Test]
    public async Task FreeProfileNeverSpendsOnReview_EvenWithSameModelAndCachedPaidConfirmation()
    {
        _scores[PrimaryModel] = 0.95;
        Assert.That((await Check()).Review?.Confirmed, Is.True);
        _llmRequests.Clear();
        var verdict = await _checks.GetAttentionBaitProbability(FreeChat, _user, _profile);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(verdict.Review, Is.Null);
            Assert.That(verdict.Probability.EroticProbability, Is.EqualTo(0.95));
            Assert.That(_llmRequests, Is.Empty);
        }
    }

    [Test]
    public async Task FreeProfileWithHighGamblingProbability_IsReportedWithoutModeration()
    {
        _scores[PrimaryModel] = 0;
        _gamblingScore = 0.95;
        _nonPersonScore = 0.9;
        _selfPromotionScore = 0.2;
        var message = new Message
        {
            Id = 123,
            From = _user,
            Chat = new Chat
            {
                Id = FreeChat,
                Type = ChatType.Supergroup,
                Title = "Free",
            },
            Text = "Hello",
        };
        using var cancellation = new CancellationTokenSource();
        var method = typeof(MessageProcessor).GetMethod("FreeChatLlmChecks", BindingFlags.Instance | BindingFlags.NonPublic)!;

        await (Task)method.Invoke(_services.GetRequiredService<MessageProcessor>(), [message, _user, _profile, cancellation.Token])!;
        await cancellation.CancelAsync();

        var sentMessages = _telegramRequests.Where(x => x.Method == "sendMessage").ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_telegramRequests.Count(x => x.Method == "forwardMessage"), Is.EqualTo(1));
            Assert.That(sentMessages, Has.Length.EqualTo(2));
            Assert.That(sentMessages.Select(x => x.Body), Has.All.Contain($"Verdict from {PrimaryModel}"));
            Assert.That(
                _telegramRequests,
                Has.None.Matches<(string Method, string Body)>(x => x.Method is "banChatMember" or "deleteMessage" or "restrictChatMember")
            );
        }
    }

    [TestCase(0.85)]
    [TestCase(0.5)]
    public async Task RepeatedProfileReusesAllVerdicts_ChangedProfileAsksAllModelsAgain(double mimo)
    {
        _scores[MiMo] = mimo;
        var first = await Check();
        var second = await Check();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_llmRequests, Has.Count.EqualTo(3));
            Assert.That(second.Review?.Confirmed, Is.EqualTo(first.Review!.Confirmed));
            Assert.That(second.Review?.MiMo.EroticProbability, Is.EqualTo(mimo));
            Assert.That(second.Review?.Glm.EroticProbability, Is.EqualTo(0.85));
            Assert.That(second.Review?.Reason, Is.EqualTo(first.Review.Reason));
        }
        _profile.Bio = "Changed profile";
        await Check();
        Assert.That(_llmRequests, Has.Count.EqualTo(6));
    }

    [TestCase(MiMo)]
    [TestCase(Glm)]
    public async Task FailedReviewerPreservesInitialVerdict_AndRetriesNextTime(string failedModel)
    {
        _failedModel = failedModel;
        var failed = await Check();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(failed.Review, Is.Null);
            Assert.That(failed.Probability.EroticProbability, Is.EqualTo(0.75));
        }
        _failedModel = null;
        _llmRequests.Clear();
        var retried = await Check();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(retried.Review?.Confirmed, Is.True);
            Assert.That(_llmRequests.Select(x => x.GetProperty("model").GetString()), Is.EquivalentTo(new[] { MiMo, Glm }));
        }
    }

    [Test]
    public async Task HalfApprovedUserNeverCallsAnyModel()
    {
        await _services.GetRequiredService<UserManager>().HalfApprove(_user.Id);
        var verdict = await Check();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(verdict.Review, Is.Null);
            Assert.That(_llmRequests, Is.Empty);
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("0")]
    [TestCase("false")]
    public async Task DisabledToggleSkipsReviews_ForFreshProfilesAndCachedConfirmations(string? setting)
    {
        Assert.That((await Check()).Review?.Confirmed, Is.True);
        Environment.SetEnvironmentVariable("DOORMAN_EROTIC_AUTOBAN_ENABLE", setting);
        var cache = _services.GetRequiredService<HybridCache>();
        var config = new Config(cache, NullLogger<Config>.Instance);
        _checks = new AiChecks(
            _services.GetRequiredService<ITelegramBotClient>(),
            config,
            cache,
            _services.GetRequiredService<UserManager>(),
            NullLogger<AiChecks>.Instance,
            _services.GetRequiredService<TelegramInvitePreviews>(),
            _api,
            _api
        );
        _llmRequests.Clear();

        var cached = await Check();
        _profile.Bio = "Changed profile while toggle is disabled";
        var fresh = await Check();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.EroticAutoBan, Is.False);
            Assert.That(cached.Review, Is.Null);
            Assert.That(fresh.Review, Is.Null);
            Assert.That(fresh.Probability.EroticProbability, Is.EqualTo(0.75));
            Assert.That(_llmRequests.Select(x => x.GetProperty("model").GetString()), Is.EqualTo(new[] { PrimaryModel }));
        }
    }

    [TestCase(0.75, 0.85, true, false)]
    [TestCase(0.75, 0.849, false, false)]
    [TestCase(0.95, 0.849, false, true)]
    public async Task MessageProfileCheck_BansOnlyOnAgreement_OtherwiseKeepsExistingModeration(
        double primary,
        double mimo,
        bool ban,
        bool restrict
    )
    {
        _scores[PrimaryModel] = primary;
        _scores[MiMo] = mimo;
        // Populate the same cache used by other profile consumers, without starting a profile watcher in this test.
        await Check();
        var message = new Message
        {
            Id = 123,
            From = _user,
            Chat = PaidGroup(),
            Text = "Hello",
        };
        var result = await _services
            .GetRequiredService<MessageProcessor>()
            .CheckUserProfile(message, _user, _profile, message.Text, message.Chat, AdminChat, CancellationToken.None);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_telegramRequests.Any(x => x.Method == "banChatMember"), Is.EqualTo(ban));
            Assert.That(_telegramRequests.Any(x => x.Method == "deleteMessage"), Is.EqualTo(ban || restrict));
            Assert.That(_telegramRequests.Any(x => x.Method == "restrictChatMember"), Is.EqualTo(restrict));
            Assert.That(result, Is.EqualTo(ban || restrict ? CheckResult.NoMoreAction : CheckResult.Suspicious));
            if (ban)
            {
                Assert.That(_telegramRequests.Single(x => x.Method == "banChatMember").Body, Does.Contain(_user.Id.ToString()));
                Assert.That(_telegramRequests.Single(x => x.Method == "sendMessage").Body, Does.Contain(MiMo).And.Contain(Glm));
                Assert.That(_services.GetRequiredService<StatisticsReporter>().Stats[PaidChat].Autoban, Is.EqualTo(1));
            }
        }
    }

    [TestCase(0.85, true)]
    [TestCase(0.849, false)]
    public async Task Reaction_BansOnlyOnAgreement(double glm, bool ban)
    {
        _scores[Glm] = glm;
        await _services
            .GetRequiredService<ReactionHandler>()
            .HandleReaction(
                new MessageReactionUpdated
                {
                    Chat = PaidGroup(),
                    User = _user,
                    MessageId = 123,
                    Date = DateTime.UtcNow,
                    OldReaction = [],
                    NewReaction = [new ReactionTypeEmoji { Emoji = "👍" }],
                }
            );
        using (Assert.EnterMultipleScope())
        {
            Assert.That(_telegramRequests.Any(x => x.Method == "banChatMember"), Is.EqualTo(ban));
            Assert.That(_telegramRequests.Any(x => x.Method == "sendMessage"), Is.True);
            Assert.That(_telegramRequests.Any(x => x.Method == "deleteMessage"), Is.False);
            if (ban)
                Assert.That(_telegramRequests.Single(x => x.Method == "sendMessage").Body, Does.Contain(MiMo).And.Contain(Glm));
        }
    }

    private ValueTask<AiChecks.SpamPhotoBio> Check() => _checks.GetAttentionBaitProbability(PaidChat, _user, _profile);

    private static Chat PaidGroup() =>
        new()
        {
            Id = PaidChat,
            Type = ChatType.Supergroup,
            Title = "Paid",
        };

    private async Task<HttpResponseMessage> RespondLlm(HttpRequestMessage request, CancellationToken ct)
    {
        using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
        var root = body.RootElement;
        _llmRequests.Enqueue(root.Clone());
        var model = root.GetProperty("model").GetString()!;
        if (model == _failedModel)
            throw new HttpRequestException($"Test failure for {model}");
        if (model != PrimaryModel && _reviewBarrier != null)
        {
            if (Interlocked.Increment(ref _reviewsStarted) == 2)
                _reviewBarrier.TrySetResult();
            await _reviewBarrier.Task.WaitAsync(TimeSpan.FromSeconds(3), ct);
        }
        var schema = root.GetProperty("response_format").GetProperty("json_schema").GetProperty("schema");
        var eroticOnly = schema.GetProperty("properties").TryGetProperty("Probability", out _);
        var content = eroticOnly
            ? JsonSerializer.Serialize(new AiChecks.SpamProbability { Probability = _scores[model], Reason = $"Verdict from {model}" })
            : JsonSerializer.Serialize(
                new AiChecks.BioClassProbability
                {
                    EroticProbability = _scores[model],
                    GamblingProbability = _gamblingScore,
                    NonPersonProbability = _nonPersonScore,
                    SelfPromotionProbability = _selfPromotionScore,
                    Reason = $"Verdict from {model}",
                }
            );
        return JsonResponse(
            new
            {
                id = "chatcmpl-test",
                @object = "chat.completion",
                created = 0,
                model,
                choices = new[]
                {
                    new
                    {
                        index = 0,
                        message = new { role = "assistant", content },
                        finish_reason = "stop",
                    },
                },
            }
        );
    }

    private async Task<HttpResponseMessage> RespondTelegram(HttpRequestMessage request, CancellationToken ct)
    {
        var method = request.RequestUri!.Segments.Last();
        var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct);
        _telegramRequests.Add((method, body));
        return method switch
        {
            "getFile" => JsonResponse(
                new
                {
                    ok = true,
                    result = new
                    {
                        file_id = "avatar",
                        file_unique_id = "avatar-unique",
                        file_path = "avatar.jpg",
                    },
                }
            ),
            "avatar.jpg" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3, 4]) },
            "getChat" => JsonResponse(
                new
                {
                    ok = true,
                    result = body.Contains(_user.Id.ToString(), StringComparison.Ordinal)
                        ? JsonSerializer.SerializeToElement(_profile, Telegram.Bot.JsonBotAPI.Options)
                        : JsonSerializer.SerializeToElement(
                            new
                            {
                                id = -1002222222222,
                                type = "channel",
                                title = "Channel",
                                description = "Channel bio",
                            }
                        ),
                }
            ),
            "sendMessage" or "forwardMessage" => JsonResponse(
                new
                {
                    ok = true,
                    result = new
                    {
                        message_id = 124,
                        date = 0,
                        chat = new { id = AdminChat, type = "supergroup" },
                    },
                }
            ),
            "banChatMember" or "deleteMessage" or "restrictChatMember" => JsonResponse(new { ok = true, result = true }),
            _ => throw new InvalidOperationException($"Unexpected Telegram request: {method}"),
        };
    }

    private static HttpResponseMessage JsonResponse(object value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request, cancellationToken);
    }
}
