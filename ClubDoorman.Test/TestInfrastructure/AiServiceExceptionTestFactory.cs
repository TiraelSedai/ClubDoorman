using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ClubDoorman.Services;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для AiServiceException
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class AiServiceExceptionTestFactory
{

    public AiServiceException CreateAiServiceException()
    {
        return new AiServiceException(
            "Test exception message"
        );
    }

    #region Configuration Methods

    #endregion
}
