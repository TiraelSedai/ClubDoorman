using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для ApprovedUsersStorage
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ApprovedUsersStorageTestFactory
{
    public Mock<ILogger<ApprovedUsersStorage>> LoggerMock { get; } = new();

    public ApprovedUsersStorage CreateApprovedUsersStorage()
    {
        return new ApprovedUsersStorage(
            LoggerMock.Object
        );
    }

    /// <summary>
    /// Создает экземпляр с кастомным путем к файлу для тестирования
    /// </summary>
    public ApprovedUsersStorage CreateApprovedUsersStorageWithCustomPath(string filePath)
    {
        // Используем рефлексию для установки приватного поля _filePath
        var service = new ApprovedUsersStorage(LoggerMock.Object);
        var field = typeof(ApprovedUsersStorage).GetField("_filePath", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(service, filePath);
        return service;
    }

    #region Configuration Methods

    public ApprovedUsersStorageTestFactory WithLoggerSetup(Action<Mock<ILogger<ApprovedUsersStorage>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    #endregion

    #region Smart Methods Based on Business Logic

    public IUserManager CreateUserManagerWithFake()
    {
        return new Mock<IUserManager>().Object;
    }
    #endregion
}
