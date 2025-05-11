using Microsoft.Extensions.Caching.Hybrid;
using NsfwONNX;
using Telegram.Bot;
using UMapx.Core;

namespace ClubDoorman;

internal sealed record UserAndChannelNsfw(string? User, string? Channel);

internal class NsfwChecks(ITelegramBotClient bot, HybridCache hybridCache, ILogger<NsfwChecks> logger)
{
    private readonly ITelegramBotClient _bot = bot;
    private readonly HybridCache _hybridCache = hybridCache;
    private readonly ILogger<NsfwChecks> _logger = logger;
    private readonly Lock _lock = new();
    private readonly OpenNsfwClassifier _nsfwClassifier = new();

    public ValueTask<UserAndChannelNsfw> GetPicturesNsfwRating(
        Telegram.Bot.Types.User user,
        CancellationToken cancellationToken = default
    ) =>
        _hybridCache.GetOrCreateAsync(
            $"nsfw:{user.Id}",
            async ct =>
            {
                string? userRes = null;
                string? chanRes = null;

                try
                {
                    var userChat = await _bot.GetChat(user.Id, cancellationToken: ct);
                    var photo = Array.Empty<byte>();
                    var chanPhoto = Array.Empty<byte>();

                    if (userChat.Photo != null)
                    {
                        using var ms = new MemoryStream();
                        await _bot.GetInfoAndDownloadFile(userChat.Photo.BigFileId, ms, cancellationToken: ct);
                        ms.Seek(0, SeekOrigin.Begin);
                        var bytes = ImageSharpConverter.ConvertToNormalizedBgrFloatChannels(ms);
                        lock (_lock)
                        {
                            var results = _nsfwClassifier.Forward(bytes);
                            _logger.LogDebug("Results matrix: {@Matrix}", results);
                            var prob = Matrice.Max(results, out int index);
                            userRes = OpenNsfwClassifier.Labels[index];
                        }
                    }
                    if (userChat.LinkedChatId != null)
                    {
                        var linkedChat = await _bot.GetChat(userChat.LinkedChatId, cancellationToken: ct);
                        if (linkedChat.Photo != null)
                        {
                            using var ms = new MemoryStream();
                            await _bot.GetInfoAndDownloadFile(linkedChat.Photo.BigFileId, ms, cancellationToken: ct);
                            ms.Seek(0, SeekOrigin.Begin);
                            var bytes = ImageSharpConverter.ConvertToNormalizedBgrFloatChannels(ms);
                            lock (_lock)
                            {
                                var results = _nsfwClassifier.Forward(bytes);
                                _logger.LogDebug("Results matrix: {@Matrix}", results);
                                var prob = Matrice.Max(results, out int index);
                                chanRes = OpenNsfwClassifier.Labels[index];
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "GetPicturesNsfwRating");
                }
                return new UserAndChannelNsfw(userRes, chanRes);
            },
            cancellationToken: cancellationToken
        );
}
