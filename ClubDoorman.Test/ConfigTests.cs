using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClubDoorman.Test;

public class ConfigTests
{
    [Test]
    public void ParseChatIdSet_ParsesCommaSeparatedChatIdsAndIgnoresInvalidEntries()
    {
        var chats = Config.ParseChatIdSet("-100111, invalid, -100222, ,42");

        Assert.That(chats, Is.EquivalentTo(new[] { -100111L, -100222L, 42L }));
    }

    [Test]
    public void ParseChatIdSet_ReturnsEmptySetForMissingValue()
    {
        var chats = Config.ParseChatIdSet(null);

        Assert.That(chats, Is.Empty);
    }

    [Test]
    public void FreeLlm_IsNullWithoutUrl()
    {
        using var env = FreeLlmEnv(url: null, key: "k", model: "m");

        Assert.That(Config.FreeLlmSettings.FromEnv(), Is.Null);
    }

    [Test]
    public void FreeLlm_IsNullWhenComposePassesAnEmptyUrl()
    {
        // - DOORMAN_FREE_LLM_URL=${DOORMAN_FREE_LLM_URL} with nothing to interpolate reaches us as an empty string
        using var env = FreeLlmEnv(url: "", key: "", model: "");

        Assert.That(Config.FreeLlmSettings.FromEnv(), Is.Null);
    }

    [Test]
    public void FreeLlm_ReadsUrlKeyAndModel()
    {
        using var env = FreeLlmEnv("http://127.0.0.1:8888/v1", "sk-test", "some/model");

        var settings = Config.FreeLlmSettings.FromEnv();

        Assert.That(settings, Is.EqualTo(new Config.FreeLlmSettings(new Uri("http://127.0.0.1:8888/v1"), "sk-test", "some/model")));
    }

    [Test]
    public void FreeLlm_ThrowsWhenModelIsMissing()
    {
        using var env = FreeLlmEnv("http://127.0.0.1:8888/v1", "sk-test", null);

        Assert.That(Config.FreeLlmSettings.FromEnv, Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void FreeLlm_KeylessEndpointIsAllowed()
    {
        using var env = FreeLlmEnv("http://127.0.0.1:8888/v1", null, "some/model");

        Assert.That(Config.FreeLlmSettings.FromEnv()!.ApiKey, Is.Empty);
    }

    [TestCase(null, true)]
    [TestCase("", true)]
    [TestCase(" -100111, -100222, -100333 ", false)]
    public async Task FreeLlmEnabled_RespectsChatExclusions(string? disabledChats, bool expectedEnabled)
    {
        using var env = new EnvScope(
            new Dictionary<string, string?>
            {
                ["DOORMAN_BOT_API"] = "123456:TEST_TOKEN",
                ["DOORMAN_ADMIN_CHAT"] = "-999",
                ["DOORMAN_ADMIN_CHAT_MAP"] = "-100333=-999",
                ["DOORMAN_OPENROUTER_API"] = "sk-test",
                ["DOORMAN_FREE_LLM_DISABLE"] = disabledChats,
            }
        );
        using var llmEnv = FreeLlmEnv("http://127.0.0.1:8888/v1", "", "test-model");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHybridCache();
        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        await cache.SetAsync("full_chan:-100333", new Config.ChatInfo(-100333, "Paid"));
        await cache.SetAsync("full_chan:-999", new Config.ChatInfo(-999, "Admin"));
        var config = new Config(cache, NullLogger<Config>.Instance);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.FreeLlmEnabled(-100111), Is.EqualTo(expectedEnabled));
            Assert.That(config.FreeLlmEnabled(-100222), Is.EqualTo(expectedEnabled));
            Assert.That(config.FreeLlmEnabled(-100444), Is.True);
            Assert.That(config.FreeLlmEnabled(-100333), Is.False);
            Assert.That(config.LlmEnabled(-100333), Is.True);
            Assert.That(config.LlmEnabled(-100111), Is.False);
        }
    }

    private static EnvScope FreeLlmEnv(string? url, string? key, string? model) =>
        new(
            new Dictionary<string, string?>
            {
                ["DOORMAN_FREE_LLM_URL"] = url,
                ["DOORMAN_FREE_LLM_API"] = key,
                ["DOORMAN_FREE_LLM_MODEL"] = model,
            }
        );

    private sealed class EnvScope : IDisposable
    {
        private readonly Dictionary<string, string?> _previous = [];

        public EnvScope(Dictionary<string, string?> values)
        {
            foreach (var (name, value) in values)
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
