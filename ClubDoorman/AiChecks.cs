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
        UserManager userManager,
        ILogger<AiChecks> logger
    )
    {
        _bot = bot;
        _config = config;
        _hybridCache = hybridCache;
        _serviceScopeFactory = serviceScopeFactory;
        _userManager = userManager;

        _logger = logger;
        _api = _config.OpenRouterApi == null ? null : CustomProviders.OpenRouter(_config.OpenRouterApi);
    }

    private readonly ResiliencePipeline _retry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions() { Delay = TimeSpan.FromMilliseconds(50) })
        .Build();
    const string Model = "google/gemini-3-flash-preview";
    private readonly OpenAiClient? _api;
    private readonly JsonSerializerOptions jso = new() { Converters = { new JsonStringEnumConverter() } };
    private readonly ITelegramBotClient _bot;
    private readonly Config _config;
    private readonly HybridCache _hybridCache;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly UserManager _userManager;

    private readonly ILogger<AiChecks> _logger;

    private static string CacheKey(long userId) => $"attention:{userId}";

    private static string ChatInfoCacheKey(long chatId) => $"chat_info:{chatId}";

    private static string LinkedChannelInfoCacheKey(long channelId) => $"linked_channel_info:{channelId}";

    public async Task MarkUserOkay(long userId, CancellationToken ct = default)
    {
        await _hybridCache.SetAsync(
            CacheKey(userId),
            new SpamPhotoBio(new BioClassProbability(), [], ""),
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

    public ValueTask ClearCache(long userId) => _hybridCache.RemoveAsync(CacheKey(userId));

    private async ValueTask<SpamPhotoBio> GetEroticPhotoBaitProbability(
        Telegram.Bot.Types.User user,
        ChatFullInfo userChat,
        CancellationToken ct = default
    )
    {
        if (_api == null)
            return new SpamPhotoBio(new BioClassProbability(), [], "");
        var probability = new BioClassProbability();
        var pic = Array.Empty<byte>();

        try
        {
            var photo = userChat.Photo!;
            using var ms = new MemoryStream();
            await _bot.GetInfoAndDownloadFile(photo.BigFileId, ms, cancellationToken: ct);
            var photoBytes = ms.ToArray();
            pic = photoBytes;
            var photoMessage = CreateContextImageMessage(photoBytes);

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
                probability.EroticProbability = response.Value1.Probability;
                _logger.LogInformation("LLM GetEroticPhotoBaitProbability: {@Prob}", probability);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "GetEroticPhotoBaitProbability");
        }
        return new SpamPhotoBio(probability, pic, Utils.FullName(user));
    }

    public async ValueTask<SpamPhotoBio> GetAttentionBaitProbability(
        Telegram.Bot.Types.User user,
        Func<string, Task>? ifChanged = default,
        bool checkJustErotic = false
    )
    {
        if (_api == null)
            return new SpamPhotoBio(new BioClassProbability(), [], "");
        return await _hybridCache.GetOrCreateAsync(
            CacheKey(user.Id),
            async ct =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var halfApproved = await db
                    .HalfApprovedUsers.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == user.Id, cancellationToken: ct);
                if (halfApproved != default)
                    return new SpamPhotoBio(new BioClassProbability(), [], "");

                var probability = new BioClassProbability();
                var pic = Array.Empty<byte>();
                var nameBioUser = string.Empty;

                try
                {
                    var userChat = await _bot.GetChat(user.Id, cancellationToken: ct);
                    if (ifChanged != default)
                        _ = CheckLater(userChat, ifChanged, ct);

                    if (checkJustErotic || (userChat.Bio == null && userChat.LinkedChatId == null))
                    {
                        _logger.LogDebug("GetAttentionBaitProbability {User}: no bio, no channel", Utils.FullName(user));
                        if (userChat.Photo != null)
                            return await GetEroticPhotoBaitProbability(user, userChat, ct);
                        return new SpamPhotoBio(new BioClassProbability(), [], "");
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
                        photoMessage = CreateContextImageMessage(photoBytes);
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
                        "Проанализируй, выглядит ли этот Telegram-профиль как «продажный» и созданный с целью привлечения внимания. Отвечай вероятностью от 0 до 1.\n"
                        + "В EroticProbability ответь, с какой вероятностью этот профиль сексуализирован, обрати внимание на эмодзи с двойным смыслом (💦💋👄🍑🍆🍒🍓🍌 и прочих) в имени, любой намёк на эротику и порно, голые фото, OnlyFans\n"
                        + "В GamblingProbability ответь, с какой вероятностью профиль связан с предложениями рабогатеть - казино, гэмблинг, трейдинг, арбитаж, привлечению трафика, крипта\n"
                        + $"В NonPersonProbability ответь, с какой вероятностью профиль даже не притворяется человеком (нет имени и фотографии человека, но например животное или персонаж из мультфильма это ок), а сразу выглядит как бизнес-аккаунт или реклама\n"
                        + "В SelfPromotionProbability ответь, с какой вероятностью профиль направлен на само-продвижение, ОСОБЕННО если у него род деятельности или намёк на бизнес указан прямо в имени (например коучинг, HR, 'Алгоритм изобилия', 'Документы об образовании', 'Образование онлайн', 'Документы под ключ'), если у него предложение вступить в группу, подписываться, бесплатные продукты, документы, дипломы, сертификаты, и другие способы привлечения"
                        + $"\nВот данные профиля:\n{nameBioUser}";

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
                            messages.Add(CreateContextImageMessage(channelPhoto));
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
                                        messages.Add(CreateContextImageMessage(channelPhoto));
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
                            await _api.Chat.CreateChatCompletionAsAsync<BioClassProbability>(
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
                        if (
                            probability.EroticProbability < Consts.LlmLowProbability
                            && probability.NonPersonProbability < Consts.LlmLowProbability
                            && probability.SelfPromotionProbability < Consts.LlmLowProbability
                            && probability.GamblingProbability < Consts.LlmLowProbability
                        )
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

    internal record ChatDescription(string Description, long? ChannelId);

    private async ValueTask<ChatDescription?> GetChatInfoAsync(long chatId, CancellationToken ct = default)
    {
        return await _hybridCache.GetOrCreateAsync<ChatDescription?>(
            ChatInfoCacheKey(chatId),
            async ct =>
            {
                try
                {
                    var chat = await _bot.GetChat(chatId, cancellationToken: ct);
                    var info = new StringBuilder();
                    info.AppendLine($"Чат: {chat.Title}");
                    if (chat.Description != null)
                        info.AppendLine($"Описание чата: {chat.Description}");

                    return new(info.ToString(), chat.LinkedChatId);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to get chat info for {ChatId}", chatId);
                    return null;
                }
            },
            new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromHours(24) },
            cancellationToken: ct
        );
    }

    private async ValueTask<string> GetLinkedChannelInfoAsync(long channelId, CancellationToken ct = default)
    {
        return await _hybridCache.GetOrCreateAsync(
            LinkedChannelInfoCacheKey(channelId),
            async ct =>
            {
                try
                {
                    var linkedChat = await _bot.GetChat(channelId, cancellationToken: ct);
                    var info = new StringBuilder();
                    info.AppendLine($"Этот чат - чат обсуждения для канала: {linkedChat.Title}");
                    if (linkedChat.Description != null)
                        info.AppendLine($"Описание канала: {linkedChat.Description}");

                    return info.ToString();
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to get linked channel info for {ChannelId}", channelId);
                    return string.Empty;
                }
            },
            new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromHours(24) },
            cancellationToken: ct
        );
    }

    private async Task CheckLater(ChatFullInfo userChat, Func<string, Task> ifChanged, CancellationToken ct = default)
    {
        try
        {
            if (userChat.Type != Telegram.Bot.Types.Enums.ChatType.Private)
                _logger.LogError("Assert failed: unexpected chat type {Type}", userChat.Type);

            var wait = TimeSpan.Zero;
            for (var i = 1; i <= 3; i++)
            {
                wait += TimeSpan.FromMinutes(Math.Exp(i) / 2);
                await Task.Delay(wait, ct);
                if (await _userManager.InBanlist(userChat.Id))
                {
                    _ = ifChanged.Invoke("пользователь теперь в блеклисте спамеров");
                    return;
                }

                var chat = await _bot.GetChat(userChat.Id, cancellationToken: ct);
                if (chat.Photo?.BigFileUniqueId != userChat.Photo?.BigFileUniqueId)
                {
                    _ = ifChanged.Invoke("пользователь сменил фото");
                    return;
                }
                if (chat.Bio != userChat.Bio)
                {
                    _ = ifChanged.Invoke(
                        $"пользователь сменил био.{Environment.NewLine}новое: {chat.Bio}{Environment.NewLine}старое: {userChat.Bio}"
                    );
                    return;
                }
                if (chat.LinkedChatId != userChat.LinkedChatId)
                {
                    _ = ifChanged.Invoke("у пользователя сменился привязанный канал");
                    return;
                }
                if (chat.FirstName != userChat.FirstName)
                {
                    _ = ifChanged.Invoke($"пользователь сменил имя{Environment.NewLine}новое: {chat.FirstName}");
                    return;
                }
                if (chat.LastName != userChat.LastName)
                {
                    _ = ifChanged.Invoke($"пользователь сменил фамилию{Environment.NewLine}новая: {chat.LastName}");
                    return;
                }
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning(e, nameof(CheckLater));
        }
    }

    public async ValueTask<SpamProbability> GetSpamProbability(Message message, bool free = false)
    {
        var probability = new SpamProbability();
        if (_api == null)
            return probability;

        var text = message.Caption ?? message.Text ?? "";
        if (message.Poll?.Question != null)
            text =
                $"Опрос: {message.Poll.Question}{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", message.Poll.Options.Select(o => o.Text))}";
        if (message.Quote?.Text != null)
            text = $"> {message.Quote.Text}{Environment.NewLine}{text}";

        if (string.IsNullOrWhiteSpace(text) && message.Photo == null)
        {
            _logger.LogDebug("GetSpamProbability: No text or photo to analyze, returning 0");
            return new SpamProbability();
        }

        var modelToUse = free ? "openrouter/free" : Model;
        var selectedPhoto = message.Photo?.Any() == true ? SelectHighestQualityPhoto(message.Photo) : null;
        var cacheKey = $"llm_spam_prob:{modelToUse}:{ShaHelper.ComputeSha256Hex(text)}";
        if (string.IsNullOrWhiteSpace(text) && selectedPhoto != null)
            cacheKey = $"llm_spam_prob:{modelToUse}:{selectedPhoto.FileUniqueId}";

        return await _hybridCache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                try
                {
                    var contextBuilder = new StringBuilder();

                    var info = await GetChatInfoAsync(message.Chat.Id, ct);
                    if (info != null)
                    {
                        var (chatInfoText, linked) = info;
                        contextBuilder.AppendLine(chatInfoText);
                        try
                        {
                            if (linked.HasValue)
                            {
                                var linkedChannelInfo = await GetLinkedChannelInfoAsync(linked.Value, ct);
                                contextBuilder.AppendLine(linkedChannelInfo);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogWarning(e, "Failed to get linked channel for chat {ChatId}", message.Chat.Id);
                        }

                        var text = message.ReplyToMessage?.Text ?? message.ReplyToMessage?.Caption;
                        if (!string.IsNullOrEmpty(text))
                        {
                            contextBuilder.AppendLine("###");
                            if (message.ReplyToMessage?.IsAutomaticForward == true)
                                contextBuilder.AppendLine("Пост в канале, на который отвечают:");
                            else
                                contextBuilder.AppendLine("Сообщение, на которое отвечают:");

                            contextBuilder.AppendLine(text);
                        }
                    }

                    byte[]? imageBytes = null;
                    if (selectedPhoto != null)
                    {
                        _logger.LogDebug(
                            "GetSpamProbability selected message photo {Width}x{Height}, file size {FileSize}",
                            selectedPhoto.Width,
                            selectedPhoto.Height,
                            selectedPhoto.FileSize
                        );
                        using var ms = new MemoryStream();
                        await _bot.GetInfoAndDownloadFile(selectedPhoto.FileId, ms, cancellationToken: ct);
                        imageBytes = ms.ToArray();
                    }

                    var promt =
                        $"Проанализируй, выглядит ли это сообщение как спам или мошенничество, созданное с целью привлечения внимания и продвижения. Отвечай вероятностью от 0 до 1. Частые примеры: казино, гэмблинг, наркотики, эротика, порно, сексуализированные сообщения, схема заработка с обещаниями высокой прибыли, схема заработка без подробностей, неофициальное трудоустройство, срочный набор на работу, NFT, крипто, призыв перейти по ссылке, призыв писать в личные сообщения, услуги рассылки и продвижения, выпрашивание денег под жалобным предлогом, предложение поделиться ресурсами и книгами по трейдингу или инвестициям, промокоды, реклама, увеличение трафика или потока клиентов, подарочные сертификаты и другие цифровые промокоды со скидкой. Обрати внимание если язык на котором общаются в чате и язык сообщения не совпадают (например, в чате пишут по-русски, а в сообщении 'привет' по-арабски).";

                    var fullPrompt = new StringBuilder();
                    fullPrompt.AppendLine(promt);
                    fullPrompt.AppendLine("###");
                    fullPrompt.AppendLine("Контекст сообщения:");
                    fullPrompt.AppendLine(contextBuilder.ToString());
                    fullPrompt.AppendLine("###");
                    if (!string.IsNullOrWhiteSpace(text))
                        fullPrompt.AppendLine($"Само сообщение, которое нужно проанализировать:\n{text}");
                    else
                        fullPrompt.AppendLine("Само сообщение не содержит текста, только изображение.");

                    var fpString = fullPrompt.ToString();
                    const string systemMessage =
                        "Ты — модератор Telegram-группы, оценивающий сообщения в чате на спам, мошенничество и продвижения сторонних ресурсов или услуг";
                    _logger.LogInformation(
                        "GetSpamProbability full prompt - System: {System}, User: {User}, HasImage: {HasImage}, Model: {Model}",
                        systemMessage,
                        fpString,
                        message.Photo != null,
                        modelToUse
                    );

                    var messages = new List<ChatCompletionRequestMessage>
                    {
                        "Ты — модератор Telegram-группы, оценивающий сообщения в чате на спам, мошенничество и продвижения сторонних ресурсов или услуг".AsSystemMessage(),
                        fpString.AsUserMessage(),
                    };
                    if (imageBytes != null)
                        messages.Add(CreateSpamImageMessage(imageBytes));

                    var response = await _retry.ExecuteAsync(
                        async token =>
                            await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                                messages: messages,
                                model: modelToUse,
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
                        return probability;
                    }
                    else
                    {
                        _logger.LogWarning("LLM GetSpamProbability resp {@Resp}", response);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, nameof(GetSpamProbability));
                }
                Task.Run(
                        async () =>
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(100));
                            await _hybridCache.RemoveAsync(cacheKey);
                        },
                        ct
                    )
                    .FireAndForget(_logger, "remove cache key " + cacheKey);
                return probability;
            },
            new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromDays(1) }
        );
    }

    internal static ChatCompletionRequestUserMessage CreateContextImageMessage(byte[] imageBytes) =>
        imageBytes.AsUserMessage(mimeType: "image/jpg", detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low)!;

    internal static ChatCompletionRequestUserMessage CreateSpamImageMessage(byte[] imageBytes) =>
        imageBytes.AsUserMessage(mimeType: "image/jpg", detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.High)!;

    internal static PhotoSize SelectHighestQualityPhoto(IEnumerable<PhotoSize> photos) =>
        photos
            .OrderByDescending(x => x.Width * x.Height)
            .ThenByDescending(x => x.FileSize ?? 0)
            .ThenByDescending(x => x.Width)
            .ThenByDescending(x => x.Height)
            .First();

    internal class SpamProbability()
    {
        public double Probability { get; set; }
        public string Reason { get; set; } = "";
    }

    internal sealed class BioClassProbability()
    {
        public double EroticProbability { get; set; }
        public double GamblingProbability { get; set; }
        public double NonPersonProbability { get; set; }
        public double SelfPromotionProbability { get; set; }
        public string Reason { get; set; } = "";
    }

    internal sealed record SpamPhotoBio(BioClassProbability Probability, byte[] Photo, string NameBio);
}
