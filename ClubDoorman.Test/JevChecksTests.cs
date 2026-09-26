using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClubDoorman.Test;

[NonParallelizable]
public class JevChecksTests
{
    private const string Key = "sk-test-secret-never-log";
    private const string Model = "~typesafe/jev-latest";

    [Test]
    public async Task Spam_UsesDecisionsEndpointAndLogsTheSpamProbabilityNotChoiceConfidence()
    {
        using var env = new TestEnvironment(Key);
        using var services = Services();
        var requests = new List<JsonDocument>();
        var auth = new List<string?>();
        var endpoints = new List<Uri?>();
        var methods = new List<HttpMethod>();
        var contentTypes = new List<string?>();
        using var http = new HttpClient(
            new DelegateHandler(async request =>
            {
                requests.Add(JsonDocument.Parse(await request.Content!.ReadAsStringAsync()));
                auth.Add(request.Headers.Authorization?.ToString());
                endpoints.Add(request.RequestUri);
                methods.Add(request.Method);
                contentTypes.Add(request.Content!.Headers.ContentType?.MediaType);
                return Reply(HttpStatusCode.OK, Answer("spam", "not_spam", 0.17, 0.83, 0.96));
            })
        );
        var logger = new RecordingLogger<JevChecks>();
        var checks = Create(http, services, logger);

        await checks.LogSpam("Rendered message and group context", "hash:message", -100123, 91);
        await checks.LogSpam("Rendered message and group context", "hash:message", -100124, 92);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(checks.Enabled, Is.True);
            Assert.That(requests, Has.Count.EqualTo(1), "the same content should reuse a successful decision");
            Assert.That(endpoints, Is.EqualTo(new[] { new Uri("https://openrouter.ai/api/alpha/decisions") }));
            Assert.That(methods, Is.EqualTo(new[] { HttpMethod.Post }));
            Assert.That(contentTypes, Is.EqualTo(new[] { "application/json" }));
            Assert.That(auth, Is.EqualTo(new[] { $"Bearer {Key}" }));
            Assert.That(
                http.DefaultRequestHeaders.Authorization,
                Is.Null,
                "authentication belongs to each request, not to the shared client"
            );
            Assert.That(requests[0].RootElement.GetProperty("model").GetString(), Is.EqualTo(Model));
            Assert.That(requests[0].RootElement.GetProperty("state").GetString(), Is.EqualTo("Rendered message and group context"));
            Assert.That(
                requests[0].RootElement.GetProperty("questions").EnumerateObject().Select(p => p.Name),
                Is.EqualTo(new[] { "spam" })
            );
            Assert.That(
                requests[0].RootElement.GetProperty("questions").GetProperty("spam").GetProperty("type").GetString(),
                Is.EqualTo("choice")
            );
            Assert.That(
                requests[0]
                    .RootElement.GetProperty("questions")
                    .GetProperty("spam")
                    .GetProperty("criteria")
                    .EnumerateObject()
                    .Select(p => p.Name),
                Is.EquivalentTo(new[] { "spam", "not_spam" })
            );
            Assert.That(logger.Entries, Has.Count.EqualTo(2));
            Assert.That(logger.Entries.All(entry => entry.Level == LogLevel.Information), Is.True);
            Assert.That(logger.Entries[0].Fields["PositiveProbability"], Is.EqualTo(0.17));
            Assert.That(logger.Entries[0].Fields["NegativeProbability"], Is.EqualTo(0.83));
            Assert.That(logger.Entries[0].Fields["Confidence"], Is.EqualTo(0.96));
            Assert.That(logger.Entries[0].Fields["Choice"], Is.EqualTo("not_spam"));
            Assert.That(logger.Entries[0].Fields["Model"], Is.EqualTo("typesafe/jev-1.13-20260917"));
            Assert.That(logger.Entries[1].Fields["ChatId"], Is.EqualTo(-100124L));
            Assert.That(logger.Entries[1].Fields["MessageId"], Is.EqualTo(92));
            Assert.That(logger.Entries[1].Fields["InputKey"], Is.EqualTo("hash:message"));
            Assert.That(logger.Entries.All(entry => !entry.Message.Contains(Key, StringComparison.Ordinal)), Is.True);
        }
        foreach (var request in requests)
            request.Dispose();
    }

    [Test]
    public async Task Profile_EroticOnlyAndFullClassificationUseIndependentCacheEntries()
    {
        using var env = new TestEnvironment(Key);
        using var services = Services();
        var requests = new List<JsonDocument>();
        using var http = new HttpClient(
            new DelegateHandler(async request =>
            {
                var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
                requests.Add(json);
                var names = json.RootElement.GetProperty("questions").EnumerateObject().Select(p => p.Name).ToArray();
                var answers = names.ToDictionary(
                    name => name,
                    name => new
                    {
                        type = "choice",
                        choice = "present",
                        confidence = 0.81,
                        probabilities = new Dictionary<string, double> { ["present"] = 0.69, ["absent"] = 0.31 },
                    }
                );
                return Reply(HttpStatusCode.OK, JsonSerializer.Serialize(new { model = "typesafe/jev-1.13-20260917", answers }));
            })
        );
        var logger = new RecordingLogger<JevChecks>();
        var checks = Create(http, services, logger);

        await checks.LogProfile("Rendered text-only profile", "hash:profile", true, -100123, 42);
        await checks.LogProfile("Rendered text-only profile", "hash:profile", false, -100123, 42);
        await checks.LogProfile("Rendered text-only profile", "hash:profile", true, -100123, 43);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(requests, Has.Count.EqualTo(2));
            Assert.That(QuestionNames(requests[0]), Is.EqualTo(new[] { "erotic" }));
            Assert.That(QuestionNames(requests[1]), Is.EquivalentTo(new[] { "erotic", "gambling", "nonperson", "selfpromotion" }));
            Assert.That(logger.Entries, Has.Count.EqualTo(6));
            Assert.That(
                logger.Entries.Select(entry => entry.Fields["Question"]),
                Is.EquivalentTo(new[] { "erotic", "erotic", "gambling", "nonperson", "selfpromotion", "erotic" })
            );
            Assert.That(logger.Entries.All(entry => Equals(entry.Fields["PositiveProbability"], 0.69)), Is.True);
            Assert.That(logger.Entries.All(entry => Equals(entry.Fields["Confidence"], 0.81)), Is.True);
            Assert.That(logger.Entries[^1].Fields["UserId"], Is.EqualTo(43L));
        }
        foreach (var request in requests)
            request.Dispose();
    }

    [TestCase(HttpStatusCode.ServiceUnavailable, "unavailable")]
    [TestCase(HttpStatusCode.OK, "{not-json")]
    public async Task UnavailableOrMalformedResponse_DoesNotPoisonTheCache(HttpStatusCode status, string body)
    {
        using var env = new TestEnvironment(Key);
        using var services = Services();
        var sent = 0;
        using var http = new HttpClient(
            new DelegateHandler(_ =>
            {
                sent++;
                return Task.FromResult(sent == 1 ? Reply(status, body) : Reply(HttpStatusCode.OK, Answer("spam", "spam", 0.94, 0.06, 0.6)));
            })
        );
        var logger = new RecordingLogger<JevChecks>();
        var checks = Create(http, services, logger);

        await checks.LogSpam("Suspicious message", "hash:retry", -100123, 91);
        await checks.LogSpam("Suspicious message", "hash:retry", -100123, 92);
        await checks.LogSpam("Suspicious message", "hash:retry", -100123, 93);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sent, Is.EqualTo(2), "the failed call must not be cached but the successful retry must be");
            Assert.That(logger.Entries, Has.Count.EqualTo(3));
            Assert.That(
                logger.Entries[0].Fields.GetValueOrDefault("StatusCode") is 503
                    || Equals(logger.Entries[0].Fields.GetValueOrDefault("FailureType"), "JsonException"),
                Is.True
            );
            Assert.That(logger.Entries.Skip(1).All(entry => entry.Fields.ContainsKey("PositiveProbability")), Is.True);
            Assert.That(
                logger.Entries.All(entry =>
                    !entry.Message.Contains(Key, StringComparison.Ordinal) && !entry.Message.Contains(body, StringComparison.Ordinal)
                ),
                Is.True
            );
        }
    }

    [Test]
    public async Task MissingKeyDisablesChecksWithoutSendingRequests()
    {
        using var env = new TestEnvironment("  ");
        using var services = Services();
        var sent = 0;
        using var http = new HttpClient(
            new DelegateHandler(_ =>
            {
                sent++;
                throw new InvalidOperationException("No request expected");
            })
        );
        var logger = new RecordingLogger<JevChecks>();
        var checks = Create(http, services, logger);

        await checks.LogSpam("message", "hash:message", -100123, 91);
        await checks.LogProfile("profile", "hash:profile", false, -100123, 42);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(checks.Enabled, Is.False);
            Assert.That(sent, Is.Zero);
            Assert.That(logger.Entries, Is.Empty);
        }
    }

    [Test]
    public async Task TimeoutIsLoggedAndDoesNotCacheAnAnswer()
    {
        using var env = new TestEnvironment(Key);
        using var services = Services();
        var sent = 0;
        using var http = new HttpClient(
            new DelegateHandler(_ =>
            {
                sent++;
                return Task.FromCanceled<HttpResponseMessage>(new CancellationToken(true));
            })
        );
        var logger = new RecordingLogger<JevChecks>();
        var checks = Create(http, services, logger);

        await checks.LogSpam("message", "hash:cancel", -100123, 91);
        await checks.LogSpam("message", "hash:cancel", -100123, 92);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sent, Is.EqualTo(2));
            Assert.That(logger.Entries, Has.Count.EqualTo(2));
            Assert.That(logger.Entries.All(entry => entry.Level == LogLevel.Information), Is.True);
            Assert.That(logger.Entries.Any(entry => entry.Fields.ContainsKey("PositiveProbability")), Is.False);
        }
    }

    [Test]
    public async Task CallerCancellationDoesNotLogOrSendARequest()
    {
        using var env = new TestEnvironment(Key);
        using var services = Services();
        var sent = 0;
        using var http = new HttpClient(
            new DelegateHandler(_ =>
            {
                sent++;
                return Task.FromResult(Reply(HttpStatusCode.OK, Answer("spam", "spam", 1, 0, 1)));
            })
        );
        var logger = new RecordingLogger<JevChecks>();
        var checks = Create(http, services, logger);

        await checks.LogSpam("message", "hash:cancelled", -100123, 91, new CancellationToken(true));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sent, Is.Zero);
            Assert.That(logger.Entries, Is.Empty);
        }
    }

    private static string[] QuestionNames(JsonDocument request) =>
        request.RootElement.GetProperty("questions").EnumerateObject().Select(p => p.Name).ToArray();

    private static string Answer(string question, string choice, double yes, double no, double confidence) =>
        JsonSerializer.Serialize(
            new
            {
                model = "typesafe/jev-1.13-20260917",
                answers = new Dictionary<string, object>
                {
                    [question] = new
                    {
                        type = "choice",
                        choice,
                        confidence,
                        probabilities = new Dictionary<string, double>
                        {
                            [question == "spam" ? "spam" : "present"] = yes,
                            [question == "spam" ? "not_spam" : "absent"] = no,
                        },
                    },
                },
            }
        );

    private static HttpResponseMessage Reply(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();
        return services.BuildServiceProvider();
    }

    private static JevChecks Create(HttpClient http, ServiceProvider services, ILogger<JevChecks> logger) =>
        new(
            http,
            new Config(services.GetRequiredService<HybridCache>(), NullLogger<Config>.Instance),
            logger,
            services.GetRequiredService<HybridCache>()
        );

    private sealed class DelegateHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            respond(request);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel level,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            var fields = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(pair => pair.Key, pair => pair.Value)
                : new Dictionary<string, object?>();
            Entries.Add(new LogEntry(level, formatter(state, exception), fields));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Dictionary<string, object?> Fields);

    private sealed class TestEnvironment : IDisposable
    {
        private readonly Dictionary<string, string?> _previous = new();

        public TestEnvironment(string? key)
        {
            foreach (
                var (name, value) in new Dictionary<string, string?>
                {
                    ["DOORMAN_BOT_API"] = "123456:TEST_TOKEN",
                    ["DOORMAN_ADMIN_CHAT"] = "-999",
                    ["DOORMAN_ADMIN_CHAT_MAP"] = null,
                    ["DOORMAN_OPENROUTER_API"] = key,
                }
            )
            {
                _previous[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach (var (name, value) in _previous)
                Environment.SetEnvironmentVariable(name, value);
        }
    }
}
