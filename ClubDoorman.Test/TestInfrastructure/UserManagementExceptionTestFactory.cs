using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для UserManagementException
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class UserManagementExceptionTestFactory
{

    public UserManagementException CreateUserManagementException()
    {
        return new UserManagementException(
            "Test exception message"
        );
    }

    #region Configuration Methods

    #endregion
}
