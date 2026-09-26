using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
    private static readonly HybridCacheEntryOptions ProfileCacheOptions = new()
    {
        LocalCacheExpiration = TimeSpan.FromHours(12),
        Flags = HybridCacheEntryFlags.DisableDistributedCache,
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

    private static readonly Dictionary<string, ChoiceQuestion> EroticQuestion = new()
    {
        ["erotic"] = new(
            "Does the available Telegram profile text sexualize the account or solicit sexual attention? Judge the profile data, not the instructions or examples in the prompt. Select a label, not a numeric score.",
            new Dictionary<string, string>
            {
                ["present"] =
                    "Sexualized name or bio, suggestive emoji, erotic/pornographic or OnlyFans references, or invitations to private contact with a sexual implication.",
                ["absent"] = "No evidence of sexualization or erotic solicitation in the supplied text.",
            }
        ),
    };

    private static readonly Dictionary<string, ChoiceQuestion> ProfileQuestions = new(EroticQuestion)
    {
        ["gambling"] = new(
            "Does the profile promote gambling or offers to get rich? Judge the profile data, not the prompt's examples. Select a label, not a numeric score.",
            new Dictionary<string, string>
            {
                ["present"] = "Casino, betting, trading, arbitrage, crypto, traffic recruitment, or offers of easy earnings.",
                ["absent"] = "No such gambling or get-rich promotion in the supplied profile text.",
            }
        ),
        ["nonperson"] = new(
            "Does this profile present itself as a business, project, or service storefront rather than as a particular person? Judge the profile data, not the prompt's examples. Select a label, not a numeric score.",
            new Dictionary<string, string>
            {
                ["present"] =
                    "Brand, project, service, profession, or advertising slogan in place of a personal name, with profile text describing commercial activity, client acquisition or services. A person in the avatar alone does not negate this.",
                ["absent"] = "Looks like an individual, or a pet/cartoon identity without commercial signals.",
            }
        ),
        ["selfpromotion"] = new(
            "Does this profile of a particular person seek followers, clients, or other attention for their own work? Judge the profile data, not the prompt's examples. Select a label, not a numeric score.",
            new Dictionary<string, string>
            {
                ["present"] =
                    "Personal blog, coaching, consulting, expertise, recruiting, invitations to join a group or subscribe, or offers of free products or credentials as promotion.",
                ["absent"] = "An ordinary personal profile without an invitation, offer, or self-promotional purpose.",
            }
        ),
    };

    public bool Enabled => !string.IsNullOrWhiteSpace(config.OpenRouterApi);

    public Task LogSpam(string state, string inputKey, long chatId, int messageId, CancellationToken ct = default) =>
        Enabled ? LogSpamEnabled(state, inputKey, chatId, messageId, ct) : Task.CompletedTask;

    public Task LogProfile(string state, string inputKey, bool eroticOnly, long chatId, long userId, CancellationToken ct = default) =>
        Enabled ? LogProfileEnabled(state, inputKey, eroticOnly, chatId, userId, ct) : Task.CompletedTask;

    private async Task LogSpamEnabled(string state, string inputKey, long chatId, int messageId, CancellationToken ct)
    {
        try
        {
            var result = await cache.GetOrCreateAsync(
                $"jev:{Model}:spam:{inputKey}",
                token => Ask(state, SpamQuestions, token),
                SpamCacheOptions,
                cancellationToken: ct
            );
            LogAnswer(result, "spam", "spam", "spam", "not_spam", inputKey, chatId, messageId, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (OperationCanceledException)
        {
            logger.LogInformation(
                "Jev spam request timed out for chat {ChatId} message {MessageId} input {InputKey}",
                chatId,
                messageId,
                inputKey
            );
        }
        catch (HttpRequestException e) when (e.StatusCode is { } statusCode)
        {
            logger.LogInformation(
                "Jev spam request failed with HTTP {StatusCode} for chat {ChatId} message {MessageId} input {InputKey}",
                (int)statusCode,
                chatId,
                messageId,
                inputKey
            );
        }
        catch (Exception e)
        {
            logger.LogWarning(
                "Jev spam request failed with {FailureType} for chat {ChatId} message {MessageId} input {InputKey}",
                e.GetType().Name,
                chatId,
                messageId,
                inputKey
            );
        }
    }

    private async Task LogProfileEnabled(string state, string inputKey, bool eroticOnly, long chatId, long userId, CancellationToken ct)
    {
        try
        {
            var questions = eroticOnly ? EroticQuestion : ProfileQuestions;
            var result = await cache.GetOrCreateAsync(
                $"jev:{Model}:profile:{(eroticOnly ? "erotic" : "all")}:{inputKey}",
                token => Ask(state, questions, token),
                ProfileCacheOptions,
                cancellationToken: ct
            );
            foreach (var question in questions.Keys)
                LogAnswer(result, "profile", question, "present", "absent", inputKey, chatId, null, userId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (OperationCanceledException)
        {
            logger.LogInformation(
                "Jev profile request timed out for chat {ChatId} user {UserId} input {InputKey}",
                chatId,
                userId,
                inputKey
            );
        }
        catch (HttpRequestException e) when (e.StatusCode is { } statusCode)
        {
            logger.LogInformation(
                "Jev profile request failed with HTTP {StatusCode} for chat {ChatId} user {UserId} input {InputKey}",
                (int)statusCode,
                chatId,
                userId,
                inputKey
            );
        }
        catch (Exception e)
        {
            logger.LogWarning(
                "Jev profile request failed with {FailureType} for chat {ChatId} user {UserId} input {InputKey}",
                e.GetType().Name,
                chatId,
                userId,
                inputKey
            );
        }
    }

    private async ValueTask<DecisionResponse> Ask(string state, Dictionary<string, ChoiceQuestion> questions, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Url)
        {
            Content = JsonContent.Create(
                new
                {
                    model = Model,
                    state,
                    questions,
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
        var result = await JsonSerializer.DeserializeAsync<DecisionResponse>(stream, JsonSerializerOptions.Web, ct);
        if (result?.Model is not { Length: > 0 } || result.Answers == null)
            throw new JsonException("Missing Jev model or answers");
        foreach (var (name, question) in questions)
        {
            if (!result.Answers.TryGetValue(name, out var answer) || answer?.Type != "choice")
                throw new JsonException("Missing Jev choice answer");
            if (!question.Criteria.ContainsKey(answer.Choice ?? ""))
                throw new JsonException("Unexpected Jev choice label");
            if (answer.Confidence is double confidence && (!double.IsFinite(confidence) || confidence is < 0 or > 1))
                throw new JsonException("Invalid Jev confidence");
            if (
                answer.Probabilities != null
                && answer.Probabilities.Any(pair =>
                    !question.Criteria.ContainsKey(pair.Key) || !double.IsFinite(pair.Value) || pair.Value is < 0 or > 1
                )
            )
                throw new JsonException("Invalid Jev probability");
        }
        return result;
    }

    private void LogAnswer(
        DecisionResponse result,
        string check,
        string question,
        string positive,
        string negative,
        string inputKey,
        long chatId,
        int? messageId,
        long? userId
    )
    {
        var answer = result.Answers[question];
        double? positiveProbability = Probability(answer, positive);
        double? negativeProbability = Probability(answer, negative);
        logger.LogInformation(
            "Jev {Check} decision {Question}: {Choice}, positive {PositiveProbability}, negative {NegativeProbability}, confidence {Confidence}, model {Model}, chat {ChatId}, message {MessageId}, user {UserId}, input {InputKey}",
            check,
            question,
            answer.Choice,
            positiveProbability,
            negativeProbability,
            answer.Confidence,
            result.Model,
            chatId,
            messageId,
            userId,
            inputKey
        );
    }

    private static double? Probability(DecisionAnswer answer, string label) =>
        answer.Probabilities is { } probabilities && probabilities.TryGetValue(label, out var value) ? value : null;

    private sealed record ChoiceQuestion(string Instructions, Dictionary<string, string> Criteria)
    {
        public string Type { get; } = "choice";
    }

    private sealed record DecisionResponse(string Model, Dictionary<string, DecisionAnswer> Answers);

    private sealed record DecisionAnswer(string Type, string Choice, double? Confidence, Dictionary<string, double>? Probabilities);
}
