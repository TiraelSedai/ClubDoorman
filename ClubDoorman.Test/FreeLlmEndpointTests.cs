using System.Text.Json;
using System.Text.Json.Serialization;
using tryAGI.OpenAI;

namespace ClubDoorman.Test;

/// <summary>
/// Talks to the real free-chat endpoint from DOORMAN_FREE_LLM_*: a local llama.cpp or whatever else speaks OpenAI.
/// Ignored when it is not configured, so CI stays green without one.
/// </summary>
public class FreeLlmEndpointTests
{
    [Test]
    public async Task FreeEndpoint_ScoresObviousSpamHigh()
    {
        var settings = Config.FreeLlmSettings.FromEnv();
        if (settings == null)
            Assert.Ignore("DOORMAN_FREE_LLM_URL is not set");

        using var api = AiChecks.BuildFreeClient(settings!);
        var prompt = AiChecks.BuildSpamPrompt(
            "ИЩУ 3-4 человек в новую команду! Доход от 200$ в день, обучение бесплатно, пиши в лс +",
            "Чат: Про еду",
            null,
            null,
            false,
            null
        );

        var response = await api.Chat.CreateChatCompletionAsAsync<AiChecks.SpamProbability>(
            messages: [prompt.Text.AsUserMessage()],
            model: settings.Model,
            strict: true,
            jsonSerializerOptions: new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } }
        );

        Assert.That(response.Value1, Is.Not.Null, "endpoint returned no parsed verdict");
        TestContext.Out.WriteLine($"{response.Value1!.Probability}: {response.Value1.Reason}");
        Assert.That(response.Value1.Probability, Is.GreaterThan(0.5));
    }
}
