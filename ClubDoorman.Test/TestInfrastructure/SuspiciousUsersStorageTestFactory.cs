using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для SuspiciousUsersStorage
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class SuspiciousUsersStorageTestFactory
{
    public Mock<ILogger<SuspiciousUsersStorage>> LoggerMock { get; } = new();

    public SuspiciousUsersStorage CreateSuspiciousUsersStorage()
    {
        return new SuspiciousUsersStorage(
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public SuspiciousUsersStorageTestFactory WithLoggerSetup(Action<Mock<ILogger<SuspiciousUsersStorage>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion
}
