using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для MimicryClassifier
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class MimicryClassifierTestFactory
{
    public Mock<ILogger<MimicryClassifier>> LoggerMock { get; } = new();

    public MimicryClassifier CreateMimicryClassifier()
    {
        return new MimicryClassifier(
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public MimicryClassifierTestFactory WithLoggerSetup(Action<Mock<ILogger<MimicryClassifier>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion
}
