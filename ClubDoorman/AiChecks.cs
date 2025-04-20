using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telegram.Bot;
using tryAGI.OpenAI;

namespace ClubDoorman
{
    internal class AiChecks(ILogger<AiChecks> logger)
    {
        private readonly TelegramBotClient _bot = new(Config.BotApi);
        private static readonly OpenAiClient? api = Config.OpenRouterApi == null ? null : CustomProviders.OpenRouter(Config.OpenRouterApi);
        private readonly JsonSerializerOptions jso = new() { Converters = { new JsonStringEnumConverter() } };

        public async ValueTask<double> GetAttentionSpammerProbability(Telegram.Bot.Types.User user)
        {
            var probability = 0.0;
            if (api == null)
                return probability;

            try
            {
                var userChat = await _bot.GetChat(user.Id);
                var photo = userChat.Photo;
                byte[]? photoBytes = null;
                ChatCompletionRequestUserMessage? photoMessage = null;
                
                if (photo != null)
                {
                    using var ms = new MemoryStream();
                    await _bot.GetInfoAndDownloadFile(photo.BigFileId, ms);
                    photoBytes = ms.ToArray();
                    photoMessage = photoBytes.AsUserMessage(
                        mimeType: "image/jpg",
                        detail: ChatCompletionRequestMessageContentPartImageImageUrlDetail.Low
                    );
                }
                var bio = userChat.Bio;
                var fullName = string.IsNullOrEmpty(user.LastName) ? user.FirstName : $"{user.FirstName} {user.LastName}";
                var userName = user.Username;

                var sb = new StringBuilder();
                sb.Append(
                    "Проанализируй, выглядит ли этот Telegram-профиль как подозрительный, созданный с целью привлечения внимания и продвижения чего-то (например, курсов, сомнительных схем заработка, OnlyFans и т.п.). Вот данные:\n Имя: "
                );
                sb.Append(fullName);
                if (userName != null)
                    sb.Append($"\nЮзернейм: @{userName}");
                sb.Append($"\nОписание профиля: {bio}");
                if (photoBytes != null)
                    sb.Append($"\nФото: ");

                var messages = new List<ChatCompletionRequestMessage>
                {
                    "Ты — ассистент, оценивающий профили на предмет того, созданы ли они для привлечения внимания и продвижения сторонних ресурсов или услуг. Учитывай признаки:\nсексуализированные женские профили (эмодзи, имена типа \"Алиночка 💋\"),\nупоминания о курсах, заработке, трейдинге, арбитраже,\nслова вроде \"миллион\", \"марафон\", \"путь к свободе\", \"доход\", \"коуч\", \"успей\",\nссылки на OnlyFans, каналы, соцсети.".AsSystemMessage(),
                    sb.ToString().AsUserMessage(),
                };
                if (photoMessage != null)
                    messages.Add(photoMessage);

                var response = await api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: "google/gemini-2.5-flash-preview",
                    strict: true,
                    jsonSerializerOptions: jso
                );
                if (response.Value1 != null)
                {
                    probability = response.Value1.Probability;
                    logger.LogInformation("Ответ сервера {Prob}", probability);
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "GetAttentionSpammerProbability");
            }
            return probability;
        }

        internal class SpamProbability()
        {
            public double Probability { get; set; }
        }
    }
}
