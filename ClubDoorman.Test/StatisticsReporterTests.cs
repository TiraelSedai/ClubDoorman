using System.Collections.Frozen;

namespace ClubDoorman.Test;

public class StatisticsReporterTests
{
    [Test]
    public void BuildStatisticsReportMessages_RoutesConfiguredMappedAdminChatToFallbackAdminChat()
    {
        var report = new Dictionary<long, Stats>
        {
            [-10] = new("Mapped Chat") { Id = -10, Autoban = 1 },
        };
        var adminChatMap = new Dictionary<long, long> { [-10] = -100111 }.ToFrozenDictionary();
        var fallbackAdminChats = Config.ParseChatIdSet("-100111");

        var messages = StatisticsReporter.BuildStatisticsReportMessages(report, adminChatMap, fallbackAdminChats, -999);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messages, Has.Count.EqualTo(1));
            Assert.That(messages[0].AdminChatId, Is.EqualTo(-999));
            Assert.That(messages[0].Text, Does.Contain("Mapped Chat"));
        }
    }

    [Test]
    public void BuildStatisticsReportMessages_KeepsUnconfiguredMappedAdminChatAssigned()
    {
        var report = new Dictionary<long, Stats>
        {
            [-10] = new("Mapped Chat") { Id = -10, Autoban = 1 },
        };
        var adminChatMap = new Dictionary<long, long> { [-10] = -100111 }.ToFrozenDictionary();

        var messages = StatisticsReporter.BuildStatisticsReportMessages(report, adminChatMap, FrozenSet<long>.Empty, -999);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messages, Has.Count.EqualTo(1));
            Assert.That(messages[0].AdminChatId, Is.EqualTo(-100111));
            Assert.That(messages[0].Text, Does.Contain("Mapped Chat"));
        }
    }

    [Test]
    public void BuildStatisticsReportMessages_SendsFallbackRoutedAndUnmappedChatsAsSeparateDefaultReports()
    {
        var report = new Dictionary<long, Stats>
        {
            [-10] = new("Mapped Chat") { Id = -10, Autoban = 1 },
            [-20] = new("Free Chat") { Id = -20, Channels = 1 },
        };
        var adminChatMap = new Dictionary<long, long> { [-10] = -100111 }.ToFrozenDictionary();
        var fallbackAdminChats = Config.ParseChatIdSet("-100111");

        var messages = StatisticsReporter.BuildStatisticsReportMessages(report, adminChatMap, fallbackAdminChats, -999);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(messages, Has.Count.EqualTo(2));
            Assert.That(messages.Select(x => x.AdminChatId), Is.All.EqualTo(-999));
            Assert.That(messages.Single(x => x.Text.Contains("Mapped Chat")).Text, Does.StartWith("За последние 24 часа"));
            Assert.That(messages.Single(x => x.Text.Contains("Free Chat")).Text, Does.StartWith("В фри чатах"));
        }
    }
}
