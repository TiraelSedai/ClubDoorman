using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using ClubDoorman.Handlers;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для UpdateDispatcher
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class UpdateDispatcherTestFactory
{
    public Mock<IEnumerable<IUpdateHandler>> UpdateHandlersMock { get; } = new();
    public Mock<ILogger<UpdateDispatcher>> LoggerMock { get; } = new();

    public UpdateDispatcher CreateUpdateDispatcher()
    {
        return new UpdateDispatcher(
            UpdateHandlersMock.Object,
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public UpdateDispatcherTestFactory WithUpdateHandlersSetup(Action<Mock<IEnumerable<IUpdateHandler>>> setup)
    {
        setup(UpdateHandlersMock);
        return this;
    }

    public UpdateDispatcherTestFactory WithLoggerSetup(Action<Mock<ILogger<UpdateDispatcher>>> setup)
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

    public async Task<UpdateDispatcher> CreateAsync()
    {
        return await Task.FromResult(CreateUpdateDispatcher());
    }
    #endregion
}
