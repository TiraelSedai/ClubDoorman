using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Hybrid;
using Polly;
using Polly.Retry;
using Telegram.Bot;
using Telegram.Bot.Types;
using tryAGI.OpenAI;

namespace ClubDoorman;

internal class AiChecks
{
    public AiChecks(ITelegramBotClient bot, Config config, HybridCache hybridCache, UserManager userManager, ILogger<AiChecks> logger)
    {
        _bot = bot;
        _config = config;
        _hybridCache = hybridCache;
        _userManager = userManager;

        _logger = logger;
        _paid = _config.OpenRouterApi == null ? null : new(CustomProviders.OpenRouter(_config.OpenRouterApi), PaidModel, PaidRetry);
        var free = _config.FreeLlm;
        // a local model answers in minutes, not seconds, and a free chat is never in a hurry: wait long, never retry
        _free =
            free == null
                ? null
                : new(
                    new OpenAiClient(free.ApiKey, new HttpClient { Timeout = FreeLlmTimeout }, baseUri: free.BaseUrl),
                    free.Model,
                    ResiliencePipeline.Empty
                );
    }

    private static readonly ResiliencePipeline PaidRetry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions() { Delay = TimeSpan.FromMilliseconds(50) })
        .Build();
    private static readonly TimeSpan FreeLlmTimeout = TimeSpan.FromMinutes(10);
    const string PaidModel = "google/gemini-3.5-flash-lite";
    private readonly LlmEndpoint? _paid;
    private readonly LlmEndpoint? _free;
    private readonly JsonSerializerOptions jso = new() { Converters = { new JsonStringEnumConverter() } };
    private readonly ITelegramBotClient _bot;
    private readonly Config _config;
    private readonly HybridCache _hybridCache;
    private readonly UserManager _userManager;

    private readonly ILogger<AiChecks> _logger;

    /// <summary>Free chats go to their own endpoint, if one is configured; everyone else goes to the paid one.</summary>
    private LlmEndpoint? EndpointFor(long chatId) => _config.NonFreeChat(chatId) ? _paid : _free;

    private const string ProfileSystemMessage =
        "Ты — модератор Telegram-группы. Твоя задача — по данным профиля определить, направлен ли аккаунт на само-продвижение или привлечение к сторонним платным/эротическим ресурсам";

    private const string ProfilePromptHeader =
        "Проанализируй, выглядит ли этот Telegram-профиль как «продажный» и созданный с целью привлечения внимания. Отвечай вероятностью от 0 до 1.\n"
        + "В EroticProbability ответь, с какой вероятностью этот профиль сексуализирован, обрати внимание на эмодзи с двойным смыслом (💦💋👄🍑🍆🍒🍓🍌 и прочих) в имени, любой намёк на эротику и порно, голые фото, OnlyFans\n"
        + "В GamblingProbability ответь, с какой вероятностью профиль связан с предложениями рабогатеть - казино, гэмблинг, трейдинг, арбитаж, привлечению трафика, крипта\n"
        + "В NonPersonProbability ответь, с какой вероятностью профиль даже не притворяется конкретным человеком, а выглядит как витрина бренда, проекта, услуги или рекламы. Ставь высокую вероятность, если в имени вместо обычного человеческого имени/фамилии указаны брендовый ник, название проекта, услуга, род деятельности или рекламный слоган, а описание подтверждает коммерческую деятельность, услуги, продвижение, удаление негатива, оформление документов, продажи, трафик или привлечение клиентов. Фото или силуэт человека не должны сами по себе снижать вероятность, если имя и описание всё равно выглядят как витрина услуги. Животное или персонаж из мультфильма без признаков бизнеса это ок.\n"
        + "В SelfPromotionProbability ответь, с какой вероятностью профиль конкретного человека направлен на само-продвижение: личный блог, экспертность, коучинг, HR, консультации, предложение вступить в группу, подписываться, получить бесплатные продукты, документы, дипломы, сертификаты, и другие способы привлечения"
        + "\nВот данные профиля:\n";

    private const string EroticPromptHeader =
        "Проанализируй, выглядит ли этот профиль пользователя сексуализированно или развратно: имя, юзернейм и аватарка. "
        + "Обрати внимание на эмодзи с двойным смыслом (💦💋👄🍑🍆🍒🍓🍌 и прочих) и на любой намёк на эротику, порно, OnlyFans, приглашение в личные сообщения. "
        + "Отвечай вероятностью от 0 до 1.\nВот данные профиля:\n";

    private const string SpamSystemMessage =
        "Ты — модератор Telegram-группы, оценивающий сообщения в чате на спам, мошенничество и продвижения сторонних ресурсов или услуг";

    private const string SpamPromptHeader =
        "Проанализируй, выглядит ли это сообщение как спам или мошенничество, созданное с целью привлечения внимания и продвижения. Отвечай вероятностью от 0 до 1. Частые примеры: казино, гэмблинг, наркотики, эротика, порно, сексуализированные сообщения, схема заработка с обещаниями высокой прибыли, схема заработка без подробностей, неофициальное трудоустройство, срочный набор на работу, NFT, крипто, призыв перейти по ссылке, призыв писать в личные сообщения, услуги рассылки и продвижения, выпрашивание денег под жалобным предлогом, предложение поделиться ресурсами и книгами по трейдингу или инвестициям, промокоды, реклама, увеличение трафика или потока клиентов, подарочные сертификаты и другие цифровые промокоды со скидкой. Обрати внимание если язык на котором общаются в чате и язык сообщения не совпадают (например, в чате пишут по-русски, а в сообщении 'привет' по-арабски).";

    private static SpamPhotoBio NoBait => new(new BioClassProbability(), [], "");

    private static string ChatInfoCacheKey(long chatId) => $"chat_info:{chatId}";

    private static string LinkedChannelInfoCacheKey(long channelId) => $"linked_channel_info:{channelId}";

    public async ValueTask<SpamPhotoBio> GetAttentionBaitProbability(
        long chatId,
        Telegram.Bot.Types.User user,
        ChatFullInfo userChat,
        Func<string, ChatFullInfo, Task>? ifChanged = default,
        CancellationToken cancellationToken = default
    )
    {
        var endpoint = EndpointFor(chatId);
        if (endpoint == null)
            return NoBait;
        // whitelist is checked by user id, before the key: a content addressed key has nothing to overwrite
        if (await _userManager.IsHalfApproved(user.Id))
            return NoBait;

        try
        {
            var inputs = await CollectProfileInputs(user, userChat, cancellationToken);
            var prompt = RenderProfilePrompt(inputs);
            return await _hybridCache.GetOrCreateAsync(
                endpoint.CacheKey(prompt.Key),
                async ct =>
                {
                    SpamPhotoBio verdict;
                    if (inputs.Bio == null && inputs.LinkedChannel == null && inputs.PhotoBigFileId == null)
                    {
                        _logger.LogDebug("GetAttentionBaitProbability {User}: nothing to ask about", Utils.FullName(user));
                        verdict = NoBait;
                    }
                    else
                    {
                        _logger.LogDebug("GetAttentionBaitProbability {User} cache miss, asking LLM", Utils.FullName(user));
                        verdict = await AskProfileLlm(prompt, endpoint, ct);
                    }
                    // the watcher starts only once there is a verdict to cache: the factory reruns on every message until
                    // it succeeds, so spawning above would leave one watcher per failed LLM call
                    if (ifChanged != default)
                        _ = CheckLater(userChat, ifChanged, ct);
                    return verdict;
                },
                new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromDays(7) },
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            // nothing is cached when the factory throws, so the next message retries instead of reusing a zero verdict
            _logger.LogWarning(e, nameof(GetAttentionBaitProbability));
            return NoBait;
        }
    }

    private async Task<ProfileInputs> CollectProfileInputs(
        Telegram.Bot.Types.User user,
        ChatFullInfo userChat,
        CancellationToken ct = default
    )
    {
        var avatar = userChat.Photo;
        // identity comes from the chat, not from the message: the callback path re-checks a profile that has since been renamed
        var fullName = Utils.FullName(userChat.FirstName ?? user.FirstName, userChat.LastName);
        var userName = userChat.Username ?? user.Username;

        PromptSection? linkedChannel = null;
        var linked = userChat.LinkedChatId;
        if (linked != null)
        {
            try
            {
                linkedChannel = ChannelSection("Информация о привязанном канале:", await _bot.GetChat(linked, cancellationToken: ct));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // a private linked channel is a 400 on every message, and a null section would silently downgrade
                // the whole check to the erotic-only branch, so say it out loud instead
                _logger.LogWarning(e, "Unable to fetch linked channel {ChannelId}", linked);
                linkedChannel = new PromptSection($"Информация о привязанном канале: недоступна (id {linked})", null, null);
            }
        }

        var mentioned = new List<PromptSection>();
        if (userChat.Bio != null)
        {
            var alreadyIncluded = new List<string>();
            var matches = MyRegexes.TelegramUsername().Matches(userChat.Bio);
            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;
                var relevantGroups = match
                    .Groups.Cast<System.Text.RegularExpressions.Group>()
                    .Skip(1) // 0th groups is full match
                    .Where(g => g.Success);

                foreach (System.Text.RegularExpressions.Group group in relevantGroups)
                {
                    var username = $"@{group.Value}";
                    if (alreadyIncluded.Contains(username))
                        continue;
                    if (alreadyIncluded.Count >= 3)
                        break;
                    alreadyIncluded.Add(username);
                    try
                    {
                        var mentionedChat = await _bot.GetChat(username, cancellationToken: ct);
                        mentioned.Add(ChannelSection("Информация об упомянутом канале:", mentionedChat));
                    }
                    catch (Exception e)
                    {
                        // an unresolvable username is normal in a bio, the rest of the profile is still worth checking
                        _logger.LogWarning(e, "Unable to fetch mentioned channel {Username}", username);
                    }
                }
            }
        }

        return new ProfileInputs(
            user.Id,
            fullName,
            userName,
            userChat.Bio,
            avatar?.BigFileUniqueId,
            avatar?.BigFileId,
            linkedChannel,
            mentioned
        );
    }

    private static PromptSection ChannelSection(string header, ChatFullInfo chat)
    {
        var info = new StringBuilder();
        info.Append(CultureInfo.InvariantCulture, $"{header}\nНазвание: {chat.Title}");
        if (chat.Username != null)
            info.Append(CultureInfo.InvariantCulture, $"\nЮзернейм: @{chat.Username}");
        if (chat.Description != null)
            info.Append(CultureInfo.InvariantCulture, $"\nОписание: {chat.Description}");
        if (chat.Photo != null)
            info.Append("\nФото:");
        return new PromptSection(info.ToString(), chat.Photo?.BigFileUniqueId, chat.Photo?.BigFileId);
    }

    internal static ProfilePrompt RenderProfilePrompt(ProfileInputs inputs)
    {
        var nameBio = new StringBuilder();
        nameBio.Append(CultureInfo.InvariantCulture, $"Имя: {inputs.FullName}");
        if (inputs.Username != null)
            nameBio.Append(CultureInfo.InvariantCulture, $"\nЮзернейм: @{inputs.Username}");
        if (inputs.Bio != null)
            nameBio.Append(CultureInfo.InvariantCulture, $"\nОписание: {inputs.Bio}");
        if (inputs.PhotoBigFileId != null)
            nameBio.Append("\nФото: ");
        var nameBioString = nameBio.ToString();

        // no bio and no linked channel means there is nothing to judge but the name and the avatar, so we only ask about erotics
        var eroticOnly = inputs.Bio == null && inputs.LinkedChannel == null;
        var systemMessage = eroticOnly ? null : ProfileSystemMessage;
        var sections = new List<PromptSection>
        {
            new((eroticOnly ? EroticPromptHeader : ProfilePromptHeader) + nameBioString, inputs.PhotoUniqueId, inputs.PhotoBigFileId),
        };
        if (!eroticOnly)
        {
            if (inputs.LinkedChannel != null)
                sections.Add(inputs.LinkedChannel);
            sections.AddRange(inputs.MentionedChannels);
        }

        var keyMaterial = new StringBuilder();
        // the model is not hashed in: the endpoint prefixes the key with its own model name
        keyMaterial.Append(inputs.UserId).Append('\n').Append(systemMessage);
        foreach (var section in sections)
            keyMaterial.Append('\n').Append(section.Text).Append('\n').Append(section.PhotoUniqueId);

        return new ProfilePrompt(
            systemMessage,
            sections,
            eroticOnly,
            eroticOnly ? inputs.FullName : nameBioString,
            $"attention:{ShaHelper.ComputeSha256Hex(keyMaterial.ToString())}"
        );
    }

    private async ValueTask<SpamPhotoBio> AskProfileLlm(ProfilePrompt prompt, LlmEndpoint endpoint, CancellationToken ct)
    {
        var messages = new List<ChatCompletionRequestMessage>();
        if (prompt.SystemMessage != null)
            messages.Add(prompt.SystemMessage.AsSystemMessage());

        var pic = Array.Empty<byte>();
        for (var i = 0; i < prompt.Sections.Count; i++)
        {
            var section = prompt.Sections[i];
            messages.Add(section.Text.AsUserMessage());
            if (section.PhotoBigFileId == null)
                continue;
            using var ms = new MemoryStream();
            await _bot.GetInfoAndDownloadFile(section.PhotoBigFileId, ms, cancellationToken: ct);
            var photoBytes = ms.ToArray();
            // section 0 is the user themselves, so its photo is the avatar, the rest are channel photos
            if (i == 0)
                pic = photoBytes;
            messages.Add(CreateContextImageMessage(photoBytes));
        }
        _logger.LogDebug("LLM prompt: {Prompt}", string.Join('\n', prompt.Sections.Select(x => x.Text)));

        var probability = await AskProfileModel(prompt.EroticOnly, messages, endpoint, ct);
        if (
            probability.EroticProbability < Consts.LlmLowProbability
            && probability.NonPersonProbability < Consts.LlmLowProbability
            && probability.SelfPromotionProbability < Consts.LlmLowProbability
            && probability.GamblingProbability < Consts.LlmLowProbability
        )
            pic = []; // cache optimization, don't store all user photos who are not spammers
        return new SpamPhotoBio(probability, pic, prompt.NameBio);
    }

    private async Task<BioClassProbability> AskProfileModel(
        bool eroticOnly,
        List<ChatCompletionRequestMessage> messages,
        LlmEndpoint endpoint,
        CancellationToken ct
    )
    {
        if (eroticOnly)
        {
            var erotic = await endpoint.Retry.ExecuteAsync(
                async token =>
                    await endpoint.Api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                        messages: messages,
                        model: endpoint.Model,
                        strict: true,
                        jsonSerializerOptions: jso,
                        cancellationToken: token
                    ),
                ct
            );
            if (erotic.Value1 == null)
            {
                _logger.LogWarning("LLM GetEroticPhotoBaitProbability: {@Resp}", erotic);
                throw new InvalidOperationException("LLM returned no parsed erotic verdict");
            }
            var probability = new BioClassProbability { EroticProbability = erotic.Value1.Probability, Reason = erotic.Value1.Reason };
            _logger.LogInformation("LLM GetEroticPhotoBaitProbability: {@Prob}", probability);
            return probability;
        }

        var response = await endpoint.Retry.ExecuteAsync(
            async token =>
                await endpoint.Api.Chat.CreateChatCompletionAsAsync<BioClassProbability>(
                    messages: messages,
                    model: endpoint.Model,
                    strict: true,
                    jsonSerializerOptions: jso,
                    cancellationToken: token
                ),
            ct
        );
        if (response.Value1 == null)
        {
            _logger.LogWarning("LLM GetAttentionBaitProbability: {@Resp}", response);
            throw new InvalidOperationException("LLM returned no parsed profile verdict");
        }
        _logger.LogInformation("LLM GetAttentionBaitProbability: {@Prob}", response.Value1);
        return response.Value1;
    }

    internal record ChatDescription(string Description, long? ChannelId);

    private async ValueTask<ChatDescription?> GetChatInfoAsync(long chatId, CancellationToken ct = default)
    {
        try
        {
            return await _hybridCache.GetOrCreateAsync<ChatDescription?>(
                ChatInfoCacheKey(chatId),
                async ct =>
                {
                    var chat = await _bot.GetChat(chatId, cancellationToken: ct);
                    var info = new StringBuilder();
                    info.AppendLine(CultureInfo.InvariantCulture, $"Чат: {chat.Title}");
                    if (chat.Description != null)
                        info.AppendLine(CultureInfo.InvariantCulture, $"Описание чата: {chat.Description}");

                    return new(info.ToString(), chat.LinkedChatId);
                },
                new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromHours(24) },
                cancellationToken: ct
            );
        }
        catch (Exception e)
        {
            // this text is part of the spam prompt and of its key, so a failure must not stick around for a day
            _logger.LogWarning(e, "Failed to get chat info for {ChatId}", chatId);
            return null;
        }
    }

    private async ValueTask<string> GetLinkedChannelInfoAsync(long channelId, CancellationToken ct = default)
    {
        try
        {
            return await _hybridCache.GetOrCreateAsync(
                LinkedChannelInfoCacheKey(channelId),
                async ct =>
                {
                    var linkedChat = await _bot.GetChat(channelId, cancellationToken: ct);
                    var info = new StringBuilder();
                    info.AppendLine(CultureInfo.InvariantCulture, $"Этот чат - чат обсуждения для канала: {linkedChat.Title}");
                    if (linkedChat.Description != null)
                        info.AppendLine(CultureInfo.InvariantCulture, $"Описание канала: {linkedChat.Description}");

                    return info.ToString();
                },
                new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromHours(24) },
                cancellationToken: ct
            );
        }
        catch (Exception e)
        {
            // same reason as above: a cached empty description silently changes the spam prompt and its key
            _logger.LogWarning(e, "Failed to get linked channel info for {ChannelId}", channelId);
            return string.Empty;
        }
    }

    private async Task CheckLater(ChatFullInfo userChat, Func<string, ChatFullInfo, Task> ifChanged, CancellationToken ct = default)
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
                    // this branch bans outright and never looks at the snapshot, so the stale one is fine
                    await ifChanged.Invoke("пользователь теперь в блеклисте спамеров", userChat);
                    return;
                }

                var chat = await _bot.GetChat(userChat.Id, cancellationToken: ct);
                if (chat.Photo?.BigFileUniqueId != userChat.Photo?.BigFileUniqueId)
                {
                    await ifChanged.Invoke("пользователь сменил фото", chat);
                    return;
                }
                if (chat.Bio != userChat.Bio)
                {
                    await ifChanged.Invoke(
                        $"пользователь сменил био.{Environment.NewLine}новое: {chat.Bio}{Environment.NewLine}старое: {userChat.Bio}",
                        chat
                    );
                    return;
                }
                if (chat.LinkedChatId != userChat.LinkedChatId)
                {
                    await ifChanged.Invoke("у пользователя сменился привязанный канал", chat);
                    return;
                }
                if (chat.FirstName != userChat.FirstName)
                {
                    await ifChanged.Invoke($"пользователь сменил имя{Environment.NewLine}новое: {chat.FirstName}", chat);
                    return;
                }
                if (chat.LastName != userChat.LastName)
                {
                    await ifChanged.Invoke($"пользователь сменил фамилию{Environment.NewLine}новая: {chat.LastName}", chat);
                    return;
                }
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning(e, nameof(CheckLater));
        }
    }

    public async ValueTask<SpamProbability> GetSpamProbability(Telegram.Bot.Types.Message message)
    {
        var endpoint = EndpointFor(message.Chat.Id);
        if (endpoint == null)
            return new SpamProbability();

        var text = Utils.TextWithLinks(message) ?? "";
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

        var selectedPhoto = message.Photo is { Length: > 0 } ? SelectHighestQualityPhoto(message.Photo) : null;

        try
        {
            var chatInfo = await GetChatInfoAsync(message.Chat.Id);
            var linkedInfo = chatInfo?.ChannelId == null ? null : await GetLinkedChannelInfoAsync(chatInfo.ChannelId.Value);
            var prompt = BuildSpamPrompt(
                text,
                chatInfo?.Description,
                linkedInfo,
                message.ReplyToMessage == null ? null : Utils.TextWithLinks(message.ReplyToMessage),
                message.ReplyToMessage?.IsAutomaticForward == true,
                selectedPhoto?.FileUniqueId
            );

            return await _hybridCache.GetOrCreateAsync(
                endpoint.CacheKey(prompt.Key),
                async ct => await AskSpamLlm(prompt.Text, selectedPhoto, endpoint, ct),
                new HybridCacheEntryOptions { LocalCacheExpiration = TimeSpan.FromDays(1) }
            );
        }
        catch (Exception e)
        {
            // nothing is cached when the factory throws, so the next identical message asks the model again
            _logger.LogWarning(e, nameof(GetSpamProbability));
            return new SpamProbability();
        }
    }

    internal static SpamPrompt BuildSpamPrompt(
        string text,
        string? chatInfo,
        string? linkedInfo,
        string? replyTo,
        bool replyToIsChannelPost,
        string? photoUniqueId
    )
    {
        var contextBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(chatInfo))
            contextBuilder.AppendLine(chatInfo);
        if (!string.IsNullOrEmpty(linkedInfo))
            contextBuilder.AppendLine(linkedInfo);
        if (!string.IsNullOrEmpty(replyTo))
        {
            contextBuilder.AppendLine("###");
            contextBuilder.AppendLine(replyToIsChannelPost ? "Пост в канале, на который отвечают:" : "Сообщение, на которое отвечают:");
            contextBuilder.AppendLine(replyTo);
        }

        var fullPrompt = new StringBuilder();
        fullPrompt.AppendLine(SpamPromptHeader);
        fullPrompt.AppendLine("###");
        fullPrompt.AppendLine("Контекст сообщения:");
        fullPrompt.AppendLine(contextBuilder.ToString());
        fullPrompt.AppendLine("###");
        if (!string.IsNullOrWhiteSpace(text))
            fullPrompt.AppendLine(CultureInfo.InvariantCulture, $"Само сообщение, которое нужно проанализировать:\n{text}");
        else
            fullPrompt.AppendLine("Само сообщение не содержит текста, только изображение.");

        var promptText = fullPrompt.ToString();
        // the picture is part of the input but not of the text, so it has to be part of the key
        return new SpamPrompt(promptText, $"llm_spam_prob:{ShaHelper.ComputeSha256Hex($"{promptText}\nPhoto: {photoUniqueId}")}");
    }

    private async ValueTask<SpamProbability> AskSpamLlm(string prompt, PhotoSize? photo, LlmEndpoint endpoint, CancellationToken ct)
    {
        byte[]? imageBytes = null;
        if (photo != null)
        {
            _logger.LogDebug(
                "GetSpamProbability selected message photo {Width}x{Height}, file size {FileSize}",
                photo.Width,
                photo.Height,
                photo.FileSize
            );
            using var ms = new MemoryStream();
            await _bot.GetInfoAndDownloadFile(photo.FileId, ms, cancellationToken: ct);
            imageBytes = ms.ToArray();
        }

        _logger.LogInformation(
            "GetSpamProbability full prompt - System: {System}, User: {User}, HasImage: {HasImage}, Model: {Model}",
            SpamSystemMessage,
            prompt,
            imageBytes != null,
            endpoint.Model
        );

        var messages = new List<ChatCompletionRequestMessage> { SpamSystemMessage.AsSystemMessage(), prompt.AsUserMessage() };
        if (imageBytes != null)
            messages.Add(CreateSpamImageMessage(imageBytes));

        var response = await endpoint.Retry.ExecuteAsync(
            async token =>
                await endpoint.Api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: endpoint.Model,
                    strict: true,
                    jsonSerializerOptions: jso,
                    cancellationToken: token
                ),
            ct
        );
        if (response.Value1 == null)
        {
            _logger.LogWarning("LLM GetSpamProbability resp {@Resp}", response);
            throw new InvalidOperationException("LLM returned no parsed spam verdict");
        }
        _logger.LogInformation("LLM GetSpamProbability {@Prob}", response.Value1);
        return response.Value1;
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

    /// <summary>Everything the profile check takes from Telegram, already fetched. Channel sections arrive as ready made text.</summary>
    internal sealed record ProfileInputs(
        long UserId,
        string FullName,
        string? Username,
        string? Bio,
        string? PhotoUniqueId,
        string? PhotoBigFileId,
        PromptSection? LinkedChannel,
        IReadOnlyList<PromptSection> MentionedChannels
    );

    /// <summary>One user message and the photo that follows it, if any.</summary>
    internal sealed record PromptSection(string Text, string? PhotoUniqueId, string? PhotoBigFileId);

    internal sealed record ProfilePrompt(
        string? SystemMessage,
        IReadOnlyList<PromptSection> Sections,
        bool EroticOnly,
        string NameBio,
        string Key
    );

    internal sealed record SpamPrompt(string Text, string Key);

    /// <summary>An OpenAI compatible endpoint together with the model to ask. Cached verdicts are keyed per model.</summary>
    private sealed record LlmEndpoint(OpenAiClient Api, string Model, ResiliencePipeline Retry)
    {
        public string CacheKey(string promptKey) => $"{promptKey}:{Model}";
    }
}
