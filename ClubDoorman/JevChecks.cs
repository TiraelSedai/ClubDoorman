using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Hybrid;

namespace ClubDoorman;

internal sealed class JevChecks(HttpClient http, Config config, ILogger<JevChecks> logger, HybridCache cache)
{
    private const string Model = "~typesafe/jev-latest";
    private const string Url = "https://openrouter.ai/api/alpha/decisions";
    private static readonly HybridCacheEntryOptions SpamCacheOptions = new()
    {
        LocalCacheExpiration = TimeSpan.FromDays(1),
        Flags = HybridCacheEntryFlags.DisableDistributedCache,
    };
    private static readonly JsonSerializerOptions ResponseJsonOptions = new(JsonSerializerOptions.Web)
    {
        NumberHandling = JsonNumberHandling.Strict,
    };

    private static readonly Dictionary<string, ChoiceQuestion> SpamQuestions = new()
    {
        ["spam"] = new(
            "Does the message itself advertise, solicit, scam, or promote an outside resource or service in a Telegram group? Judge the message in context, not the surrounding prompt's examples. The prompt may ask for a numeric probability; instead select one of these labels.",
            new Dictionary<string, string>
            {
                ["spam"] =
                    "Unsolicited advertisement, gambling, drugs, sexual solicitation, get-rich-quick schemes, suspicious jobs, crypto/NFT promotions, off-platform links or DMs for an offer, fundraising pretexts, promotional codes, or traffic/client acquisition.",
                ["not_spam"] =
                    "Ordinary conversation, a legitimate on-topic answer, or a quoted/mentioned offer that the sender is not promoting.",
            }
        ),
    };

    public bool Enabled => !string.IsNullOrWhiteSpace(config.OpenRouterApi);

    public async Task<SpamVerdict?> GetSpamProbability(string state, string inputKey, CancellationToken ct = default)
    {
        if (!Enabled)
        {
            logger.LogInformation("Jev spam check unavailable: OpenRouter API key is not configured");
            return null;
        }

        try
        {
            return await cache.GetOrCreateAsync(
                $"jev:{Model}:spam:{inputKey}",
                token => Ask(state, token),
                SpamCacheOptions,
                cancellationToken: ct
            );
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException e)
        {
            logger.LogInformation(e, "Jev spam request timed out for input {InputKey}", inputKey);
        }
        catch (HttpRequestException e)
        {
            logger.LogInformation(e, "Jev spam request failed for input {InputKey}", inputKey);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Jev spam request failed for input {InputKey}", inputKey);
        }
        return null;
    }

    private async ValueTask<SpamVerdict> Ask(string state, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = JsonContent.Create(
                new
                {
                    model = Model,
                    state,
                    questions = SpamQuestions,
                },
                options: JsonSerializerOptions.Web
            ),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.OpenRouterApi);
        // Send a fixed-length JSON body rather than a chunked request.
        await request.Content.LoadIntoBufferAsync(ct);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var result = await JsonSerializer.DeserializeAsync<DecisionResponse>(stream, ResponseJsonOptions, ct);
        if (
            string.IsNullOrWhiteSpace(result?.Model)
            || result.Answers == null
            || !result.Answers.TryGetValue("spam", out var answer)
            || answer?.Type != "choice"
            || answer.Choice is not ("spam" or "not_spam")
            || answer.Confidence is not double confidence
            || !double.IsFinite(confidence)
            || confidence is < 0 or > 1
            || (
                answer.Probabilities != null
                && answer.Probabilities.Any(pair =>
                    pair.Key is not ("spam" or "not_spam") || !double.IsFinite(pair.Value) || pair.Value is < 0 or > 1
                )
            )
        )
            throw new JsonException("Invalid Jev spam answer or confidence");
        return new SpamVerdict(answer.Choice == "spam", confidence);
    }

    internal sealed record SpamVerdict(bool IsSpam, double Confidence);

    private sealed record ChoiceQuestion(string Instructions, Dictionary<string, string> Criteria)
    {
        public string Type { get; } = "choice";
    }

    private sealed record DecisionResponse(string Model, Dictionary<string, DecisionAnswer> Answers);

    private sealed record DecisionAnswer(string Type, string Choice, double? Confidence, Dictionary<string, double>? Probabilities);
}
