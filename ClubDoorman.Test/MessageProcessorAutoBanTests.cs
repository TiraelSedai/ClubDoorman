namespace ClubDoorman.Test;

public class MessageProcessorAutoBanTests
{
    [TestCase(1.2529202f, 0.85, true)]
    [TestCase(1.000001f, 0.85, true)]
    [TestCase(1f, 0.85, false)]
    [TestCase(0.9996753f, 0.85, false)]
    [TestCase(2f, 0.849999, false)]
    [TestCase(2f, 0.80, false)]
    [TestCase(1.2999327f, 0.05, false)]
    [TestCase(0.31f, 0.90, true)]
    [TestCase(1f, 0.90, true)]
    [TestCase(0.9f, 0.899999, false)]
    public void MlSpam_AutoBanRequiresLlmConfirmation(float score, double llmProbability, bool expected)
    {
        Assert.That(MessageProcessor.ShouldAutoBanMlSpam(score, llmProbability), Is.EqualTo(expected));
    }
}
