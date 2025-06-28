using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Polly;
using Polly.Retry;
using Telegram.Bot;
using Telegram.Bot.Types;
using tryAGI.OpenAI;

namespace ClubDoorman;

internal class AiChecks
{
    public AiChecks(
        ITelegramBotClient bot,
        Config config,
        HybridCache hybridCache,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AiChecks> logger
    )
    {
        _bot = bot;
        _config = config;
        _hybridCache = hybridCache;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _api = _config.OpenRouterApi == null ? null : CustomProviders.OpenRouter(_config.OpenRouterApi);
    }

    private readonly ResiliencePipeline _retry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions() { Delay = TimeSpan.FromMilliseconds(50) })
        .Build();
    const string Model = "google/gemini-2.5-flash";
    private readonly OpenAiClient? _api;
    private readonly JsonSerializerOptions jso = new() { Converters = { new JsonStringEnumConverter() } };
    private readonly ITelegramBotClient _bot;
    private readonly Config _config;
    private readonly HybridCache _hybridCache;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AiChecks> _logger;

    private static string CacheKey(long userId) => $"attention:{userId}";

    public async Task MarkUserOkay(long userId, CancellationToken ct = default)
    {
        await _hybridCache.SetAsync(
            CacheKey(userId),
            new SpamPhotoBio(new SpamProbability(), [], ""),
            new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromDays(100) },
            cancellationToken: ct
        );
        using var scope = _serviceScopeFactory.CreateScope();
        await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var halfApproved = await db.HalfApprovedUsers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken: ct);
        if (halfApproved == default)
        {
            db.Add(new HalfApprovedUser { Id = userId });
            await db.SaveChangesAsync(ct);
        }
    }

    private async ValueTask<SpamPhotoBio> GetEroticPhotoBaitProbability(
        Telegram.Bot.Types.User user,
        ChatFullInfo userChat,
        CancellationToken ct = default
    )
    {
        if (_api == null)
            return new SpamPhotoBio(new SpamProbability(), [], "");
        var probability = new SpamProbability();
        var pic = Array.Empty<byte>();

        try
        {
            var photo = userChat.Photo!;
            using var ms = new MemoryStream();
            await _bot.GetInfoAndDownloadFile(photo.BigFileId, ms, cancellationToken: ct);
            var photoBytes = ms.ToArray();
            pic = photoBytes;
            var photoMessage = photoBytes.AsUserMessage(
                mimeType: "image/jpg",
                detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low
            );

            var prompt =
                "Проанализируй, выглядит ли эта аватарка пользователя сексуализированно или развратно. Отвечай вероятностью от 0 до 1.";
            var messages = new List<ChatCompletionRequestMessage> { prompt.AsUserMessage(), photoMessage };
            var response = await _retry.ExecuteAsync(
                async token =>
                    await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                        messages: messages,
                        model: Model,
                        strict: true,
                        jsonSerializerOptions: jso,
                        cancellationToken: token
                    ),
                ct
            );
            if (response.Value1 != null)
            {
                probability = response.Value1;
                _logger.LogInformation("LLM GetEroticPhotoBaitProbability: {@Prob}", probability);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "GetEroticPhotoBaitProbability");
        }
        return new SpamPhotoBio(probability, pic, Utils.FullName(user));
    }

    public ValueTask<SpamPhotoBio> GetAttentionBaitProbability(Telegram.Bot.Types.User user, Func<string, Task>? ifChanged = default)
    {
        if (_api == null)
            return ValueTask.FromResult(new SpamPhotoBio(new SpamProbability(), [], ""));
        return _hybridCache.GetOrCreateAsync(
            CacheKey(user.Id),
            async ct =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var halfApproved = await db
                    .HalfApprovedUsers.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == user.Id, cancellationToken: ct);
                if (halfApproved != default)
                    return new SpamPhotoBio(new SpamProbability(), [], "");

                var probability = new SpamProbability();
                var pic = Array.Empty<byte>();
                var nameBioUser = string.Empty;

                try
                {
                    var userChat = await _bot.GetChat(user.Id, cancellationToken: ct);
                    _ = CheckLater(userChat, ifChanged);

                    if (userChat.Bio == null && userChat.LinkedChatId == null)
                    {
                        _logger.LogDebug("GetAttentionBaitProbability {User}: no bio, no channel", Utils.FullName(user));
                        if (userChat.Photo != null)
                            return await GetEroticPhotoBaitProbability(user, userChat, ct);
                        return new SpamPhotoBio(new SpamProbability(), [], "");
                    }

                    _logger.LogDebug("GetAttentionBaitProbability {User} cache miss, asking LLM", Utils.FullName(user));
                    var photo = userChat.Photo;
                    byte[]? photoBytes = null;
                    ChatCompletionRequestUserMessage? photoMessage = null;

                    if (photo != null)
                    {
                        using var ms = new MemoryStream();
                        await _bot.GetInfoAndDownloadFile(photo.BigFileId, ms, cancellationToken: ct);
                        photoBytes = ms.ToArray();
                        pic = photoBytes;
                        photoMessage = photoBytes.AsUserMessage(
                            mimeType: "image/jpg",
                            detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low
                        );
                    }

                    var sb = new StringBuilder();
                    sb.Append($"Имя: {Utils.FullName(user)}");
                    if (user.Username != null)
                        sb.Append($"\nЮзернейм: @{user.Username}");
                    if (userChat.Bio != null)
                        sb.Append($"\nОписание: {userChat.Bio}");
                    if (photoBytes != null)
                        sb.Append($"\nФото: ");

                    nameBioUser = sb.ToString();
                    var promptDebugString = nameBioUser;
                    var prompt =
                        $"Проанализируй, выглядит ли этот Telegram-профиль как «продажный» и созданный с целью привлечения внимания. Отвечай вероятностью от 0 до 1. Особенно внимательно учитывай признаки:\nсексуализированные профили (эмодзи с двойным смыслом - 💦, 💋, 👄, 🍑, 🍆, 🍒, 🍓, 🍌 и прочих в имени, любой намёк на эротику и порно, голые фото), упоминания о курсах, заработке, трейдинге, арбитраже, привлечению трафика, ссылки на OnlyFans, соцсети. Обрати особенно внимание, если род занятий указан прямо в имени (например: HR, SMM, недвижимость, маркетинг). Вот данные профиля:\n{nameBioUser}";

                    var messages = new List<ChatCompletionRequestMessage>
                    {
                        "Ты — модератор Telegram-группы. Твоя задача — по данным профиля определить, направлен ли аккаунт на само-продвижение или привлечение к сторонним платным/эротическим ресурсам".AsSystemMessage(),
                        prompt.AsUserMessage(),
                    };
                    if (photoMessage != null)
                        messages.Add(photoMessage);

                    var linked = userChat.LinkedChatId;
                    if (linked != null)
                    {
                        byte[]? channelPhoto = null;
                        var linkedChat = await _bot.GetChat(linked, cancellationToken: ct);
                        var info = new StringBuilder();
                        info.Append($"Информация о привязанном канале:\nНазвание: {linkedChat.Title}");
                        if (linkedChat.Username != null)
                            sb.Append($"\nЮзернейм: @{linkedChat.Username}");
                        if (linkedChat.Description != null)
                            info.Append($"\nОписание: {linkedChat.Description}");
                        if (linkedChat.Photo != null)
                        {
                            info.Append($"\nФото:");
                            using var ms = new MemoryStream();
                            await _bot.GetInfoAndDownloadFile(linkedChat.Photo.BigFileId, ms, cancellationToken: ct);
                            channelPhoto = ms.ToArray();
                        }
                        var sbStr = info.ToString();
                        promptDebugString += "\n" + sbStr;
                        messages.Add(sbStr.AsUserMessage());
                        if (channelPhoto != null)
                            messages.Add(
                                channelPhoto.AsUserMessage(
                                    mimeType: "image/jpg",
                                    detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low
                                )
                            );
                    }

                    if (userChat.Bio != null)
                    {
                        var alreadyIncluded = new List<string>();
                        var matches = MyRegexes.TelegramUsername().Matches(userChat.Bio);
                        foreach (Match match in matches)
                        {
                            if (!match.Success)
                                continue;
                            var relevantGroups = match
                                .Groups.Cast<Group>()
                                .Skip(1) // 0th groups is full match
                                .Where(g => g.Success);

                            foreach (Group group in relevantGroups)
                            {
                                try
                                {
                                    var username = $"@{group.Value}";
                                    if (alreadyIncluded.Contains(username))
                                        continue;
                                    if (alreadyIncluded.Count >= 3)
                                        break;
                                    byte[]? channelPhoto = null;
                                    var mentionedChat = await _bot.GetChat(username, cancellationToken: ct);
                                    var info = new StringBuilder();
                                    info.Append($"Информация об упомянутом канале:\nНазвание: {mentionedChat.Title}");
                                    if (mentionedChat.Username != null)
                                        info.Append($"\nЮзернейм: @{mentionedChat.Username}");
                                    if (mentionedChat.Description != null)
                                        info.Append($"\nОписание: {mentionedChat.Description}");
                                    if (mentionedChat.Photo != null)
                                    {
                                        info.Append($"\nФото:");
                                        using var ms = new MemoryStream();
                                        await _bot.GetInfoAndDownloadFile(mentionedChat.Photo.BigFileId, ms, cancellationToken: ct);
                                        channelPhoto = ms.ToArray();
                                    }
                                    var sbStr = info.ToString();
                                    promptDebugString += "\n" + sbStr;
                                    messages.Add(sbStr.AsUserMessage());
                                    if (channelPhoto != null)
                                        messages.Add(
                                            channelPhoto.AsUserMessage(
                                                mimeType: "image/jpg",
                                                detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low
                                            )
                                        );
                                }
                                catch (Exception e)
                                {
                                    _logger.LogWarning(e, "Exception in matches");
                                }
                            }
                        }
                    }

                    _logger.LogDebug("LLM prompt: {Promt}", promptDebugString);

                    var response = await _retry.ExecuteAsync(
                        async token =>
                            await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                                messages: messages,
                                model: Model,
                                strict: true,
                                jsonSerializerOptions: jso,
                                cancellationToken: token
                            ),
                        ct
                    );
                    if (response.Value1 != null)
                    {
                        probability = response.Value1;
                        if (probability.Probability < Consts.LlmLowProbability)
                            pic = []; // cache optimization, don't store all user photos who are not spammers
                        _logger.LogInformation("LLM GetAttentionBaitProbability: {@Prob}", probability);
                    }
                    else
                    {
                        _logger.LogInformation("LLM GetAttentionBaitProbability: {@Resp}", response);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "GetAttentionBaitProbability");
                }
                return new SpamPhotoBio(probability, pic, nameBioUser);
            },
            new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromDays(7) }
        );
    }

    private async Task CheckLater(ChatFullInfo userChat, Func<string, Task>? ifChanged = default)
    {
        try
        {
            if (userChat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
                _logger.LogError("Assert failed: unexpected chat type {Type}", userChat.Type);

            var wait = TimeSpan.Zero;
            for (var i = 1; i <= 3; i++)
            {
                wait += TimeSpan.FromMinutes(5 * i);
                await Task.Delay(wait);
                var chat = await _bot.GetChat(userChat.Id);
                if (chat.Photo?.BigFileUniqueId != userChat.Photo?.BigFileUniqueId)
                {
                    _ = ifChanged?.Invoke("пользователь сменил фото");
                    return;
                }
                if (chat.Bio != userChat.Bio)
                {
                    _ = ifChanged?.Invoke("пользователь сменил био");
                    return;
                }
                if (chat.LinkedChatId != userChat.LinkedChatId)
                {
                    _ = ifChanged?.Invoke("у пользователя сменился привязанный канал");
                    return;
                }
                if (chat.FirstName != userChat.FirstName)
                {
                    _ = ifChanged?.Invoke("пользователь сменил имя");
                    return;
                }
                if (chat.LastName != userChat.LastName)
                {
                    _ = ifChanged?.Invoke("пользователь сменил фамилию");
                    return;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, nameof(CheckLater));
        }
    }

    public ValueTask<SpamProbability> GetSpamProbability(Message message)
    {
        var probability = new SpamProbability();
        if (_api == null)
            return ValueTask.FromResult(probability);

        var text = message.Caption ?? message.Text;
        var cacheKey = $"llm_spam_prob:{text}";

        return _hybridCache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                try
                {
                    byte[]? imageBytes = null;
                    if (message.Photo != null)
                    {
                        using var ms = new MemoryStream();
                        await _bot.GetInfoAndDownloadFile(message.Photo.OrderBy(x => x.Width).First().FileId, ms, cancellationToken: ct);
                        imageBytes = ms.ToArray();
                    }

                    var promt =
                        $"Проанализируй, выглядит ли это сообщение как спам или мошенничество, созданное с целью привлечения внимания и продвижения. Отвечай вероятностью от 0 до 1. Частые примеры: казино, гэмблинг, наркотики, эротика, порно, сексуализированные сообщения, схема заработка с обещаниями высокой прибыли, схема заработка без подробностей, неофициальное трудоустройство, срочный набор на работу, NFT, крипто, призыв перейти по ссылке, призыв писать в личные сообщения, услуги рассылки и продвижения, выпрашивание денег под жалобным предлогом, предложение поделиться ресурсами и книгами по трейдингу или инвестициям, промокоды, реклама, увеличение трафика или потока клиентов, подарочные сертификаты и другие цифровые промокоды со скидкой. Сообщение:\n";

                    var messages = new List<ChatCompletionRequestMessage>
                    {
                        "Ты — модератор Telegram-группы, оценивающий сообщения в чате на спам, мошенничество и продвижения сторонних ресурсов или услуг".AsSystemMessage(),
                        (promt + text).AsUserMessage(),
                    };
                    if (imageBytes != null)
                        messages.Add(
                            imageBytes.AsUserMessage(
                                mimeType: "image/jpg",
                                detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low
                            )
                        );

                    var response = await _retry.ExecuteAsync(
                        async token =>
                            await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                                messages: messages,
                                model: Model,
                                strict: true,
                                jsonSerializerOptions: jso,
                                cancellationToken: token
                            ),
                        ct
                    );
                    if (response.Value1 != null)
                    {
                        probability = response.Value1;
                        _logger.LogInformation("LLM GetSpamProbability {@Prob}", probability);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, nameof(GetSpamProbability));
                }
                return probability;
            },
            new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromDays(1) }
        );
    }

    internal class SpamProbability()
    {
        public double Probability { get; set; }
        public string Reason { get; set; } = "";
    }

    internal sealed record SpamPhotoBio(SpamProbability SpamProbability, byte[] Photo, string NameBio);
}
