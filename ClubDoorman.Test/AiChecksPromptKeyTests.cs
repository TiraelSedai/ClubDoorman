namespace ClubDoorman.Test;

/// <summary>
/// The cache key must describe the whole input: change anything that reaches the model, get a different key.
/// Pure render functions only, no bot and no LLM.
/// </summary>
public class AiChecksPromptKeyTests
{
    private const string Model = "test/model";

    private static AiChecks.ProfileInputs Profile(
        long userId = 42,
        string fullName = "Настя Петрова",
        string? username = "nastya",
        string? bio = "Пишу про еду и путешествия",
        string? photoUniqueId = "avatar-unique",
        string? photoBigFileId = "avatar-big",
        AiChecks.PromptSection? linkedChannel = null,
        IReadOnlyList<AiChecks.PromptSection>? mentionedChannels = null
    ) => new(userId, fullName, username, bio, photoUniqueId, photoBigFileId, linkedChannel, mentionedChannels ?? []);

    [Test]
    public void ProfileKey_DiffersOnUserId()
    {
        // two burner accounts with the same bare name render the same prompt, they still must not share a verdict
        var one = Profile(userId: 1, username: null, bio: null, photoUniqueId: null, photoBigFileId: null);
        var two = Profile(userId: 2, username: null, bio: null, photoUniqueId: null, photoBigFileId: null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(AiChecks.RenderProfilePrompt(one).Key, Is.Not.EqualTo(AiChecks.RenderProfilePrompt(two).Key));
            Assert.That(AiChecks.RenderProfilePrompt(one).Sections[0].Text, Is.EqualTo(AiChecks.RenderProfilePrompt(two).Sections[0].Text));
        }
    }

    private static AiChecks.PromptSection Channel(string text = "Информация о привязанном канале:\nНазвание: Про еду") =>
        new(text, "channel-photo-unique", "channel-photo-big");

    [Test]
    public void ProfileKey_DiffersOnBio()
    {
        var a = AiChecks.RenderProfilePrompt(Profile(bio: "Пишу про еду"));
        var b = AiChecks.RenderProfilePrompt(Profile(bio: "Пишу про еду, жду в лс"));

        Assert.That(a.Key, Is.Not.EqualTo(b.Key));
    }

    [Test]
    public void ProfileKey_DiffersOnFullName()
    {
        var a = AiChecks.RenderProfilePrompt(Profile(fullName: "Настя Петрова"));
        var b = AiChecks.RenderProfilePrompt(Profile(fullName: "Настя 🍑 жду в лс"));

        Assert.That(a.Key, Is.Not.EqualTo(b.Key));
    }

    [Test]
    public void ProfileKey_DiffersOnUsername()
    {
        var a = AiChecks.RenderProfilePrompt(Profile(username: "nastya"));
        var b = AiChecks.RenderProfilePrompt(Profile(username: "speed_marketing"));

        Assert.That(a.Key, Is.Not.EqualTo(b.Key));
    }

    [Test]
    public void ProfileKey_DiffersOnLinkedChannelSection()
    {
        var a = AiChecks.RenderProfilePrompt(Profile(linkedChannel: Channel("Информация о привязанном канале:\nНазвание: Про еду")));
        var b = AiChecks.RenderProfilePrompt(Profile(linkedChannel: Channel("Информация о привязанном канале:\nНазвание: Казино 24/7")));

        Assert.That(a.Key, Is.Not.EqualTo(b.Key));
    }

    [Test]
    public void ProfileKey_DiffersOnMentionedChannelSection()
    {
        var a = AiChecks.RenderProfilePrompt(Profile(mentionedChannels: [Channel("Информация об упомянутом канале:\nНазвание: Про еду")]));
        var b = AiChecks.RenderProfilePrompt(
            Profile(mentionedChannels: [Channel("Информация об упомянутом канале:\nНазвание: Казино 24/7")])
        );

        Assert.That(a.Key, Is.Not.EqualTo(b.Key));
    }

    [Test]
    public void ProfileKey_DiffersOnAvatarUniqueId()
    {
        var a = AiChecks.RenderProfilePrompt(Profile(photoUniqueId: "avatar-one"));
        var b = AiChecks.RenderProfilePrompt(Profile(photoUniqueId: "avatar-two"));

        Assert.That(a.Key, Is.Not.EqualTo(b.Key));
    }

    [Test]
    public void ProfileKey_IsStableForEqualInputs()
    {
        var a = AiChecks.RenderProfilePrompt(Profile(linkedChannel: Channel(), mentionedChannels: [Channel("Упомянутый")]));
        var b = AiChecks.RenderProfilePrompt(Profile(linkedChannel: Channel(), mentionedChannels: [Channel("Упомянутый")]));

        Assert.That(a.Key, Is.EqualTo(b.Key));
    }

    [Test]
    public void EroticOnlyPrompt_ContainsNameAndUsername()
    {
        var prompt = AiChecks.RenderProfilePrompt(Profile(fullName: "Настя 🍑", username: "nastya_hot", bio: null));

        var text = string.Join('\n', prompt.Sections.Select(x => x.Text));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(prompt.EroticOnly, Is.True);
            Assert.That(text, Does.Contain("Настя 🍑"));
            Assert.That(text, Does.Contain("@nastya_hot"));
        }
    }

    [Test]
    public void ProfileWithoutBioAndChannel_IsShortPromptWithItsOwnKey()
    {
        var shortPrompt = AiChecks.RenderProfilePrompt(Profile(bio: null));
        var withBio = AiChecks.RenderProfilePrompt(Profile(bio: "Пишу про еду"));
        var withChannel = AiChecks.RenderProfilePrompt(Profile(bio: null, linkedChannel: Channel()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(shortPrompt.EroticOnly, Is.True);
            Assert.That(shortPrompt.SystemMessage, Is.Null);
            Assert.That(withBio.EroticOnly, Is.False);
            Assert.That(withBio.SystemMessage, Is.Not.Null);
            Assert.That(withChannel.EroticOnly, Is.False);
            Assert.That(withChannel.SystemMessage, Is.Not.Null);
            Assert.That(shortPrompt.Key, Is.Not.EqualTo(withBio.Key));
            Assert.That(shortPrompt.Key, Is.Not.EqualTo(withChannel.Key));
            Assert.That(withBio.Key, Is.Not.EqualTo(withChannel.Key));
        }
    }

    [Test]
    public void SpamKey_DiffersOnChatInfo()
    {
        var a = AiChecks.BuildSpamPrompt("Привет всем", "Чат: Про еду", null, null, false, null, Model);
        var b = AiChecks.BuildSpamPrompt("Привет всем", "Чат: Трейдинг", null, null, false, null, Model);

        Assert.That(a.Key, Is.Not.EqualTo(b.Key));
    }

    [Test]
    public void SpamKey_DiffersOnPhoto()
    {
        var a = AiChecks.BuildSpamPrompt("Смотрите", null, null, null, false, "photo-one", Model);
        var b = AiChecks.BuildSpamPrompt("Смотрите", null, null, null, false, "photo-two", Model);

        Assert.That(a.Key, Is.Not.EqualTo(b.Key));
    }

    [Test]
    public void SpamKey_DiffersOnModel()
    {
        var a = AiChecks.BuildSpamPrompt("Привет всем", "Чат: Про еду", null, null, false, "photo", Model);
        var b = AiChecks.BuildSpamPrompt("Привет всем", "Чат: Про еду", null, null, false, "photo", "openrouter/free");

        Assert.That(a.Key, Is.Not.EqualTo(b.Key));
    }

    [Test]
    public void SpamKey_IsStableForEqualInputs()
    {
        var a = AiChecks.BuildSpamPrompt("Привет всем", "Чат: Про еду", "Канал", "Исходное сообщение", true, "photo", Model);
        var b = AiChecks.BuildSpamPrompt("Привет всем", "Чат: Про еду", "Канал", "Исходное сообщение", true, "photo", Model);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(a.Key, Is.EqualTo(b.Key));
            Assert.That(a.Text, Does.Contain("Привет всем"));
        }
    }
}
