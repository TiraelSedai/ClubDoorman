using ClubDoorman.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для ConfigurationException
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class ConfigurationExceptionTestFactory
{

    public ConfigurationException CreateConfigurationException()
    {
        return new ConfigurationException(
            "Test exception message"
        );
    }

    #region Configuration Methods

    #endregion
}
