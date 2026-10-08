using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.ML;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using tryAGI.OpenAI;
using Message = Telegram.Bot.Types.Message;
using User = Telegram.Bot.Types.User;

namespace ClubDoorman.Test;

[NonParallelizable]
public sealed class MessageProcessorMlOverrideTests
{
    private const long PaidChat = -1001234567890;
    private const long FreeChat = -1009876543210;
    private const long AdminChat = -1001111111111;

    [TestCase(PaidChat, "Hi")]
    [TestCase(FreeChat, "Hi")]
    [TestCase(PaidChat, "привет")]
    [TestCase(FreeChat, "привет")]
    public async Task GreetingUpdate_DeletesAndRestricts_WithStandardReporting(long chatId, string text)
    {
        await using var fixture = await Fixture.Create(score: -1f, llmProbability: 0.0, llmAvailable: true);

        await fixture.CheckTextUpdate(chatId, text);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Requests.Count(x => x.Method == "deleteMessage"), Is.EqualTo(1));
            Assert.That(fixture.Requests.Count(x => x.Method == "restrictChatMember"), Is.EqualTo(1));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("banChatMember"));
            Assert.That(fixture.Requests.Count(x => x.Method == "forwardMessage"), Is.EqualTo(chatId == PaidChat ? 1 : 0));
            Assert.That(fixture.Requests.Count(x => x.Method == "sendMessage"), Is.EqualTo(chatId == PaidChat ? 1 : 0));
        }
        if (chatId == PaidChat)
        {
            using var report = JsonDocument.Parse(fixture.Requests.Single(x => x.Method == "sendMessage").Body);
            Assert.That(report.RootElement.GetProperty("text").GetString(), Does.Contain("в этом сообщении написано привет"));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PollUpdate_UsesSpamChecksInsteadOfEmptyTextReport(bool edited)
    {
        await using var fixture = await Fixture.Create(score: 2f, llmProbability: 0.2, llmAvailable: true);

        await fixture.CheckPoll(edited);

        var reports = fixture
            .Requests.Where(x => x.Method == "sendMessage")
            .Select(x =>
            {
                using var body = JsonDocument.Parse(x.Body);
                return body.RootElement.GetProperty("text").GetString();
            })
            .ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Contain("deleteMessage"));
            Assert.That(reports, Is.Not.Empty);
            Assert.That(reports, Has.None.Contains("пустое сообщение/подпись"));
            Assert.That(fixture.LlmRequests, Has.Count.EqualTo(1));
        }
        using var request = JsonDocument.Parse(fixture.LlmRequests.Single());
        var prompt = request.RootElement.GetProperty("messages").EnumerateArray().Last().GetProperty("content").GetString();
        Assert.That(prompt, Does.Contain("[ опрос ] https://t.me/+example joined?\n- option one\n- option two"));
    }

    [TestCase(0.300001f, 0.0)]
    [TestCase(0.5f, 0.1)]
    public async Task PaidClassifierScoreInStrictOverrideBand_AndAvailableLlmAtOrBelowTenPercent_IsReportedWithoutDeletion(
        float score,
        double llmProbability
    )
    {
        await using var fixture = await Fixture.Create(score, llmProbability, llmAvailable: true);

        var result = await fixture.Check(PaidChat);

        AssertReportOnly(fixture, result);
    }

    [TestCase(0.5f, 0.100001)]
    [TestCase(2f, 0.0)]
    public async Task PaidClassifierScoreOrLlmOutsideStrictOverrideBand_UsesExistingDeletion(float score, double llmProbability)
    {
        await using var fixture = await Fixture.Create(score, llmProbability, llmAvailable: true);

        var result = await fixture.Check(PaidChat);

        AssertDeleted(fixture, result);
    }

    [Test]
    public async Task PaidClassifierScoreAtExistingThreshold_DoesNotEnterOverride()
    {
        await using var fixture = await Fixture.Create(Consts.ClassifierSpamScoreThreshold, 0.0, llmAvailable: true);

        var result = await fixture.Check(PaidChat);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.Pass));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("forwardMessage"));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("sendMessage"));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("deleteMessage"));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("restrictChatMember"));
        }
    }

    [TestCase("Bad Request: message to forward not found")]
    [TestCase("Bad Request: MESSAGE_ID_INVALID")]
    public async Task PaidReportOnly_MessageAlreadyDeletedFromChat_IsNotReported(string forwardError)
    {
        await using var fixture = await Fixture.Create(score: 0.5f, llmProbability: 0.1, llmAvailable: true, forwardError: forwardError);

        var result = await fixture.Check(PaidChat);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.Suspicious));
            Assert.That(fixture.Requests.Count(x => x.Method == "forwardMessage"), Is.EqualTo(1));
            Assert.That(fixture.Requests.Select(x => x.Method), Has.None.StartsWith("send"));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("deleteMessage"));
        }
    }

    [Test]
    public async Task PaidReportOnly_ForwardFailsForOtherReason_SendsFallbackAndReport()
    {
        await using var fixture = await Fixture.Create(
            score: 0.5f,
            llmProbability: 0.1,
            llmAvailable: true,
            forwardError: "Bad Request: message can't be forwarded"
        );

        var result = await fixture.Check(PaidChat);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.Suspicious));
            Assert.That(fixture.Requests.Count(x => x.Method == "sendMessage"), Is.EqualTo(2));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("deleteMessage"));
        }
    }

    [Test]
    public async Task PaidChatUnavailableLlm_DefaultZeroDoesNotLookLikeAvailableLowScore()
    {
        await using var fixture = await Fixture.Create(score: 0.5f, llmProbability: 0.0, llmAvailable: false);

        var result = await fixture.Check(PaidChat);

        AssertDeleted(fixture, result);
    }

    [Test]
    public async Task FreeChatWithClassifierScoreAboveExistingGate_PreservesExistingDeletion()
    {
        await using var fixture = await Fixture.Create(score: 0.5f, llmProbability: 0.0, llmAvailable: true);

        var result = await fixture.Check(FreeChat);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.NoMoreAction));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Contain("deleteMessage"));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Contain("restrictChatMember"));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("forwardMessage"));
        }
    }

    [Test]
    public async Task PaidLowLlm_RemainsReportOnlyOnRepeatedSameMessageCacheHit()
    {
        await using var fixture = await Fixture.Create(score: 0.5f, llmProbability: 0.1, llmAvailable: true);

        var first = await fixture.Check(PaidChat);
        var second = await fixture.Check(PaidChat);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(CheckResult.Suspicious));
            Assert.That(second, Is.EqualTo(CheckResult.Suspicious));
            Assert.That(fixture.LlmRequests, Has.Count.EqualTo(1));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("deleteMessage"));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("restrictChatMember"));
        }
    }

    [TestCase(PaidChat, 0.9, "spam", 0.9, true)]
    [TestCase(FreeChat, 0.9, "spam", 0.9, true)]
    [TestCase(PaidChat, 0.1, "not_spam", 0.9, false)]
    [TestCase(FreeChat, 0.1, "not_spam", 0.9, false)]
    public async Task AmbiguousMl_ConfidentAgreement_LearnsInEveryChat(
        long chatId,
        double lunaProbability,
        string jevLabel,
        double jevConfidence,
        bool expectedSpam
    )
    {
        await using var fixture = await Fixture.Create(0, lunaProbability, true, jevLabel, jevConfidence);

        await fixture.Check(chatId);
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var records = await fixture.Records();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(records.Select(x => (x.Text, x.IsSpam)), Is.EqualTo(new[] { ("ordinary message with enough text", expectedSpam) }));
            Assert.That(fixture.LlmRequests, Has.Count.EqualTo(1), "Paid moderation must reuse Luna");
            Assert.That(fixture.JevRequests, Has.Count.EqualTo(1));
            Assert.That(fixture.Requests.Where(x => x.Method == "sendMessage").Select(RequestChatId), Does.Contain(AdminChat));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task LowConfidenceHam_ConfidentAgreement_DoesNotRequestManualDatasetReview(bool delayJev)
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var fixture = await Fixture.Create(
            -0.3917967f,
            0.01,
            true,
            "not_spam",
            1,
            jevRelease: delayJev ? release.Task : null,
            lowConfidenceHamForward: true
        );

        try
        {
            var result = await fixture.Check(PaidChat).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(result, Is.EqualTo(CheckResult.Pass));
            if (delayJev)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(fixture.ReviewCompleted.Task.IsCompleted, Is.False);
                    Assert.That(fixture.Requests.Where(x => x.Method is "forwardMessage" or "sendMessage"), Is.Empty);
                }
            }
        }
        finally
        {
            release.TrySetResult();
        }

        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That((await fixture.Records()).Single().IsSpam, Is.False);
            Assert.That(fixture.Requests.Count(x => x.Method == "forwardMessage"), Is.EqualTo(1));
            Assert.That(fixture.Requests.Count(x => x.Method == "sendMessage"), Is.EqualTo(1));
            Assert.That(RequestText(fixture.Requests.Single(x => x.Method == "sendMessage")), Does.Not.Contain("конфиденс низкий"));
        }
    }

    [Test]
    public async Task HighConfidenceLlmSpam_ConfidentAgreement_DoesNotRequestManualDatasetReview(
        [Values(0.0042500496f, -0.76131344f)] float score,
        [Values] bool delayJev
    )
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var fixture = await Fixture.Create(
            score,
            0.99,
            true,
            "spam",
            0.95,
            jevRelease: delayJev ? release.Task : null,
            lowConfidenceHamForward: true
        );

        try
        {
            var result = await fixture.Check(PaidChat).WaitAsync(TimeSpan.FromSeconds(5));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(CheckResult.NoMoreAction));
                Assert.That(fixture.Requests.Select(x => x.Method), Does.Contain("deleteMessage"));
                Assert.That(
                    fixture.Requests.Where(x => x.Method == "sendMessage").Select(RequestText),
                    Has.None.Contains("Хорошая идея - добавить сообщение в датасет")
                );
                if (delayJev)
                    Assert.That(fixture.ReviewCompleted.Task.IsCompleted, Is.False);
            }
        }
        finally
        {
            release.TrySetResult();
        }

        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That((await fixture.Records()).Single().IsSpam, Is.True);
            Assert.That(fixture.Requests.Count(x => x.Method == "sendMessage"), Is.EqualTo(2));
            Assert.That(
                fixture.Requests.Where(x => x.Method == "sendMessage").Select(RequestText),
                Has.None.Contains("Хорошая идея - добавить сообщение в датасет")
            );
            Assert.That(fixture.LlmRequests, Has.Count.EqualTo(1));
        }
    }

    [TestCase(0.0042500496f, "not_spam", 0.95)]
    [TestCase(0.0042500496f, null, 0.95)]
    [TestCase(0.0042500496f, "spam", 0.89)]
    [TestCase(-0.76131344f, "not_spam", 0.95)]
    [TestCase(-0.76131344f, null, 0.95)]
    [TestCase(-0.76131344f, "spam", 0.89)]
    public async Task HighConfidenceLlmSpam_WithoutConsensus_RequestsManualDatasetReviewAfterDeletion(
        float score,
        string? jevLabel,
        double jevConfidence
    )
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var fixture = await Fixture.Create(
            score,
            0.99,
            true,
            jevLabel,
            jevConfidence,
            jevRelease: release.Task,
            lowConfidenceHamForward: true,
            failForwardAfterDeletion: true
        );

        try
        {
            var result = await fixture.Check(PaidChat).WaitAsync(TimeSpan.FromSeconds(5));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(CheckResult.NoMoreAction));
                Assert.That(fixture.Requests.Select(x => x.Method), Does.Contain("deleteMessage"));
                Assert.That(fixture.ReviewCompleted.Task.IsCompleted, Is.False);
                Assert.That(
                    fixture.Requests.Where(x => x.Method == "sendMessage").Select(RequestText),
                    Has.None.Contains("Хорошая идея - добавить сообщение в датасет")
                );
            }
        }
        finally
        {
            release.TrySetResult();
        }

        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var reports = fixture.Requests.Where(x => x.Method == "sendMessage").Select(RequestText).ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(await fixture.Records(), Is.Empty);
            Assert.That(reports, Has.Length.EqualTo(3));
            Assert.That(reports, Does.Contain("ordinary message with enough text"));
            Assert.That(reports, Has.Exactly(1).Contains("LLM считает сообщение спамом с высокой уверенностью"));
            Assert.That(reports, Has.None.Contains("конфиденс низкий"));
        }
    }

    [TestCase(-0.5f, true, 2)]
    [TestCase(-1f, true, 2)]
    [TestCase(Consts.ClassifierSpamScoreThreshold, true, 1)]
    [TestCase(0f, false, 1)]
    public async Task HighConfidenceLlmSpam_ManualDatasetReview_PreservesExistingGates(float score, bool enabled, int expectedReports)
    {
        await using var fixture = await Fixture.Create(score, 0.99, true, "not_spam", lowConfidenceHamForward: enabled);

        var result = await fixture.Check(PaidChat);
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.NoMoreAction));
            Assert.That(await fixture.Records(), Is.Empty);
            Assert.That(fixture.Requests.Count(x => x.Method == "sendMessage"), Is.EqualTo(expectedReports));
        }
    }

    [Test]
    public async Task ConfidentHamMl_LunaBelowHighSpamConfidence_DoesNotRequestJev()
    {
        await using var fixture = await Fixture.Create(-1f, 0.899999, true, "spam", 0.95, lowConfidenceHamForward: true);

        var result = await fixture.Check(PaidChat);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.Suspicious));
            Assert.That(fixture.JevRequests, Is.Empty);
            Assert.That(await fixture.Records(), Is.Empty);
        }
    }

    [TestCase(0.01, true, "spam")]
    [TestCase(0.3, true, "not_spam")]
    [TestCase(0.01, true, null)]
    [TestCase(0, false, "not_spam")]
    public async Task LowConfidenceHam_WithoutConsensus_StillRequestsManualDatasetReview(
        double lunaProbability,
        bool lunaAvailable,
        string? jevLabel
    )
    {
        await using var fixture = await Fixture.Create(
            -0.3917967f,
            lunaProbability,
            lunaAvailable,
            jevLabel,
            lowConfidenceHamForward: true
        );

        var result = await fixture.Check(PaidChat);
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.Pass));
            Assert.That(await fixture.Records(), Is.Empty);
            Assert.That(fixture.Requests.Count(x => x.Method == "forwardMessage"), Is.EqualTo(1));
            Assert.That(RequestText(fixture.Requests.Single(x => x.Method == "sendMessage")), Does.Contain("конфиденс низкий"));
        }
    }

    [Test]
    public async Task LowConfidenceHam_LunaReportsSpam_DoesNotRequestHamDatasetReview()
    {
        await using var fixture = await Fixture.Create(-0.3917967f, 0.75, true, "not_spam", lowConfidenceHamForward: true);

        var result = await fixture.Check(PaidChat);
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.Suspicious));
            Assert.That(await fixture.Records(), Is.Empty);
            Assert.That(RequestText(fixture.Requests.Single(x => x.Method == "sendMessage")), Does.Not.Contain("конфиденс низкий"));
        }
    }

    [Test]
    public async Task LowConfidenceHam_ModelsDisabled_StillRequestsManualDatasetReview()
    {
        await using var fixture = await Fixture.Create(-0.3917967f, 0.01, true, lowConfidenceHamForward: true, modelsEnabled: false);

        var result = await fixture.Check(PaidChat);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.Pass));
            Assert.That(await fixture.Records(), Is.Empty);
            Assert.That(fixture.LlmRequests, Is.Empty);
            Assert.That(fixture.JevRequests, Is.Empty);
            Assert.That(fixture.Requests.Count(x => x.Method == "forwardMessage"), Is.EqualTo(1));
            Assert.That(RequestText(fixture.Requests.Single(x => x.Method == "sendMessage")), Does.Contain("конфиденс низкий"));
        }
    }

    [TestCase(-0.5f)]
    [TestCase(0.5f)]
    [TestCase(-1f)]
    [TestCase(1f)]
    public async Task MlOutsideStrictBand_DoesNotRequestDatasetModels(float score)
    {
        await using var fixture = await Fixture.Create(score, 0.9, true, "spam");

        await fixture.Check(FreeChat);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.JevRequests, Is.Empty);
            Assert.That(fixture.LlmRequests, Is.Empty);
            Assert.That(await fixture.Records(), Is.Empty);
        }
    }

    [TestCase(0.899999, "spam", 0.9)]
    [TestCase(0.100001, "not_spam", 0.9)]
    [TestCase(0.9, "spam", 0.899999)]
    [TestCase(0.9, "not_spam", 0.9)]
    [TestCase(0.1, "spam", 0.9)]
    [TestCase(0.0, null, 0.9)]
    public async Task WeakMissingOrDisagreeingVerdict_DoesNotLearn(double lunaProbability, string? jevLabel, double jevConfidence)
    {
        await using var fixture = await Fixture.Create(0, lunaProbability, true, jevLabel, jevConfidence);

        await fixture.Check(FreeChat);
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await fixture.Records(), Is.Empty);
            Assert.That(fixture.Requests.Where(x => x.Method == "sendMessage"), Is.Empty);
        }
    }

    [Test]
    public async Task UnavailableLuna_IsNotConfidentHam()
    {
        await using var fixture = await Fixture.Create(0, 0, false, "not_spam");

        await fixture.Check(FreeChat);
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(await fixture.Records(), Is.Empty);
    }

    [TestCase("{\"Reason\":\"no probability\"}")]
    [TestCase("{\"Probability\":-0.1,\"Reason\":\"invalid probability\"}")]
    [TestCase("{\"Probability\":1.1,\"Reason\":\"invalid probability\"}")]
    public async Task MalformedLunaVerdict_DoesNotPoisonDataset(string lunaContent)
    {
        await using var fixture = await Fixture.Create(0, 0, true, "not_spam", lunaContent: lunaContent);

        await fixture.Check(FreeChat);
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(await fixture.Records(), Is.Empty);
    }

    [TestCase(PaidChat)]
    [TestCase(FreeChat)]
    public async Task ApprovedUser_AmbiguousMlStillLearnsInEveryChat(long chatId)
    {
        await using var fixture = await Fixture.Create(0, 0.1, true, "not_spam");

        await fixture.CheckApproved(chatId);
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                (await fixture.Records()).Select(x => (x.Text, x.IsSpam)),
                Is.EqualTo(new[] { ("trusted message with enough text", false) })
            );
            Assert.That(fixture.LlmRequests, Has.Count.EqualTo(1));
            Assert.That(fixture.JevRequests, Has.Count.EqualTo(1));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("deleteMessage"));
        }
    }

    [TestCase(0.01, "not_spam", false)]
    [TestCase(0.99, "spam", true)]
    public async Task ApprovedUser_ConfidentAgreement_DoesNotRequestManualHamReview(
        double lunaProbability,
        string jevLabel,
        bool expectedSpam
    )
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var fixture = await Fixture.Create(
            0.13794184f,
            lunaProbability,
            true,
            jevLabel,
            modelRelease: release.Task,
            predictedSpam: true
        );

        try
        {
            await fixture.CheckApproved(PaidChat).WaitAsync(TimeSpan.FromSeconds(5));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(fixture.ReviewCompleted.Task.IsCompleted, Is.False);
                Assert.That(fixture.Requests.Where(x => x.Method is "forwardMessage" or "sendMessage" or "deleteMessage"), Is.Empty);
            }
        }
        finally
        {
            release.TrySetResult();
        }
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That((await fixture.Records()).Single().IsSpam, Is.EqualTo(expectedSpam));
            Assert.That(fixture.Requests.Count(x => x.Method == "forwardMessage"), Is.EqualTo(1));
            Assert.That(fixture.Requests.Count(x => x.Method == "sendMessage"), Is.EqualTo(1));
            Assert.That(
                RequestText(fixture.Requests.Single(x => x.Method == "sendMessage")),
                Does.Not.Contain("Возможно стоит добавить в ham")
            );
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("deleteMessage"));
        }
    }

    [TestCase(PaidChat, true, true, "spam", true)]
    [TestCase(PaidChat, true, true, null, true)]
    [TestCase(PaidChat, true, false, "not_spam", true)]
    [TestCase(FreeChat, true, true, "spam", false)]
    [TestCase(PaidChat, false, true, "spam", false)]
    public async Task ApprovedUser_WithoutConsensus_PreservesManualHamReviewGates(
        long chatId,
        bool predictedSpam,
        bool lunaAvailable,
        string? jevLabel,
        bool expectedReport
    )
    {
        await using var fixture = await Fixture.Create(0.13794184f, 0.01, lunaAvailable, jevLabel, predictedSpam: predictedSpam);

        await fixture.CheckApproved(chatId);
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await fixture.Records(), Is.Empty);
            Assert.That(fixture.Requests.Count(x => x.Method == "sendMessage"), Is.EqualTo(expectedReport ? 1 : 0));
            Assert.That(fixture.Requests.Count(x => x.Method == "forwardMessage"), Is.EqualTo(expectedReport ? 1 : 0));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("deleteMessage"));
            if (expectedReport)
                Assert.That(
                    RequestText(fixture.Requests.Single(x => x.Method == "sendMessage")),
                    Does.Contain("Возможно стоит добавить в ham")
                );
        }
    }

    [TestCase(0.5f, true)]
    [TestCase(0.13794184f, false)]
    public async Task ApprovedUser_WithoutDatasetReview_StillRequestsManualHamReview(float score, bool modelsEnabled)
    {
        await using var fixture = await Fixture.Create(score, 0.01, true, "not_spam", modelsEnabled: modelsEnabled, predictedSpam: true);

        await fixture.CheckApproved(PaidChat);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.LlmRequests, Is.Empty);
            Assert.That(fixture.JevRequests, Is.Empty);
            Assert.That(await fixture.Records(), Is.Empty);
            Assert.That(fixture.Requests.Count(x => x.Method == "forwardMessage"), Is.EqualTo(1));
            Assert.That(
                RequestText(fixture.Requests.Single(x => x.Method == "sendMessage")),
                Does.Contain("Возможно стоит добавить в ham")
            );
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("deleteMessage"));
        }
    }

    [Test]
    public async Task ModelsSeeReplyContext_ButDatasetContainsOnlyMessage()
    {
        const string replyText = "context outside the classified message";
        await using var fixture = await Fixture.Create(0, 0.1, true, "not_spam");

        await fixture.Check(FreeChat, replyText);
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(fixture.LlmRequests.Single(), Does.Contain(replyText));
            Assert.That(fixture.JevRequests.Single(), Does.Contain(replyText));
            Assert.That((await fixture.Records()).Single().Text, Is.EqualTo("ordinary message with enough text"));
        }
    }

    [Test]
    public async Task FreeChat_DoesNotWaitForEitherModel()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var fixture = await Fixture.Create(0, 0.1, true, "not_spam", modelRelease: release.Task);
        try
        {
            var result = await fixture.Check(FreeChat).WaitAsync(TimeSpan.FromSeconds(5));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.EqualTo(CheckResult.Pass));
                Assert.That(fixture.ReviewCompleted.Task.IsCompleted, Is.False);
                Assert.That(await fixture.Records(), Is.Empty);
            }
        }
        finally
        {
            release.TrySetResult();
        }
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.That((await fixture.Records()).Single().IsSpam, Is.False);
    }

    [Test]
    public async Task PaidChat_DoesNotWaitForJev_AndReusesCompletedLuna()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var fixture = await Fixture.Create(0.4f, 0.1, true, "not_spam", jevRelease: release.Task);
        try
        {
            var result = await fixture.Check(PaidChat).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.That(result, Is.EqualTo(CheckResult.Suspicious));
            Assert.That(fixture.ReviewCompleted.Task.IsCompleted, Is.False);
        }
        finally
        {
            release.TrySetResult();
        }
        await fixture.ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        using (Assert.EnterMultipleScope())
        {
            Assert.That((await fixture.Records()).Single().IsSpam, Is.False);
            Assert.That(fixture.LlmRequests, Has.Count.EqualTo(1));
        }
    }

    private static void AssertReportOnly(Fixture fixture, CheckResult result)
    {
        var adminRequests = fixture.Requests.Where(x => x.Method is "forwardMessage" or "sendMessage").ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.Suspicious));
            Assert.That(adminRequests.Select(RequestChatId), Has.All.EqualTo(AdminChat));
            Assert.That(adminRequests, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("deleteMessage"));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Not.Contain("restrictChatMember"));
        }
    }

    private static long RequestChatId((string Method, string Body) request)
    {
        using var body = JsonDocument.Parse(request.Body);
        return body.RootElement.GetProperty("chat_id").GetInt64();
    }

    private static string? RequestText((string Method, string Body) request)
    {
        using var body = JsonDocument.Parse(request.Body);
        return body.RootElement.GetProperty("text").GetString();
    }

    private static void AssertDeleted(Fixture fixture, CheckResult result)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CheckResult.NoMoreAction));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Contain("deleteMessage"));
            Assert.That(fixture.Requests.Select(x => x.Method), Does.Contain("restrictChatMember"));
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly Dictionary<string, string?> _previousEnvironment;
        private readonly SqliteConnection _db;
        private readonly ServiceProvider _services;
        private readonly HttpClient _telegramHttp;
        private readonly HttpClient _llmHttp;
        private readonly OpenAiClient _api;
        private readonly HttpClient _jevHttp;
        private bool _reviewStarted;
        private readonly float _score;
        private readonly bool _lunaHighSpam;
        private readonly User _user = new()
        {
            Id = 42,
            FirstName = "Test",
            Username = "test_user",
        };

        private Fixture(
            Dictionary<string, string?> previousEnvironment,
            SqliteConnection db,
            ServiceProvider services,
            HttpClient telegramHttp,
            HttpClient llmHttp,
            OpenAiClient api,
            ConcurrentQueue<(string Method, string Body)> requests,
            ConcurrentQueue<string> llmRequests,
            ConcurrentQueue<string> jevRequests,
            TaskCompletionSource reviewCompleted,
            HttpClient jevHttp,
            float score,
            bool lunaHighSpam
        )
        {
            _previousEnvironment = previousEnvironment;
            _db = db;
            _services = services;
            _telegramHttp = telegramHttp;
            _llmHttp = llmHttp;
            _api = api;
            Requests = requests;
            LlmRequests = llmRequests;
            _score = score;
            _lunaHighSpam = lunaHighSpam;
            _jevHttp = jevHttp;
            JevRequests = jevRequests;
            ReviewCompleted = reviewCompleted;
        }

        public ConcurrentQueue<(string Method, string Body)> Requests { get; }
        public ConcurrentQueue<string> LlmRequests { get; }
        public ConcurrentQueue<string> JevRequests { get; }
        public TaskCompletionSource ReviewCompleted { get; }

        public Task<IReadOnlyList<SpamHamRecord>> Records() =>
            _services.GetRequiredService<SpamHamClassifier>().GetLatestSpamHamRecords(10);

        public static async Task<Fixture> Create(
            float score,
            double llmProbability,
            bool llmAvailable,
            string? jevLabel = null,
            double jevConfidence = 0.9,
            Task? modelRelease = null,
            Task? jevRelease = null,
            string? lunaContent = null,
            bool lowConfidenceHamForward = false,
            bool modelsEnabled = true,
            bool failForwardAfterDeletion = false,
            bool? predictedSpam = null,
            string? forwardError = null
        )
        {
            var values = new Dictionary<string, string?>
            {
                ["DOORMAN_BOT_API"] = "123456:TEST_TOKEN",
                ["DOORMAN_ADMIN_CHAT"] = AdminChat.ToString(),
                ["DOORMAN_ADMIN_CHAT_MAP"] = $"{PaidChat}={AdminChat}",
                ["DOORMAN_OPENROUTER_API"] = modelsEnabled ? "sk-test" : null,
                ["DOORMAN_FREE_LLM_URL"] = null,
                ["DOORMAN_FREE_LLM_MODEL"] = null,
                ["DOORMAN_FREE_LLM_API"] = null,
                ["DOORMAN_CLUB_SERVICE_TOKEN"] = null,
                ["DOORMAN_HIGH_CONFIDENCE_AUTOBAN_DISABLE"] = null,
                ["DOORMAN_CHANNEL_MARKETOLOGY_EXCLUSION"] = null,
                ["DOORMAN_LOW_CONFIDENCE_HAM_ENABLE"] = lowConfidenceHamForward ? "true" : null,
                ["DOORMAN_APPROVED_ML_SPAM_CHECK_DISABLE"] = null,
            };
            var previousEnvironment = values.Keys.ToDictionary(key => key, Environment.GetEnvironmentVariable);
            foreach (var (key, value) in values)
                Environment.SetEnvironmentVariable(key, value);

            var db = new SqliteConnection("Data Source=:memory:");
            await db.OpenAsync();
            var requests = new ConcurrentQueue<(string Method, string Body)>();
            var llmRequests = new ConcurrentQueue<string>();
            var jevRequests = new ConcurrentQueue<string>();
            var reviewCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var telegramHttp = new HttpClient(
                new Handler(
                    async (request, ct) =>
                    {
                        var method = request.RequestUri!.Segments.Last();
                        var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct);
                        requests.Enqueue((method, body));
                        if (failForwardAfterDeletion && method == "forwardMessage" && requests.Any(x => x.Method == "deleteMessage"))
                            forwardError ??= "Bad Request: message to forward not found";
                        if (forwardError != null && method == "forwardMessage")
                        {
                            var response = JsonResponse(
                                new
                                {
                                    ok = false,
                                    error_code = 400,
                                    description = forwardError,
                                }
                            );
                            response.StatusCode = HttpStatusCode.BadRequest;
                            return response;
                        }
                        return TelegramResponse(method);
                    }
                )
            );
            var llmHttp = new HttpClient(
                new Handler(
                    async (request, ct) =>
                    {
                        var requestBody = await request.Content!.ReadAsStringAsync(ct);
                        llmRequests.Enqueue(requestBody);
                        if (modelRelease != null)
                            await modelRelease.WaitAsync(ct);
                        if (!llmAvailable)
                            throw new HttpRequestException("test LLM outage");
                        using var body = JsonDocument.Parse(requestBody);
                        var model = body.RootElement.GetProperty("model").GetString();
                        var content =
                            lunaContent
                            ?? JsonSerializer.Serialize(
                                new AiChecks.SpamProbability { Probability = llmProbability, Reason = "test verdict" }
                            );
                        return JsonResponse(
                            new
                            {
                                id = "chatcmpl-test",
                                @object = "chat.completion",
                                created = 0,
                                model,
                                choices = new[]
                                {
                                    new
                                    {
                                        index = 0,
                                        message = new { role = "assistant", content },
                                        finish_reason = "stop",
                                    },
                                },
                            }
                        );
                    }
                )
            );
            var api = new OpenAiClient("sk-test", llmHttp, baseUri: new Uri("https://llm.test/v1"));
            api.Options.Retry = new AutoSDKRetryOptions { MaxAttempts = 1 };
            var jevHttp = new HttpClient(
                new Handler(
                    async (request, ct) =>
                    {
                        jevRequests.Enqueue(await request.Content!.ReadAsStringAsync(ct));
                        if ((jevRelease ?? modelRelease) is { } release)
                            await release.WaitAsync(ct);
                        return JsonResponse(
                            new
                            {
                                model = "~typesafe/jev-latest",
                                answers = new
                                {
                                    spam = new
                                    {
                                        type = "choice",
                                        choice = jevLabel,
                                        confidence = jevConfidence,
                                    },
                                },
                            }
                        );
                    }
                )
            );

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ILogger<MessageProcessor>>(new ReviewLogger(reviewCompleted));
            services.AddHybridCache();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(db));
            services.AddSingleton<Config>();
            services.AddSingleton<ITelegramBotClient>(new TelegramBotClient("123456:TEST_TOKEN", telegramHttp));
            services.AddSingleton<UserManager>();
            services.AddSingleton(telegramHttp);
            services.AddSingleton<TelegramInvitePreviews>();
            services.AddSingleton(provider => new AiChecks(
                provider.GetRequiredService<ITelegramBotClient>(),
                provider.GetRequiredService<Config>(),
                provider.GetRequiredService<HybridCache>(),
                provider.GetRequiredService<UserManager>(),
                NullLogger<AiChecks>.Instance,
                provider.GetRequiredService<TelegramInvitePreviews>(),
                api,
                null
            ));
            services.AddSingleton(provider => new JevChecks(
                jevHttp,
                provider.GetRequiredService<Config>(),
                NullLogger<JevChecks>.Instance,
                provider.GetRequiredService<HybridCache>()
            ));
            services.AddSingleton<ReactionHandler>();
            services.AddSingleton<MessageProcessor>();
            services.AddSingleton<CaptchaManager>();
            services.AddSingleton<StatisticsReporter>();
            services.AddSingleton(provider => new SpamHamClassifier(
                NullLogger<SpamHamClassifier>.Instance,
                provider.GetRequiredService<IServiceScopeFactory>(),
                SpamHamClassifierStartupMode.SkipBackgroundTraining
            ));
            services.AddSingleton<AdminCommandHandler>();
            services.AddSingleton<BadMessageManager>();
            services.AddSingleton<RecentMessagesStorage>();
            services.AddSingleton<SpamDeduplicationCache>();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<BioInviteTracker>();
            var serviceProvider = services.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
            // Config resolves chat titles during initialization; keep the fixture independent of Telegram.
            var cache = serviceProvider.GetRequiredService<HybridCache>();
            await cache.SetAsync($"full_chan:{PaidChat}", new Config.ChatInfo(PaidChat, "Paid"));
            await cache.SetAsync($"full_chan:{AdminChat}", new Config.ChatInfo(AdminChat, "Admin"));

            var result = new Fixture(
                previousEnvironment,
                db,
                serviceProvider,
                telegramHttp,
                llmHttp,
                api,
                requests,
                llmRequests,
                jevRequests,
                reviewCompleted,
                jevHttp,
                score,
                llmAvailable && lunaContent == null && llmProbability >= Consts.LlmHighProbability
            );
            result.InstallClassifierScore(serviceProvider.GetRequiredService<SpamHamClassifier>(), predictedSpam);
            return result;
        }

        public async Task<CheckResult> Check(long chatId, string? replyText = null)
        {
            var processor = _services.GetRequiredService<MessageProcessor>();
            _reviewStarted =
                _services.GetRequiredService<JevChecks>().Enabled
                && (_score is > -0.5f and < 0.5f || chatId == PaidChat && _score < 0 && _lunaHighSpam);
            var message = new Message
            {
                Id = 123,
                From = _user,
                Chat = new Chat
                {
                    Id = chatId,
                    Title = chatId == PaidChat ? "Paid" : "Free",
                    Type = ChatType.Supergroup,
                },
                Text = "ordinary message with enough text",
                ReplyToMessage = replyText == null ? null : new Message { Id = 122, Text = replyText },
            };
            var method = typeof(MessageProcessor).GetMethod("CheckMessageContent", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var result =
                (Task<CheckResult>)
                    method.Invoke(
                        processor,
                        [message, _user, message.Text!, message.Text!, message.Text!, message.Chat, CancellationToken.None]
                    )!;
            return await result;
        }

        public async Task CheckTextUpdate(long chatId, string text)
        {
            await _services.GetRequiredService<HybridCache>().SetAsync($"user:banned:{_user.Id}", false);
            await _services
                .GetRequiredService<MessageProcessor>()
                .HandleUpdate(
                    new Update
                    {
                        Message = new Message
                        {
                            Id = 127,
                            From = _user,
                            Chat = new Chat
                            {
                                Id = chatId,
                                Title = chatId == PaidChat ? "Paid" : "Free",
                                Type = ChatType.Supergroup,
                            },
                            Text = text,
                        },
                    },
                    CancellationToken.None
                );
        }

        public async Task CheckPoll(bool edited)
        {
            await _services.GetRequiredService<HybridCache>().SetAsync($"user:banned:{_user.Id}", false);
            var message = new Message
            {
                Id = 126,
                From = _user,
                Chat = new Chat
                {
                    Id = PaidChat,
                    Title = "Paid",
                    Type = ChatType.Supergroup,
                },
                Poll = new Poll
                {
                    Id = "123",
                    Question = "https://t.me/+example joined?",
                    Options = [new PollOption { Text = "option one" }, new PollOption { Text = "option two" }],
                },
            };
            var update = edited ? new Update { EditedMessage = message } : new Update { Message = message };
            await _services.GetRequiredService<MessageProcessor>().HandleUpdate(update, CancellationToken.None);
        }

        public async Task CheckApproved(long chatId)
        {
            await _services.GetRequiredService<UserManager>().Approve(_user.Id);
            _reviewStarted = _score is > -0.5f and < 0.5f && _services.GetRequiredService<JevChecks>().Enabled;
            await _services
                .GetRequiredService<MessageProcessor>()
                .HandleUpdate(
                    new Update
                    {
                        Message = new Message
                        {
                            Id = 125,
                            From = _user,
                            Chat = new Chat
                            {
                                Id = chatId,
                                Title = "Trusted",
                                Type = ChatType.Supergroup,
                            },
                            Text = "trusted message with enough text",
                        },
                    },
                    CancellationToken.None
                );
        }

        public async Task<bool> SpamVerdictIsAvailable(long chatId)
        {
            var message = new Message
            {
                Id = 123,
                From = _user,
                Chat = new Chat
                {
                    Id = chatId,
                    Title = chatId == PaidChat ? "Paid" : "Free",
                    Type = ChatType.Supergroup,
                },
                Text = "ordinary message with enough text",
            };
            return (await _services.GetRequiredService<AiChecks>().GetSpamProbability(message)).IsAvailable;
        }

        private void InstallClassifierScore(SpamHamClassifier classifier, bool? predictedSpam)
        {
            var ml = new MLContext(seed: 1);
            var data = ml.Data.LoadFromEnumerable(new[] { new MessageData { Text = "smoke" } });
            var transformer = ml
                .Transforms.CustomMapping<MessageData, FixedPrediction>(
                    (_, output) =>
                    {
                        output.Score = _score;
                        output.PredictedLabel = predictedSpam ?? _score > Consts.ClassifierSpamScoreThreshold;
                    },
                    contractName: null
                )
                .Fit(data);
            var engine = ml.Model.CreatePredictionEngine<MessageData, MessagePrediction>(transformer);
            typeof(SpamHamClassifier).GetField("_engine", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(classifier, engine);
        }

        public async ValueTask DisposeAsync()
        {
            if (_reviewStarted)
                await ReviewCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            _services.Dispose();
            _api.Dispose();
            _llmHttp.Dispose();
            _jevHttp.Dispose();
            _telegramHttp.Dispose();
            _db.Dispose();
            foreach (var (key, value) in _previousEnvironment)
                Environment.SetEnvironmentVariable(key, value);
        }

        private sealed class FixedPrediction
        {
            public float Score { get; set; }
            public bool PredictedLabel { get; set; }
        }

        private sealed class ReviewLogger(TaskCompletionSource completed) : ILogger<MessageProcessor>
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            )
            {
                if (formatter(state, exception).StartsWith("Spam/ham dataset review finished", StringComparison.Ordinal))
                    completed.TrySetResult();
            }
        }

        private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                respond(request, cancellationToken);
        }

        private static HttpResponseMessage TelegramResponse(string method) =>
            method switch
            {
                "getMe" => JsonResponse(
                    new
                    {
                        ok = true,
                        result = new
                        {
                            id = 123456,
                            is_bot = true,
                            first_name = "Test bot",
                        },
                    }
                ),
                "getChat" => JsonResponse(
                    new
                    {
                        ok = true,
                        result = new
                        {
                            id = PaidChat,
                            type = "supergroup",
                            title = "Paid",
                            description = "test chat",
                        },
                    }
                ),
                "sendMessage" or "forwardMessage" => JsonResponse(
                    new
                    {
                        ok = true,
                        result = new
                        {
                            message_id = 124,
                            date = 0,
                            chat = new { id = AdminChat, type = "supergroup" },
                        },
                    }
                ),
                "deleteMessage" or "restrictChatMember" => JsonResponse(new { ok = true, result = true }),
                _ => throw new InvalidOperationException($"Unexpected Telegram request: {method}"),
            };

        private static HttpResponseMessage JsonResponse(object value) =>
            new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };
    }
}
