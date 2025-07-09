using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Caching;
using Polly;
using Polly.Retry;
using Telegram.Bot;
using Telegram.Bot.Types;
using tryAGI.OpenAI;

namespace ClubDoorman;

internal class AiChecks
{
    private readonly TelegramBotClient _bot;
    private readonly ILogger _logger;
    private readonly OpenAiClient? _api;
    private readonly JsonSerializerOptions _jsonOptions = new() { Converters = { new JsonStringEnumConverter() } };
    
    // Retry policy для обработки временных ошибок API
    private readonly ResiliencePipeline _retry = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions() { Delay = TimeSpan.FromMilliseconds(50) })
        .Build();
    
    const string Model = "google/gemini-2.5-flash";
    
    public AiChecks(TelegramBotClient bot, ILogger logger)
    {
        _bot = bot;
        _logger = logger;
        _api = Config.OpenRouterApi == null ? null : CustomProviders.OpenRouter(Config.OpenRouterApi);
    }

    private static string CacheKey(long userId) => $"ai_profile_check:{userId}";

    /// <summary>
    /// Отмечает пользователя как проверенного и безопасного
    /// </summary>
    public void MarkUserOkay(long userId)
    {
        var cacheItem = new CacheItem(CacheKey(userId), new SpamPhotoBio(new SpamProbability(), [], ""));
        var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddDays(30) };
        MemoryCache.Default.Set(cacheItem, policy);
        _logger.LogInformation("Пользователь {UserId} отмечен как безопасный", userId);
    }

    /// <summary>
    /// Получает вероятность того, что профиль создан для привлечения внимания/спама
    /// </summary>
    public async ValueTask<SpamPhotoBio> GetAttentionBaitProbability(Telegram.Bot.Types.User user, Func<string, Task>? ifChanged = default)
    {
        if (_api == null)
        {
            _logger.LogDebug("OpenAI API не настроен, пропускаем AI проверку профиля");
            return new SpamPhotoBio(new SpamProbability(), [], "");
        }

        var cached = MemoryCache.Default.Get(CacheKey(user.Id)) as SpamPhotoBio;
        if (cached != null)
        {
            _logger.LogDebug("Найден кэш для пользователя {UserId}: {Probability}", user.Id, cached.SpamProbability.Probability);
            return cached;
        }

        var probability = new SpamProbability();
        var pic = Array.Empty<byte>();
        var nameBioUser = string.Empty;

        try
        {
            var userChat = await _bot.GetChat(user.Id);
            
            // Если у пользователя нет био и нет связанного канала - проверяем только фото
            if (userChat.Bio == null && userChat.LinkedChatId == null)
            {
                _logger.LogDebug("У пользователя {UserId} нет био и связанного канала", user.Id);
                if (userChat.Photo != null)
                    return await GetEroticPhotoBaitProbability(user, userChat);
                
                // Нет ни био, ни фото - считаем безопасным
                var result = new SpamPhotoBio(new SpamProbability(), [], "");
                CacheResult(user.Id, result);
                return result;
            }

            _logger.LogDebug("Анализируем профиль пользователя {UserId} с помощью AI", user.Id);
            
            var sb = new StringBuilder();
            sb.Append($"Имя: {Utils.FullName(user)}");
            if (user.Username != null)
                sb.Append($"\nЮзернейм: @{user.Username}");
            if (userChat.Bio != null)
                sb.Append($"\nОписание: {userChat.Bio}");

            nameBioUser = sb.ToString();
            byte[]? photoBytes = null;
            ChatCompletionRequestUserMessage? photoMessage = null;

            // Загружаем фото профиля если есть
            if (userChat.Photo != null)
            {
                using var ms = new MemoryStream();
                await _bot.GetInfoAndDownloadFile(userChat.Photo.BigFileId, ms);
                photoBytes = ms.ToArray();
                pic = photoBytes;
                photoMessage = photoBytes.AsUserMessage(mimeType: "image/jpg");
                sb.Append($"\nФото: прикреплено");
            }

            var prompt = $"""
                Проанализируй, выглядит ли этот Telegram-профиль как «продажный» и созданный с целью привлечения внимания. 
                Отвечай вероятностью от 0 до 1. 
                
                Особенно внимательно учитывай признаки:
                • сексуализированные профили (эмодзи с двойным смыслом - 💦, 💋, 👄, 🍑, 🍆, 🍒, 🍓, 🍌 и прочих в имени, любой намёк на эротику и порно, голые фото)
                • упоминания о курсах, заработке, трейдинге, арбитраже, привлечению трафика
                • ссылки на OnlyFans, соцсети
                • род занятий указан прямо в имени (например: HR, SMM, недвижимость, маркетинг)
                
                Вот данные профиля:
                {nameBioUser}
                """;

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — модератор Telegram-группы. Твоя задача — по данным профиля определить, направлен ли аккаунт на само-продвижение или привлечение к сторонним платным/эротическим ресурсам".AsSystemMessage(),
                prompt.AsUserMessage(),
            };
            
            if (photoMessage != null)
                messages.Add(photoMessage);

            // Анализируем связанный канал если есть
            if (userChat.LinkedChatId != null)
            {
                try
                {
                    var linkedChat = await _bot.GetChat(userChat.LinkedChatId.Value);
                    var info = new StringBuilder();
                    info.Append($"Информация о привязанном канале:\nНазвание: {linkedChat.Title}");
                    if (linkedChat.Username != null)
                        info.Append($"\nЮзернейм: @{linkedChat.Username}");
                    if (linkedChat.Description != null)
                        info.Append($"\nОписание: {linkedChat.Description}");
                    
                    messages.Add(info.ToString().AsUserMessage());
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Не удалось получить информацию о связанном канале {ChannelId}", userChat.LinkedChatId);
                }
            }

            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );

            if (response.Value1 != null)
            {
                probability = response.Value1;
                _logger.LogInformation("AI анализ профиля пользователя {UserId}: {Probability} - {Reason}", 
                    user.Id, probability.Probability, probability.Reason);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при AI анализе профиля пользователя {UserId}", user.Id);
        }

        var finalResult = new SpamPhotoBio(probability, pic, nameBioUser);
        CacheResult(user.Id, finalResult);
        return finalResult;
    }

    /// <summary>
    /// Анализирует фото профиля на предмет сексуализированного контента
    /// </summary>
    private async ValueTask<SpamPhotoBio> GetEroticPhotoBaitProbability(Telegram.Bot.Types.User user, ChatFullInfo userChat)
    {
        if (_api == null)
            return new SpamPhotoBio(new SpamProbability(), [], "");

        var probability = new SpamProbability();
        var pic = Array.Empty<byte>();

        try
        {
            var photo = userChat.Photo!;
            using var ms = new MemoryStream();
            await _bot.GetInfoAndDownloadFile(photo.BigFileId, ms);
            var photoBytes = ms.ToArray();
            pic = photoBytes;
            
            var photoMessage = photoBytes.AsUserMessage(mimeType: "image/jpg");
            var prompt = "Проанализируй, выглядит ли эта аватарка пользователя сексуализированно или развратно. Отвечай вероятностью от 0 до 1.";
            
            var messages = new List<ChatCompletionRequestMessage> 
            { 
                prompt.AsUserMessage(), 
                photoMessage 
            };
            
            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );
            
            if (response.Value1 != null)
            {
                probability = response.Value1;
                _logger.LogInformation("AI анализ фото профиля пользователя {UserId}: {Probability}", 
                    user.Id, probability.Probability);
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при анализе фото профиля пользователя {UserId}", user.Id);
        }

        return new SpamPhotoBio(probability, pic, Utils.FullName(user));
    }

    /// <summary>
    /// Анализирует сообщение на предмет спама с помощью AI
    /// </summary>
    public async ValueTask<SpamProbability> GetSpamProbability(Message message)
    {
        if (_api == null)
            return new SpamProbability();

        try
        {
            var text = message.Text ?? message.Caption ?? "";
            if (string.IsNullOrWhiteSpace(text))
                return new SpamProbability();

            var prompt = $"""
                Проанализируй это сообщение на предмет спама. Отвечай вероятностью от 0 до 1.
                
                Признаки спама:
                • Реклама товаров/услуг
                • Просьбы о переходах по ссылкам
                • Навязчивые предложения
                • Массовые рассылки
                • Мошенничество
                
                Сообщение: {text}
                """;

            var messages = new List<ChatCompletionRequestMessage>
            {
                "Ты — антиспам модератор. Анализируй сообщения на предмет спама.".AsSystemMessage(),
                prompt.AsUserMessage()
            };

            var response = await _retry.ExecuteAsync(
                async token => await _api.Chat.CreateChatCompletionAsAsync<SpamProbability>(
                    messages: messages,
                    model: Model,
                    strict: true,
                    jsonSerializerOptions: _jsonOptions,
                    cancellationToken: token
                )
            );

            if (response.Value1 != null)
            {
                _logger.LogDebug("AI анализ сообщения: {Probability} - {Reason}", 
                    response.Value1.Probability, response.Value1.Reason);
                return response.Value1;
            }
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Ошибка при AI анализе сообщения");
        }

        return new SpamProbability();
    }

    private void CacheResult(long userId, SpamPhotoBio result)
    {
        var cacheItem = new CacheItem(CacheKey(userId), result);
        var policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddHours(24) };
        MemoryCache.Default.Set(cacheItem, policy);
    }
}

// Модели данных для AI ответов
internal class SpamProbability
{
    public double Probability { get; set; }
    public string Reason { get; set; } = "";
}

internal sealed record SpamPhotoBio(SpamProbability SpamProbability, byte[] Photo, string NameBio); 