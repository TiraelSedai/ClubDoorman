using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClubDoorman.Test;

[NonParallelizable]
public sealed class JevChecksTests
{
    [TestCase("spam", true)]
    [TestCase("not_spam", false)]
    public async Task ReturnsDecisionWithConfidence(string choice, bool isSpam)
    {
        using var fixture = new Fixture();
        fixture.RespondWith(Answer(choice, "0.91"));

        var verdict = await fixture.Checks.GetSpamProbability("Message in context", "message-key");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(verdict, Is.EqualTo(new JevChecks.SpamVerdict(isSpam, 0.91)));
            Assert.That(fixture.Requests, Has.Count.EqualTo(1));
        }
        var request = fixture.Requests.Single();
        using var body = JsonDocument.Parse(request.Body);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Uri.AbsoluteUri, Is.EqualTo("https://openrouter.ai/api/alpha/decisions"));
            Assert.That(request.Authorization, Is.EqualTo("Bearer sk-test"));
            Assert.That(request.ContentLength, Is.EqualTo(Encoding.UTF8.GetByteCount(request.Body)));
            Assert.That(body.RootElement.GetProperty("model").GetString(), Is.EqualTo("~typesafe/jev-latest"));
            var question = body.RootElement.GetProperty("questions").GetProperty("spam");
            Assert.That(question.GetProperty("type").GetString(), Is.EqualTo("choice"));
            Assert.That(question.GetProperty("criteria").TryGetProperty("spam", out _), Is.True);
            Assert.That(question.GetProperty("criteria").TryGetProperty("not_spam", out _), Is.True);
        }
    }

    [TestCase("""{"model":"~typesafe/jev-latest","answers":{"spam":{"type":"choice","choice":"spam"}}}""")]
    [TestCase("""{"model":"~typesafe/jev-latest","answers":{"spam":{"type":"choice","choice":"not_spam","confidence":null}}}""")]
    [TestCase("""{"model":"~typesafe/jev-latest","answers":{"spam":{"type":"choice","choice":"spam","confidence":-0.01}}}""")]
    [TestCase("""{"model":"~typesafe/jev-latest","answers":{"spam":{"type":"choice","choice":"spam","confidence":1.01}}}""")]
    [TestCase("""{"model":"~typesafe/jev-latest","answers":{"spam":{"type":"choice","choice":"spam","confidence":1e999}}}""")]
    [TestCase("""{"model":"~typesafe/jev-latest","answers":{"spam":{"type":"choice","choice":"spam","confidence":"0.9"}}}""")]
    [TestCase("""{"model":"~typesafe/jev-latest","answers":{"spam":{"type":"choice","choice":"unsure","confidence":0.9}}}""")]
    [TestCase("""{"model":"~typesafe/jev-latest","answers":{"spam":{"type":"text","choice":"not_spam","confidence":0.9}}}""")]
    [TestCase(
        """{"model":"~typesafe/jev-latest","answers":{"spam":{"type":"choice","choice":"not_spam","confidence":0.9,"probabilities":{"spam":1.1}}}}"""
    )]
    [TestCase(
        """{"model":"~typesafe/jev-latest","answers":{"spam":{"type":"choice","choice":"not_spam","confidence":0.9,"probabilities":{"unknown":0.9}}}}"""
    )]
    [TestCase("""{"model":"~typesafe/jev-latest","answers":{}}""")]
    [TestCase("""{"answers":{"spam":{"type":"choice","choice":"spam","confidence":0.9}}}""")]
    [TestCase("not json")]
    public async Task InvalidDecisionIsUnavailableNotHam(string response)
    {
        using var fixture = new Fixture();
        fixture.RespondWith(response);
        fixture.RespondWith(Answer("spam", "0.95"));

        var first = await fixture.Checks.GetSpamProbability("Message", "message-key");
        var second = await fixture.Checks.GetSpamProbability("Message", "message-key");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.Null);
            Assert.That(second, Is.EqualTo(new JevChecks.SpamVerdict(true, 0.95)), "invalid responses must not be cached");
            Assert.That(fixture.Requests, Has.Count.EqualTo(2));
        }
    }

    [TestCase("0")]
    [TestCase("1")]
    public async Task ConfidenceBoundariesAreValid(string confidence)
    {
        using var fixture = new Fixture();
        fixture.RespondWith(Answer("not_spam", confidence));

        var verdict = await fixture.Checks.GetSpamProbability("Message", "message-key");

        Assert.That(verdict, Is.EqualTo(new JevChecks.SpamVerdict(false, double.Parse(confidence))));
    }

    [Test]
    public async Task FailedEndpointIsUnavailableAndNotCached()
    {
        using var fixture = new Fixture();
        fixture.RespondWith("", HttpStatusCode.InternalServerError);
        fixture.RespondWith(Answer("not_spam", "0.87"));

        var first = await fixture.Checks.GetSpamProbability("Message", "message-key");
        var second = await fixture.Checks.GetSpamProbability("Message", "message-key");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.Null);
            Assert.That(second, Is.EqualTo(new JevChecks.SpamVerdict(false, 0.87)));
            Assert.That(fixture.Requests, Has.Count.EqualTo(2));
        }
    }

    [TestCase(null)]
    [TestCase("  ")]
    public async Task MissingApiKeyDisablesDecisions(string? apiKey)
    {
        using var fixture = new Fixture(apiKey);

        var verdict = await fixture.Checks.GetSpamProbability("Message", "message-key");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Checks.Enabled, Is.False);
            Assert.That(verdict, Is.Null);
            Assert.That(fixture.Requests, Is.Empty);
        }
    }

    [Test]
    public async Task CachesOnlySameContextInput()
    {
        using var fixture = new Fixture();
        fixture.RespondWith(Answer("spam", "0.9"));
        fixture.RespondWith(Answer("not_spam", "0.95"));

        var first = await fixture.Checks.GetSpamProbability("Message plus first chat context", "first-key");
        var repeated = await fixture.Checks.GetSpamProbability("Message plus first chat context", "first-key");
        var otherContext = await fixture.Checks.GetSpamProbability("Message plus second chat context", "second-key");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(new JevChecks.SpamVerdict(true, 0.9)));
            Assert.That(repeated, Is.EqualTo(first));
            Assert.That(otherContext, Is.EqualTo(new JevChecks.SpamVerdict(false, 0.95)));
            Assert.That(fixture.Requests, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public void CallerCancellationPropagatesWithoutRequest()
    {
        using var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(
            async () => await fixture.Checks.GetSpamProbability("Message", "message-key", cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>()
        );
        Assert.That(fixture.Requests, Is.Empty);
    }

    private static string Answer(string choice, string confidence) =>
        JsonSerializer.Serialize(
            new
            {
                model = "~typesafe/jev-latest",
                answers = new
                {
                    spam = new
                    {
                        type = "choice",
                        choice,
                        confidence = double.Parse(confidence, System.Globalization.CultureInfo.InvariantCulture),
                    },
                },
            }
        );

    private sealed class Fixture : IDisposable
    {
        private readonly Dictionary<string, string?> _previousEnvironment = [];
        private readonly ServiceProvider _services;
        private readonly HttpClient _http;
        private readonly Queue<HttpResponseMessage> _responses = new();
        public List<(HttpMethod Method, Uri Uri, string? Authorization, long? ContentLength, string Body)> Requests { get; } = [];
        public JevChecks Checks { get; }

        public Fixture(string? apiKey = "sk-test")
        {
            foreach (
                var (name, value) in new Dictionary<string, string?>
                {
                    ["DOORMAN_BOT_API"] = "123456:TEST_TOKEN",
                    ["DOORMAN_ADMIN_CHAT"] = "-1001111111111",
                    ["DOORMAN_ADMIN_CHAT_MAP"] = null,
                    ["DOORMAN_OPENROUTER_API"] = apiKey,
                }
            )
            {
                _previousEnvironment[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHybridCache();
            _services = services.BuildServiceProvider();
            _http = new HttpClient(new Handler(this));
            var config = new Config(_services.GetRequiredService<HybridCache>(), NullLogger<Config>.Instance);
            Checks = new JevChecks(_http, config, NullLogger<JevChecks>.Instance, _services.GetRequiredService<HybridCache>());
        }

        public void RespondWith(string body, HttpStatusCode status = HttpStatusCode.OK) =>
            _responses.Enqueue(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

        public void Dispose()
        {
            while (_responses.TryDequeue(out var response))
                response.Dispose();
            _http.Dispose();
            _services.Dispose();
            foreach (var (name, value) in _previousEnvironment)
                Environment.SetEnvironmentVariable(name, value);
        }

        private sealed class Handler(Fixture fixture) : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                fixture.Requests.Add(
                    (
                        request.Method,
                        request.RequestUri!,
                        request.Headers.Authorization?.ToString(),
                        request.Content.Headers.ContentLength,
                        body
                    )
                );
                return fixture._responses.Dequeue();
            }
        }
    }
}
