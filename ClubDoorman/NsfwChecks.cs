using Microsoft.Extensions.Caching.Hybrid;
using NsfwSpyNS;
using Telegram.Bot;

namespace ClubDoorman;

internal sealed record UserAndChannelNsfw(NsfwSpyResult? User, NsfwSpyResult? Channel);

internal class NsfwChecks(ITelegramBotClient bot, INsfwSpy nsfwSpy, HybridCache hybridCache, ILogger<NsfwChecks> logger)
{
    private readonly ITelegramBotClient _bot = bot;
    private readonly INsfwSpy _nsfwSpy = nsfwSpy;
    private readonly HybridCache _hybridCache = hybridCache;
    private readonly ILogger<NsfwChecks> _logger = logger;
    private readonly Lock _lock = new();

    public ValueTask<UserAndChannelNsfw> GetPicturesNsfwRating(
        Telegram.Bot.Types.User user,
        CancellationToken cancellationToken = default
    ) =>
        _hybridCache.GetOrCreateAsync(
            $"nsfw:{user.Id}",
            async ct =>
            {
                NsfwSpyResult? userRes = null;
                NsfwSpyResult? chanRes = null;

                try
                {
                    var userChat = await _bot.GetChat(user.Id, cancellationToken: ct);
                    var photo = Array.Empty<byte>();
                    var chanPhoto = Array.Empty<byte>();

                    if (userChat.Photo != null)
                    {
                        using var ms = new MemoryStream();
                        await _bot.GetInfoAndDownloadFile(userChat.Photo.BigFileId, ms, cancellationToken: ct);
                        photo = ms.ToArray();
                    }
                    if (userChat.LinkedChatId != null)
                    {
                        var linkedChat = await _bot.GetChat(userChat.LinkedChatId, cancellationToken: ct);
                        if (linkedChat.Photo != null)
                        {
                            using var ms = new MemoryStream();
                            await _bot.GetInfoAndDownloadFile(linkedChat.Photo.BigFileId, ms, cancellationToken: ct);
                            chanPhoto = ms.ToArray();
                        }
                    }

                    if (photo.Length > 0)
                        lock (_lock)
                            userRes = _nsfwSpy.ClassifyImage(photo);
                    if (chanPhoto.Length > 0)
                        lock (_lock)
                            chanRes = _nsfwSpy.ClassifyImage(chanPhoto);
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
