using System.Runtime.Caching;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Polly;
using Polly.Retry;
using Telegram.Bot;
using Telegram.Bot.Types;
using tryAGI.OpenAI;

namespace ClubDoorman;

internal class AiChecks
{
    public AiChecks(ITelegramBotClient bot, Config config, ILogger<AiChecks> logger)
    {
        _bot = bot;
        _config = config;
        _logger = logger;
        _api = _config.OpenRouterApi == null ? null : CustomProviders.OpenRouter(_config.OpenRouterApi);
    }

    private readonly ResiliencePipeline _retry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions() { Delay = TimeSpan.FromMilliseconds(50) })
        .Build();
    const string Model = "google/gemini-2.5-flash-preview";
    private readonly OpenAiClient? _api;
    private readonly JsonSerializerOptions jso = new() { Converters = { new JsonStringEnumConverter() } };
    private readonly ITelegramBotClient _bot;
    private readonly Config _config;
    private readonly ILogger<AiChecks> _logger;

    public static void MarkUserOkay(long userId)
    {
        var cacheKey = $"attention:{userId}";
        MemoryCache.Default.Add(cacheKey, (double?)0.0, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddYears(1) });
    }

    public async ValueTask<(double, byte[], string)> GetAttentionBaitProbability(
        Telegram.Bot.Types.User user,
        bool checkEvenIfNoBio = false
    )
    {
        var probability = 0.0;
        var nameBioUser = "";
        var pic = Array.Empty<byte>();
        if (_api == null)
            return (probability, pic, nameBioUser);

        var cacheKey = $"attention:{user.Id}";
        var exists = MemoryCache.Default.Get(cacheKey) as double?;
        if (exists.HasValue)
            return (exists.Value, pic, nameBioUser);

        try
        {
            var userChat = await _bot.GetChat(user.Id);
            if (!checkEvenIfNoBio && userChat.Bio == null && userChat.LinkedChatId == null)
            {
                _logger.LogDebug("GetAttentionBaitProbability {User} skipping: no bio, no channel", Utils.FullName(user));
                return (probability, pic, nameBioUser);
            }

            _logger.LogDebug("GetAttentionBaitProbability {User} cache miss, asking LLM", Utils.FullName(user));
            var photo = userChat.Photo;
            byte[]? photoBytes = null;
            ChatCompletionRequestUserMessage? photoMessage = null;

            if (photo != null)
            {
                using var ms = new MemoryStream();
                await _bot.GetInfoAndDownloadFile(photo.BigFileId, ms);
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
                $"Проанализируй, выглядит ли этот Telegram-профиль как «продажный» и созданный с целью привлечения внимания. Отвечай вероятностью от 0 до 1. Особенно внимательно учитывай признаки:\nсексуализированные профили (эмодзи с двойным смыслом - 💦, 💋, 👄, 🍑, 🍆, 🍒, 🍓, 🍌 и прочих в имени, любой намёк на эротику и порно, голые фото), упоминания о курсах, заработке, трейдинге, арбитраже, привлечению трафика, ссылки на OnlyFans, соцсети. Обращай внимание, если профессия или род занятий указано прямо в имени (например, HR, SMM или маркетинг). Вот данные профиля:\n{nameBioUser}";

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
                var linkedChat = await _bot.GetChat(linked);
                var info = new StringBuilder();
                sb.Append($"Информация о привязанном канале:\nНазвание: {linkedChat.Title}");
                if (linkedChat.Username != null)
                    sb.Append($"\nЮзернейм: @{linkedChat.Username}");
                if (linkedChat.Description != null)
                    sb.Append($"\nОписание: {linkedChat.Description}");
                if (linkedChat.Photo != null)
                {
                    sb.Append($"\nФото:");
                    using var ms = new MemoryStream();
                    await _bot.GetInfoAndDownloadFile(linkedChat.Photo.BigFileId, ms);
                    channelPhoto = ms.ToArray();
                }
                var sbStr = sb.ToString();
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

            _logger.LogDebug("LLM prompt: {Promt}", promptDebugString);

            var response = await _retry.ExecuteAsync(async token =>
                await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: jso,
                    cancellationToken: token
                )
            );
            if (response.Value1 != null)
            {
                probability = response.Value1.Probability;
                MemoryCache.Default.Add(cacheKey, (double?)probability, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromDays(3) });
                _logger.LogInformation("LLM GetAttentionBaitProbability: {Prob}", probability);
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
        return (probability, pic, nameBioUser);
    }

    public async ValueTask<double> GetSpamProbability(Message message)
    {
        var probability = 0.0;
        if (_api == null)
            return probability;

        var text = message.Caption ?? message.Text;
        var cacheKey = $"llm_spam_prob:{text}";
        var exists = MemoryCache.Default.Get(cacheKey) as double?;
        if (exists.HasValue)
            return exists.Value;

        try
        {
            byte[]? imageBytes = null;
            if (message.Photo != null)
            {
                using var ms = new MemoryStream();
                await _bot.GetInfoAndDownloadFile(message.Photo.OrderBy(x => x.Width).First().FileId, ms);
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
                    imageBytes.AsUserMessage(mimeType: "image/jpg", detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low)
                );

            var response = await _retry.ExecuteAsync(async token =>
                await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: jso,
                    cancellationToken: token
                )
            );
            if (response.Value1 != null)
            {
                probability = response.Value1.Probability;
                MemoryCache.Default.Add(cacheKey, (double?)probability, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromHours(1) });
                _logger.LogInformation("LLM GetSpamProbability {Prob}", probability);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, nameof(GetSpamProbability));
        }
        return probability;
    }

    internal class SpamProbability()
    {
        public double Probability { get; set; }
    }
}
