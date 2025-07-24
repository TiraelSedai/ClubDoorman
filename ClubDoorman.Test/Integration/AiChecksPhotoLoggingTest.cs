using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using ClubDoorman.Test.TestInfrastructure;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Reflection;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ClubDoorman.Test.Integration;

[TestFixture]
[Category("integration")]
[Category("ai-photo")]
public class AiChecksPhotoLoggingTest
{
    private string? FindEnvFile()
    {
        var baseDir = AppContext.BaseDirectory;
        var currentDir = Directory.GetCurrentDirectory();
        Console.WriteLine($"AppContext.BaseDirectory: {baseDir}");
        Console.WriteLine($"Current directory: {currentDir}");
        
        // Пробуем разные пути относительно AppContext.BaseDirectory
        var possiblePaths = new[]
        {
            Path.Combine(baseDir, "../../../../ClubDoorman/.env"),
            Path.Combine(baseDir, "../../../ClubDoorman/.env"),
            Path.Combine(baseDir, "../../ClubDoorman/.env"),
            Path.Combine(baseDir, "../ClubDoorman/.env"),
            Path.Combine(baseDir, "ClubDoorman/.env"),
            Path.Combine(baseDir, "../../../../ClubDoorman/ClubDoorman/.env"),
            Path.Combine(baseDir, "../../../ClubDoorman/ClubDoorman/.env"),
            Path.Combine(baseDir, "../../ClubDoorman/ClubDoorman/.env"),
            Path.Combine(baseDir, "../ClubDoorman/ClubDoorman/.env"),
            Path.Combine(baseDir, "ClubDoorman/ClubDoorman/.env")
        };
        
        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            Console.WriteLine($"Checking path: {path} -> {fullPath}");
            if (File.Exists(path))
            {
                Console.WriteLine($"Found .env file at: {path}");
                return path;
            }
        }
        
        return null; // Файл не найден
    }
    private ILogger<AiChecks> _logger = null!;
    private FakeTelegramClient _fakeBot = null!;
    private AiChecks _aiChecks = null!;

    [SetUp]
    public void Setup()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<AiChecks>();
        _fakeBot = new FakeTelegramClient();
        
        // Загружаем .env файл
        var envPath = FindEnvFile();
        if (envPath == null)
        {
            Assert.Ignore("Файл .env не найден, пропускаем интеграционный тест");
        }
        DotNetEnv.Env.Load(envPath);
        
        // Загружаем переменные в Environment для Config.cs
        var apiKey = DotNetEnv.Env.GetString("DOORMAN_OPENROUTER_API");
        var botToken = DotNetEnv.Env.GetString("DOORMAN_BOT_API");
        var adminChat = DotNetEnv.Env.GetString("DOORMAN_ADMIN_CHAT");
        
        Environment.SetEnvironmentVariable("DOORMAN_OPENROUTER_API", apiKey);
        Environment.SetEnvironmentVariable("DOORMAN_BOT_API", botToken);
        Environment.SetEnvironmentVariable("DOORMAN_ADMIN_CHAT", adminChat);
        
        // Отладочная информация
        Console.WriteLine($"DotNetEnv API Key: {apiKey}");
        Console.WriteLine($"Environment API Key: {Environment.GetEnvironmentVariable("DOORMAN_OPENROUTER_API")}");
        
        // Проверяем наличие API ключа
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.Ignore("DOORMAN_OPENROUTER_API не установлен, пропускаем интеграционный тест");
        }
        
        _aiChecks = new AiChecks(_fakeBot, _logger, AppConfigTestFactory.CreateDefault());
    }

    [Test]
    public async Task GetAttentionBaitProbability_WithRealPhoto_ShouldAnalyzePhotoInAPI()
    {
        // Arrange - создаем пользователя с фото, но БЕЗ био
        var user = new User
        {
            Id = 12345,
            FirstName = "Test",
            LastName = "User"
        };

        // Настраиваем FakeTelegramClient чтобы он возвращал реальное фото
        _fakeBot.SetupGetChatFullInfo(user.Id, new ChatFullInfo
        {
            Id = user.Id,
            Type = ChatType.Private,
            Bio = null, // НЕТ био - это важно!
            LinkedChatId = null,
            Photo = new ChatPhoto
            {
                SmallFileId = "fake_small_file_id",
                BigFileId = "fake_big_file_id"
            }
        });

        // Настраиваем GetFileAsync чтобы возвращать реальное фото
        var photoPath = "/home/kpblc/projects/ClubDoorman/tmp/big.png";
        _fakeBot.SetupGetFile("fake_big_file_id", photoPath);

        // Act
        var result = await _aiChecks.GetAttentionBaitProbability(user);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Photo.Length, Is.GreaterThan(0), "Фото должно быть загружено");
        Assert.That(result.SpamProbability, Is.Not.Null);
        
        Console.WriteLine($"Результат: вероятность={result.SpamProbability.Probability}, причина={result.SpamProbability.Reason}");
        Console.WriteLine($"Размер фото: {result.Photo.Length} байт");
    }
}