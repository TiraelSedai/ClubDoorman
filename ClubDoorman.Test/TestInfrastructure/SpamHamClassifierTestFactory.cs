using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для SpamHamClassifier
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class SpamHamClassifierTestFactory
{
    public Mock<ILogger<SpamHamClassifier>> LoggerMock { get; } = new();

    public SpamHamClassifier CreateSpamHamClassifier()
    {
        return new SpamHamClassifier(
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public SpamHamClassifierTestFactory WithLoggerSetup(Action<Mock<ILogger<SpamHamClassifier>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion

    #region Smart Methods Based on Business Logic

    public async Task<SpamHamClassifier> CreateAsync()
    {
        return await Task.FromResult(CreateSpamHamClassifier());
    }

    public Mock<ISpamHamClassifier> CreateMockSpamHamClassifier()
    {
        return new Mock<ISpamHamClassifier>();
    }
    #endregion
}
