using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot;
using Telegram.Bot.Types;
using tryAGI.OpenAI;

namespace ClubDoorman;


internal class AiChecks(ITelegramBotClient bot, ILogger<AiChecks> logger)
{
    const string Model = "google/gemini-2.5-flash-preview";
    private static readonly OpenAiClient? api = Config.OpenRouterApi == null ? null : CustomProviders.OpenRouter(Config.OpenRouterApi);
    private readonly JsonSerializerOptions jso = new() { Converters = { new JsonStringEnumConverter() } };

    public static void MarkUserOkay(long userId)
    {
        var cacheKey = $"attention:{userId}";
        MemoryCache.Default.Add(cacheKey, (double?)0.0, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1) });
    }

    public async ValueTask<(double, byte[], string)> GetAttentionBaitProbability(Telegram.Bot.Types.User user)
    {
        var probability = 0.0;
        var nameBioUser = "";
        var pic = Array.Empty<byte>();
        if (api == null)
            return (probability, pic, nameBioUser);

        var cacheKey = $"attention:{user.Id}";
        var exists = MemoryCache.Default.Get(cacheKey) as double?;
        if (exists.HasValue)
            return (exists.Value, pic, nameBioUser);

        try
        {
            var userChat = await bot.GetChat(user.Id);
            if (string.IsNullOrWhiteSpace(userChat.Bio))
                return (probability, pic, nameBioUser);
            var photo = userChat.Photo;
            byte[]? photoBytes = null;
            ChatCompletionRequestUserMessage? photoMessage = null;

            if (photo != null)
            {
                using var ms = new MemoryStream();
                await bot.GetInfoAndDownloadFile(photo.BigFileId, ms);
                photoBytes = ms.ToArray();
                pic = photoBytes;
                photoMessage = photoBytes.AsUserMessage(
                    mimeType: "image/jpg",
                    detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low
                );
            }
            var bio = userChat.Bio;
            var fullName = Utils.FullName(user);
            var userName = user.Username;

            var sb = new StringBuilder();
            sb.Append($"Имя: {fullName}");
            if (userName != null)
                sb.Append($"\nЮзернейм: @{userName}");
            sb.Append($"\nОписание профиля: {bio}");
            if (photoBytes != null)
                sb.Append($"\nФото: ");

            nameBioUser = sb.ToString();
            var promt =
                $"Проанализируй, выглядит ли этот Telegram-профиль как подозрительный, созданный с целью привлечения внимания и продвижения чего-то (например, курсов, крипто-схем, сомнительных схем заработка, OnlyFans, эротика и порно, сексуальные услуги и т.п.). Вот данные:\n{nameBioUser}";

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — ассистент, оценивающий профили на предмет того, созданы ли они для привлечения внимания и продвижения сторонних ресурсов или услуг. Учитывай признаки:\nсексуализированные женские профили (эмодзи капелек, поцелуйчиков, персиков и прочих в имени, любой намёк на эротику и порно, голые фото),\nупоминания о курсах, заработке, трейдинге, арбитраже,\nслова вроде \"миллион\", \"марафон\", \"путь к свободе\", \"доход\", \"коуч\", \"успей\",\nссылки на OnlyFans, каналы, соцсети.".AsSystemMessage(),
                promt.AsUserMessage(),
            };
            if (photoMessage != null)
                messages.Add(photoMessage);


            var response = await api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                messages: messages,
                model: Model,
                strict: true,
                jsonSerializerOptions: jso
            );
            if (response.Value1 != null)
            {
                probability = response.Value1.Probability;
                MemoryCache.Default.Add(
                    cacheKey,
                    (double?)probability,
                    new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(1) }
                );
                logger.LogInformation("LLM GetAttentionBaitProbability: {Prob}", probability);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "GetAttentionSpammerProbability");
        }
        return (probability, pic, nameBioUser);
    }

    public async ValueTask<double> GetSpamProbability(Message message)
    {
        var probability = 0.0;
        if (api == null)
            return probability;

        try
        {
            byte[]? imageBytes = null;
            if (message.Photo != null)
            {
                using var ms = new MemoryStream();
                await bot.GetInfoAndDownloadFile(message.Photo.OrderBy(x => x.Width).First().FileId, ms);
                imageBytes = ms.ToArray();
            }

            var promt =
                $"Проанализируй, выглядит ли это сообщение как спам или мошенничество, созданное с целью привлечения внимания и продвижения. Отвечай вероятностью от 0 до 1. Частые примеры: казино, гэмблинг, наркотики, эротика, порно, сексуализированные сообщения, схема заработка с обещаниями высокой прибыли, схема заработка без подробностей, неофициальное трудоустройство, срочный набор на работу, NFT, крипто, призыв перейти по ссылке, призыв писать в личные сообщения (или же \"в ЛС\"), услуги рассылки и продвижения, выпрашивание денег под жалобным предлогом, предложение поделиться ресурсами и книгами по трейдингу или инвестициям, промокоды, реклама, увеличение трафика или потока клиентов, подарочные сертификаты и другие цифровые промокоды со скидкой. Сообщение:\n";

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — ассистент, оценивающий сообщения в Telegram-чате на спам, мошенничество и продвижения сторонних ресурсов или услуг".AsSystemMessage(),
                (promt + (message.Caption ?? message.Text)).AsUserMessage(),
            };
            if (imageBytes != null)
                messages.Add(
                    imageBytes.AsUserMessage(mimeType: "image/jpg", detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low)
                );

            var response = await api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                messages: messages,
                model: Model,
                strict: true,
                jsonSerializerOptions: jso
            );
            if (response.Value1 != null)
            {
                probability = response.Value1.Probability;
                logger.LogInformation("LLM GetSpamProbability {Prob}", probability);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, nameof(GetSpamProbability));
        }
        return probability;
    }

    internal class SpamProbability()
    {
        public double Probability { get; set; }
    }
}
