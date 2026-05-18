namespace ClubDoorman.Test;

public class ConfigTests
{
    [Test]
    public void ParseChatIdSet_ParsesCommaSeparatedChatIdsAndIgnoresInvalidEntries()
    {
        var chats = Config.ParseChatIdSet("-100111, invalid, -100222, ,42");

        Assert.That(chats, Is.EquivalentTo(new[] { -100111L, -100222L, 42L }));
    }

    [Test]
    public void ParseChatIdSet_ReturnsEmptySetForMissingValue()
    {
        var chats = Config.ParseChatIdSet(null);

        Assert.That(chats, Is.Empty);
    }
}
