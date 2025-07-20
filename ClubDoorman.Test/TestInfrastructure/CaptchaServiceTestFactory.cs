using ClubDoorman.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ClubDoorman.TestInfrastructure;

/// <summary>
/// TestFactory для CaptchaService
/// Автоматически сгенерировано
/// </summary>
[TestFixture]
[Category("test-infrastructure")]
public class CaptchaServiceTestFactory
{
    public Mock<ILogger<CaptchaService>> LoggerMock { get; } = new();
    public Mock<ITelegramBotClientWrapper> TelegramBotClientWrapperMock { get; } = new();
    public FakeTelegramClient FakeTelegramClient { get; } = new();

    public CaptchaService CreateCaptchaService()
    {
        return new CaptchaService(
            FakeTelegramClient,
            LoggerMock.Object
        );
    }

    public CaptchaService CreateCaptchaServiceWithFake(FakeTelegramClient? fake = null)
    {
        var client = fake ?? new FakeTelegramClient();
        return new CaptchaService(
            client,
            LoggerMock.Object
        );
    }

    #region Configuration Methods

    public CaptchaServiceTestFactory WithLoggerSetup(Action<Mock<ILogger<CaptchaService>>> setup)
    {
        setup(LoggerMock);
        return this;
    }

    public CaptchaServiceTestFactory WithTelegramBotClientWrapperSetup(Action<Mock<ITelegramBotClientWrapper>> setup)
    {
        setup(TelegramBotClientWrapperMock);
        return this;
    }



    #endregion
}
