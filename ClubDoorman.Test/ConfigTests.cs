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
