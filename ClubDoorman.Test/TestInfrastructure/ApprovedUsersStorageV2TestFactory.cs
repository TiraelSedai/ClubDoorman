using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для ApprovedUsersStorageV2
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ApprovedUsersStorageV2TestFactory
{
    public Mock<ILogger<ApprovedUsersStorageV2>> LoggerMock { get; } = new();

    public ApprovedUsersStorageV2 CreateApprovedUsersStorageV2()
    {
        return new ApprovedUsersStorageV2(
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public ApprovedUsersStorageV2TestFactory WithLoggerSetup(Action<Mock<ILogger<ApprovedUsersStorageV2>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion
}
