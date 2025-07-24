using ClubDoorman.Services;
using Moq;

namespace ClubDoorman.Test.TestInfrastructure;

/// <summary>
/// Фабрика для создания моков IAppConfig в тестах
/// </summary>
public static class AppConfigTestFactory
{
    /// <summary>
    /// Создаёт мок IAppConfig с настройками по умолчанию для тестов
    /// </summary>
    public static IAppConfig CreateDefault()
    {
        var mock = new Mock<IAppConfig>();
        
        // Настройки по умолчанию для тестов
        mock.Setup(x => x.OpenRouterApi).Returns("test-api-key");
        mock.Setup(x => x.SuspiciousDetectionEnabled).Returns(true);
        mock.Setup(x => x.MimicryThreshold).Returns(0.7);
        mock.Setup(x => x.SuspiciousToApprovedMessageCount).Returns(3);
        mock.Setup(x => x.AdminChatId).Returns(123456789);
        mock.Setup(x => x.LogAdminChatId).Returns(123456789);
        mock.Setup(x => x.AiEnabledChats).Returns(new HashSet<long> { 123456789 });
        
        // Методы
        mock.Setup(x => x.IsAiEnabledForChat(It.IsAny<long>())).Returns(true);
        mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
        mock.Setup(x => x.IsPrivateStartAllowed()).Returns(true);
        
        return mock.Object;
    }
    
    /// <summary>
    /// Создаёт мок IAppConfig с отключенным AI
    /// </summary>
    public static IAppConfig CreateWithoutAi()
    {
        var mock = new Mock<IAppConfig>();
        
        // Настройки без AI
        mock.Setup(x => x.OpenRouterApi).Returns((string?)null);
        mock.Setup(x => x.SuspiciousDetectionEnabled).Returns(false);
        mock.Setup(x => x.MimicryThreshold).Returns(0.7);
        mock.Setup(x => x.SuspiciousToApprovedMessageCount).Returns(3);
        mock.Setup(x => x.AdminChatId).Returns(123456789);
        mock.Setup(x => x.LogAdminChatId).Returns(123456789);
        mock.Setup(x => x.AiEnabledChats).Returns(new HashSet<long>());
        
        // Методы
        mock.Setup(x => x.IsAiEnabledForChat(It.IsAny<long>())).Returns(false);
        mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(true);
        mock.Setup(x => x.IsPrivateStartAllowed()).Returns(true);
        
        return mock.Object;
    }
    
    /// <summary>
    /// Создаёт мок IAppConfig с кастомными настройками
    /// </summary>
    public static IAppConfig CreateCustom(
        string? openRouterApi = "test-api-key",
        bool suspiciousDetectionEnabled = true,
        double mimicryThreshold = 0.7,
        int suspiciousToApprovedMessageCount = 3,
        long adminChatId = 123456789,
        long logAdminChatId = 123456789,
        HashSet<long>? aiEnabledChats = null,
        bool isAiEnabledForChat = true,
        bool isChatAllowed = true,
        bool isPrivateStartAllowed = true)
    {
        var mock = new Mock<IAppConfig>();
        
        mock.Setup(x => x.OpenRouterApi).Returns(openRouterApi);
        mock.Setup(x => x.SuspiciousDetectionEnabled).Returns(suspiciousDetectionEnabled);
        mock.Setup(x => x.MimicryThreshold).Returns(mimicryThreshold);
        mock.Setup(x => x.SuspiciousToApprovedMessageCount).Returns(suspiciousToApprovedMessageCount);
        mock.Setup(x => x.AdminChatId).Returns(adminChatId);
        mock.Setup(x => x.LogAdminChatId).Returns(logAdminChatId);
        mock.Setup(x => x.AiEnabledChats).Returns(aiEnabledChats ?? new HashSet<long> { adminChatId });
        
        mock.Setup(x => x.IsAiEnabledForChat(It.IsAny<long>())).Returns(isAiEnabledForChat);
        mock.Setup(x => x.IsChatAllowed(It.IsAny<long>())).Returns(isChatAllowed);
        mock.Setup(x => x.IsPrivateStartAllowed()).Returns(isPrivateStartAllowed);
        
        return mock.Object;
    }
} 