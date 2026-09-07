using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Caching.Hybrid;
using SixLabors.ImageSharp;

namespace ClubDoorman;

internal sealed record TelegramInvitePreview(string Hash, TelegramInviteContent? Content);

internal sealed class TelegramInviteContent
{
    public TelegramInviteContent(string title, string description, byte[]? photo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;
        Description = description;
        Photo = photo;
        PhotoHash = photo == null ? null : Convert.ToHexString(SHA256.HashData(photo));
        PhotoMimeType = photo == null ? null : Image.DetectFormat(photo).DefaultMimeType;
        // JSON preserves field boundaries; neither invite tokens nor CDN URLs identify the advertised group.
        Fingerprint = ShaHelper.ComputeSha256Hex(JsonSerializer.Serialize(new[] { title, description, PhotoHash }));
    }

    public string Title { get; }
    public string Description { get; }
    public byte[]? Photo { get; }
    public string? PhotoHash { get; }
    public string? PhotoMimeType { get; }
    public string Fingerprint { get; }
}

internal sealed partial class TelegramInvitePreviews(HttpClient http, HybridCache cache, ILogger<TelegramInvitePreviews> logger)
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        LocalCacheExpiration = TimeSpan.FromHours(1),
        Flags = HybridCacheEntryFlags.DisableDistributedCache,
    };

    // Invite hashes are case-sensitive. A numeric +path is a phone link, not a group invite.
    [GeneratedRegex(
        @"(?<![\w./@-])(?:(?i:(?:https?://)?(?:t\.me|telegram\.me|telegram\.dog)/)(?:(?i:joinchat)/(?<hash>[a-zA-Z0-9_-]+)|\+(?<phoneOrHash>[a-zA-Z0-9_-]+))|(?i:tg://join\?invite=)(?<hash>[a-zA-Z0-9_-]+))",
        RegexOptions.CultureInvariant
    )]
    private static partial Regex InviteLinks();

    public static string[] ExtractHashes(string? bio) =>
        InviteLinks()
            .Matches(bio ?? "")
            .Where(match => match.Groups["hash"].Success || !match.Groups["phoneOrHash"].Value.All(char.IsAsciiDigit))
            .Select(match => match.Groups["hash"].Success ? match.Groups["hash"].Value : match.Groups["phoneOrHash"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public async Task<IReadOnlyList<TelegramInvitePreview>> GetFromBio(string? bio, CancellationToken ct = default)
    {
        var previews = new List<TelegramInvitePreview>();
        foreach (var hash in ExtractHashes(bio))
        {
            TelegramInviteContent? content = null;
            try
            {
                content = await cache.GetOrCreateAsync<TelegramInviteContent?>(
                    $"invite-preview:{hash}",
                    token => Fetch(hash, token),
                    CacheOptions,
                    cancellationToken: ct
                );
            }
            catch (HttpRequestException e)
            {
                logger.LogInformation(e, "Unable to fetch Telegram invite preview {Hash}", hash);
            }
            catch (OperationCanceledException e) when (!ct.IsCancellationRequested)
            {
                logger.LogInformation(e, "Telegram invite preview timed out {Hash}", hash);
            }
            // Failed HTTP requests are not cached; exact-invite detection still works on this message.
            previews.Add(new TelegramInvitePreview(hash, content));
        }
        return previews;
    }

    private async ValueTask<TelegramInviteContent?> Fetch(string hash, CancellationToken ct)
    {
        var html = await http.GetStringAsync($"https://t.me/+{hash}", ct);
        using var document = new HtmlParser().ParseDocument(html);
        var title = document.QuerySelector(".tgme_page_title")?.TextContent.Trim();
        // Telegram returns HTTP 200 and a generic invitation page for an invalid/unavailable invite.
        if (title == null)
        {
            logger.LogInformation("Telegram invite has no public preview {Hash}", hash);
            return null;
        }
        var descriptionNode = document.QuerySelector(".tgme_page_description");
        if (descriptionNode != null)
        {
            foreach (var br in descriptionNode.QuerySelectorAll("br"))
                br.Replace(document.CreateTextNode("\n"));
        }
        var description = descriptionNode?.TextContent.Trim() ?? "";
        var photoUrl = document.QuerySelector(".tgme_page_photo_image")?.GetAttribute("src");
        byte[]? photo = null;
        if (photoUrl != null)
        {
            var uri = new Uri(photoUrl, UriKind.Absolute);
            // Fetch only Telegram-hosted images from the page, never arbitrary external URLs or redirects.
            if (
                uri.Scheme != "https"
                || !uri.IsDefaultPort
                || (uri.Host != "t.me" && !uri.Host.EndsWith(".telesco.pe", StringComparison.Ordinal))
            )
                throw new InvalidDataException($"Unexpected Telegram invite photo URL: {photoUrl}");
            photo = await http.GetByteArrayAsync(uri, ct);
        }
        return new TelegramInviteContent(title, description, photo);
    }
}
