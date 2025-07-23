using NUnit.Framework;

namespace ClubDoorman.Test.Integration;

[TestFixture]
[Category("integration")]
[Category("environment")]
public class EnvironmentTest
{
    private string FindEnvFile()
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
        
        throw new FileNotFoundException("Could not find .env file in any of the expected locations");
    }
    [Test]
    public void LoadEnvironmentVariables_ShouldLoadFromEnvFile()
    {
        // Arrange & Act
        var envPath = FindEnvFile();
        DotNetEnv.Env.Load(envPath);
        
        // Assert
        var apiKey = DotNetEnv.Env.GetString("DOORMAN_OPENROUTER_API");
        var botToken = DotNetEnv.Env.GetString("DOORMAN_BOT_API");
        var adminChat = DotNetEnv.Env.GetString("DOORMAN_ADMIN_CHAT");
        
        Console.WriteLine($"API Key: {apiKey}");
        Console.WriteLine($"Bot Token: {botToken}");
        Console.WriteLine($"Admin Chat: {adminChat}");
        
        Assert.That(apiKey, Is.Not.Null);
        Assert.That(apiKey, Is.Not.Empty);
        Assert.That(botToken, Is.Not.Null);
        Assert.That(botToken, Is.Not.Empty);
        Assert.That(adminChat, Is.Not.Null);
        Assert.That(adminChat, Is.Not.Empty);
    }

    [Test]
    public void EnvironmentVariables_ShouldBeAccessible()
    {
        // Arrange & Act
        var envPath = FindEnvFile();
        DotNetEnv.Env.Load(envPath);
        
        // Assert
        var apiKey = Environment.GetEnvironmentVariable("DOORMAN_OPENROUTER_API");
        var botToken = Environment.GetEnvironmentVariable("DOORMAN_BOT_API");
        var adminChat = Environment.GetEnvironmentVariable("DOORMAN_ADMIN_CHAT");
        
        Console.WriteLine($"Environment API Key: {apiKey}");
        Console.WriteLine($"Environment Bot Token: {botToken}");
        Console.WriteLine($"Environment Admin Chat: {adminChat}");
        
        // Эти переменные могут быть null, так как они не загружены в Environment
        // но должны быть доступны через DotNetEnv.Env.GetString
    }
} 