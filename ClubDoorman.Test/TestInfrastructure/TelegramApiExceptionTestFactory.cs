using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для TelegramApiException
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class TelegramApiExceptionTestFactory
{

    public TelegramApiException CreateTelegramApiException()
    {
        return new TelegramApiException(
            "Test exception message"
        );
    }

    #region Configuration Methods

    #endregion
}
