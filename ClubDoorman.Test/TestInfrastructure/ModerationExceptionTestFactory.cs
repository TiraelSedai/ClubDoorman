using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для ModerationException
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ModerationExceptionTestFactory
{

    public ModerationException CreateModerationException()
    {
        return new ModerationException(
            "Test exception message"
        );
    }

    #region Configuration Methods

    #endregion
}
