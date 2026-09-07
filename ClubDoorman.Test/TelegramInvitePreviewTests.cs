using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Color = SixLabors.ImageSharp.Color;

namespace ClubDoorman.Test;

public class TelegramInvitePreviewTests
{
    private const string First = "https://t.me/+FirstInvite";
    private const string Second = "https://t.me/+SecondInvite";
    private const string PhotoUrl = "https://cdn4.telesco.pe/file/group.jpg";

    [TestCase(-1001804114352)]
    [TestCase(-1001878451986)]
    public async Task DifferentInvites_ExpandIntoActualModelMessagesAndWarnForSharedGroup(long secondChat)
    {
        using var fixture = new PreviewFixture();
        fixture.Page(First, Html());
        fixture.Page(Second, Html().Replace(PhotoUrl, PhotoUrl + "?other-cdn-token").Replace("41 subscribers", "987 subscribers"));
        fixture.Image(PhotoUrl);
        fixture.Image(PhotoUrl + "?other-cdn-token");
        var tracker = new BioInviteTracker(TimeProvider.System);

        var first = await fixture.Previews.GetFromBio(First);
        var second = await fixture.Previews.GetFromBio(Second);
        var initial = tracker.Observe(Post(1), First, first);
        var match = tracker.Observe(Post(2, secondChat), Second, second);
        var inputs = await fixture.Collector.Collect(User(), UserChat($"{First} {Second}"));
        var prompt = AiChecks.RenderProfilePrompt(inputs);
        var (messages, avatar) = await AiChecks.BuildProfileMessages(prompt, fixture.Bot);
        var imageUrls = messages
            .Where(message => message.IsUser && message.User!.Content.IsValue2)
            .SelectMany(message => message.User!.Content.Value2!)
            .Where(part => part.IsImageContentPart)
            .Select(part => part.ImageContentPart!.ImageUrl.Url)
            .ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(initial.IsShared, Is.False);
            Assert.That(match.IsShared, Is.True);
            Assert.That(match.Warnings, Has.Count.EqualTo(2));
            Assert.That(match.Warnings.All(warning => warning.Reason.Contains("Вероятно", StringComparison.Ordinal)), Is.True);
            Assert.That(
                match.Warnings.All(warning =>
                    warning.Reason.Contains(First, StringComparison.Ordinal) && warning.Reason.Contains(Second, StringComparison.Ordinal)
                ),
                Is.True
            );
            Assert.That(inputs.MentionedChannels, Has.Count.EqualTo(2));
            Assert.That(prompt.Sections[1].Text, Does.Contain("6 Figs soon…").And.Contain("Иногда любим MEXC"));
            Assert.That(prompt.Sections.Select(section => section.Text), Has.None.Contains("subscribers"));
            Assert.That(prompt.Sections.Select(section => section.Text), Has.None.Contains("987"));
            Assert.That(
                imageUrls,
                Is.EqualTo(Enumerable.Repeat("data:image/png;base64," + Convert.ToBase64String(PreviewFixture.Picture()), 2))
            );
            Assert.That(avatar, Is.Empty, "the group's image must not become the user's avatar in the moderation report");
            Assert.That(fixture.Requests, Has.Count.EqualTo(4), "collector and moderation share the real HybridCache");
        }
    }

    [Test]
    public async Task SubscriberCountAndCdnUrl_DoNotChangeFingerprintOrPromptCacheKey()
    {
        using var one = new PreviewFixture();
        using var two = new PreviewFixture();
        one.Page(First, Html());
        two.Page(First, Html().Replace("41 subscribers", "123456 subscribers").Replace(PhotoUrl, PhotoUrl + "?rotated"));
        one.Image(PhotoUrl);
        two.Image(PhotoUrl + "?rotated");

        var a = await one.Previews.GetFromBio(First);
        var b = await two.Previews.GetFromBio(First);
        var promptA = AiChecks.RenderProfilePrompt(await one.Collector.Collect(User(), UserChat(First)));
        var promptB = AiChecks.RenderProfilePrompt(await two.Collector.Collect(User(), UserChat(First)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(a[0].Content!.Fingerprint, Is.EqualTo(b[0].Content!.Fingerprint));
            Assert.That(a[0].Content!.PhotoHash, Is.Not.Null);
            Assert.That(promptA.Key, Is.EqualTo(promptB.Key));
            Assert.That(promptA.Sections[1].Text, Is.EqualTo(promptB.Sections[1].Text));
            Assert.That(promptB.Sections[1].Text, Does.Not.Contain("123456"));
        }
    }

    [TestCase("title")]
    [TestCase("description")]
    [TestCase("photo")]
    public async Task EachContentField_ChangesFingerprintAndPromptKey(string changedField)
    {
        using var one = new PreviewFixture();
        using var two = new PreviewFixture();
        one.Page(First, Html());
        var changedHtml = changedField switch
        {
            "title" => Html().Replace("6 Figs soon…", "Another group"),
            "description" => Html().Replace("Иногда любим MEXC", "Другое описание"),
            _ => Html(),
        };
        two.Page(First, changedHtml);
        two.Page(Second, changedHtml);
        one.Image(PhotoUrl);
        two.Image(PhotoUrl, changedField == "photo");

        var original = await one.Previews.GetFromBio(First);
        var changed = await two.Previews.GetFromBio(Second);
        var tracker = new BioInviteTracker(TimeProvider.System);
        tracker.Observe(Post(1), First, original);
        var unrelated = tracker.Observe(Post(2), Second, changed);
        var originalPrompt = AiChecks.RenderProfilePrompt(await one.Collector.Collect(User(), UserChat(First)));
        var changedPrompt = AiChecks.RenderProfilePrompt(await two.Collector.Collect(User(), UserChat(First)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(original[0].Content!.Fingerprint, Is.Not.EqualTo(changed[0].Content!.Fingerprint));
            Assert.That(unrelated.IsShared, Is.False);
            Assert.That(unrelated.Warnings, Is.Empty);
            Assert.That(originalPrompt.Key, Is.Not.EqualTo(changedPrompt.Key), "the bio and invite token stayed the same");
        }
    }

    [Test]
    public async Task GenericUnavailablePages_DoNotBecomeGroupContextOrCollide_ButExactInvitesStillWork()
    {
        using var fixture = new PreviewFixture();
        var unavailable = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures/telegram-invite-invalid.html"));
        fixture.Page(First, unavailable);
        fixture.Page(Second, unavailable);
        var first = await fixture.Previews.GetFromBio(First);
        var second = await fixture.Previews.GetFromBio(Second);
        var tracker = new BioInviteTracker(TimeProvider.System);
        tracker.Observe(Post(1), First, first);
        var unrelated = tracker.Observe(Post(2), Second, second);
        var exact = tracker.Observe(Post(3), First, first);
        var inputs = await fixture.Collector.Collect(User(), UserChat(First));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first[0].Content, Is.Null);
            Assert.That(second[0].Content, Is.Null);
            Assert.That(unrelated.IsShared, Is.False);
            Assert.That(exact.Warnings, Has.Count.EqualTo(2));
            Assert.That(inputs.MentionedChannels, Is.Empty);
            Assert.That(fixture.Requests, Has.Count.EqualTo(2), "unavailable pages have no photo to download");
        }
    }

    [Test]
    public async Task MissingPhotoAndHtmlEntities_AreRepresentedWithoutTelegramLogoOrSubscriberCount()
    {
        using var fixture = new PreviewFixture();
        fixture.Page(
            First,
            """
            <meta property="og:image" content="https://telegram.org/img/logo.png">
            <div class="tgme_page_title"><span>A &amp; B</span></div>
            <div class="tgme_page_extra">999 members</div>
            <div class="tgme_page_description">First &lt;line&gt;<br>Second &quot;line&quot;</div>
            """
        );

        var previews = await fixture.Previews.GetFromBio(First);
        var inputs = await fixture.Collector.Collect(User(), UserChat(First));
        var prompt = AiChecks.RenderProfilePrompt(inputs);
        var (messages, _) = await AiChecks.BuildProfileMessages(prompt, fixture.Bot);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews[0].Content!.Title, Is.EqualTo("A & B"));
            Assert.That(previews[0].Content!.Description, Is.EqualTo("First <line>\nSecond \"line\""));
            Assert.That(previews[0].Content!.Photo, Is.Null);
            Assert.That(prompt.Sections[1].Text, Does.Not.Contain("999"));
            Assert.That(JsonSerializer.Serialize(messages), Does.Not.Contain("image_url"));
            Assert.That(fixture.Requests, Is.EqualTo(new[] { First }));
        }
    }

    [Test]
    public async Task PhotoDownloadFailure_DoesNotCreatePartialFingerprintOrPoisonCache()
    {
        using var fixture = new PreviewFixture();
        fixture.Page(First, Html());
        fixture.Responses[PhotoUrl] = () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        var unavailable = await fixture.Previews.GetFromBio(First);
        fixture.Image(PhotoUrl);
        var recovered = await fixture.Previews.GetFromBio(First);
        var tracker = new BioInviteTracker(TimeProvider.System);
        tracker.Observe(Post(1), First, unavailable);
        var exact = tracker.Observe(Post(2), First, recovered);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unavailable[0].Content, Is.Null);
            Assert.That(recovered[0].Content!.Photo, Is.EqualTo(PreviewFixture.Picture()));
            Assert.That(exact.Warnings, Has.Count.EqualTo(2));
            Assert.That(fixture.Requests, Has.Count.EqualTo(4));
        }
    }

    [Test]
    public async Task InvalidPhotoBytes_FailVisiblyAndAreNotCached()
    {
        using var fixture = new PreviewFixture();
        fixture.Page(First, Html());
        fixture.Responses[PhotoUrl] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not an image") };
        Assert.That(async () => await fixture.Previews.GetFromBio(First), Throws.TypeOf<UnknownImageFormatException>());
        fixture.Image(PhotoUrl);
        var recovered = await fixture.Previews.GetFromBio(First);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recovered[0].Content!.Photo, Is.EqualTo(PreviewFixture.Picture()));
            Assert.That(fixture.Requests, Has.Count.EqualTo(4));
        }
    }

    [Test]
    public void ExternalPhotoUrl_IsRejectedBeforeItIsFetched()
    {
        using var fixture = new PreviewFixture();
        fixture.Page(First, Html().Replace(PhotoUrl, "https://example.org/group.jpg"));

        Assert.That(
            async () => await fixture.Previews.GetFromBio(First),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("https://example.org/group.jpg")
        );
        Assert.That(fixture.Requests, Is.EqualTo(new[] { First }));
    }

    [Test]
    public async Task ContentAndExactMatches_ShareWarningSuppression()
    {
        using var fixture = new PreviewFixture();
        fixture.Page(First, Html());
        fixture.Page(Second, Html());
        fixture.Image(PhotoUrl);
        var first = await fixture.Previews.GetFromBio(First);
        var second = await fixture.Previews.GetFromBio(Second);
        var tracker = new BioInviteTracker(TimeProvider.System);
        tracker.Observe(Post(1), First, first);
        var contentMatch = tracker.Observe(Post(2), Second, second);
        var exactMatch = tracker.Observe(Post(3), First, first);
        var repeat = tracker.Observe(Post(2), Second, second);
        var rotatedInvite = tracker.Observe(Post(2), First, first);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(contentMatch.Warnings, Has.Count.EqualTo(2));
            Assert.That(exactMatch.Warnings, Has.Count.EqualTo(1));
            Assert.That(exactMatch.Warnings[0].Message.From!.Id, Is.EqualTo(3));
            Assert.That(repeat.IsShared, Is.True);
            Assert.That(repeat.Warnings, Is.Empty);
            Assert.That(rotatedInvite.IsShared, Is.True);
            Assert.That(rotatedInvite.Warnings, Is.Empty);
        }
    }

    [Test]
    public async Task LegacyInvite_IsResolvedOnceAndIsNotLookedUpAsPublicUsername()
    {
        using var fixture = new PreviewFixture();
        fixture.Page(First, Html());
        fixture.Image(PhotoUrl);
        var inputs = await fixture.Collector.Collect(User(), UserChat($"https://t.me/joinchat/FirstInvite {First}"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(inputs.MentionedChannels, Has.Count.EqualTo(1));
            Assert.That(inputs.MentionedChannels[0].Text, Does.Contain("6 Figs soon…"));
            Assert.That(fixture.Requests, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public void Cancellation_IsPropagated()
    {
        using var fixture = new PreviewFixture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(
            async () => await fixture.Previews.GetFromBio(First, cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>()
        );
        Assert.That(fixture.Requests, Is.Empty);
    }

    private static string Html() =>
        File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures/telegram-invite-valid.html"));

    private static User User() => new() { Id = 42, FirstName = "User" };

    private static ChatFullInfo UserChat(string bio) =>
        new()
        {
            Id = 42,
            Type = ChatType.Private,
            FirstName = "User",
            Bio = bio,
        };

    private static Message Post(long id, long chatId = -1001804114352) =>
        new()
        {
            Id = checked((int)id),
            Chat = new Chat
            {
                Id = chatId,
                Type = ChatType.Supergroup,
                Title = "Chat",
            },
            From = new User { Id = id, FirstName = $"User{id}" },
            Text = $"Message from user {id}",
        };

    private sealed class PreviewFixture : IDisposable
    {
        private readonly ServiceProvider _services;
        private readonly HttpClient _http;
        public Dictionary<string, Func<HttpResponseMessage>> Responses { get; } = [];
        public List<string> Requests { get; } = [];
        public TelegramInvitePreviews Previews { get; }
        public ProfileInputCollector Collector { get; }
        public ITelegramBotClient Bot { get; }

        public PreviewFixture()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHybridCache();
            _services = services.BuildServiceProvider();
            _http = new HttpClient(new Handler(this));
            Previews = new TelegramInvitePreviews(
                _http,
                _services.GetRequiredService<HybridCache>(),
                NullLogger<TelegramInvitePreviews>.Instance
            );
            Bot = new TelegramBotClient("123456789:ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghi", _http);
            Collector = new ProfileInputCollector(Bot, NullLogger<AiChecks>.Instance, Previews);
        }

        public void Page(string url, string html) =>
            Responses[url] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html) };

        public void Image(string url, bool different = false) =>
            Responses[url] = () => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Picture(different)) };

        public static byte[] Picture(bool different = false)
        {
            using var image = new Image<Rgba32>(2, 2, different ? Color.Blue : Color.Red);
            using var stream = new MemoryStream();
            image.SaveAsPng(stream);
            return stream.ToArray();
        }

        public void Dispose()
        {
            _http.Dispose();
            _services.Dispose();
        }

        private sealed class Handler(PreviewFixture fixture) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var url = request.RequestUri!.AbsoluteUri;
                fixture.Requests.Add(url);
                Assert.That(fixture.Responses.ContainsKey(url), Is.True, $"Unexpected HTTP request: {url}");
                return Task.FromResult(fixture.Responses[url]());
            }
        }
    }
}
