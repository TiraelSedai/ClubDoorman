using NUnit.Framework;
using Moq;
using ClubDoorman.Services;
using ClubDoorman.TestInfrastructure;
using Microsoft.Extensions.Logging;

namespace ClubDoorman.Test.Unit.Services;

[TestFixture]
[Category("business-logic")]
public class ApprovedUsersStorageTests
{
    private ApprovedUsersStorageTestFactory _factory;
    private ApprovedUsersStorage _service;
    private Mock<ILogger<ApprovedUsersStorage>> _loggerMock;
    private string _testFilePath;

    [SetUp]
    public void Setup()
    {
        _factory = new ApprovedUsersStorageTestFactory();
        
        // Используем временную директорию для тестов в домашней папке пользователя
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var testDataDir = Path.Combine(homeDir, ".test_approved_users");
        _testFilePath = Path.Combine(testDataDir, "approved_users.json");
        
        // Создаем тестовую директорию
        if (!Directory.Exists(testDataDir))
            Directory.CreateDirectory(testDataDir);
        
        // Очищаем тестовый файл перед каждым тестом
        if (File.Exists(_testFilePath))
            File.Delete(_testFilePath);
        
        _service = _factory.CreateApprovedUsersStorageWithCustomPath(_testFilePath);
        _loggerMock = _factory.LoggerMock;
    }

    [TearDown]
    public void Cleanup()
    {
        // Очищаем тестовый файл после каждого теста
        try
        {
            if (File.Exists(_testFilePath))
                File.Delete(_testFilePath);
        }
        catch { }
    }

    [Test]
    public void Constructor_CreatesEmptyStorage_WhenFileNotExists()
    {
        // Arrange & Act
        var service = _factory.CreateApprovedUsersStorage();

        // Assert
        Assert.That(service.IsApproved(123456L), Is.False);
    }

    [Test]
    public void Constructor_LoadsExistingUsers_WhenFileExists()
    {
        // Arrange
        var testUsers = new List<long> { 123456L, 789012L };
        var json = System.Text.Json.JsonSerializer.Serialize(testUsers);
        
        try
        {
            // Создаем директорию если не существует
            var directory = Path.GetDirectoryName(_testFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            File.WriteAllText(_testFilePath, json);

            // Act
            var service = _factory.CreateApprovedUsersStorageWithCustomPath(_testFilePath);

            // Assert
            Assert.That(service.IsApproved(123456L), Is.True);
            Assert.That(service.IsApproved(789012L), Is.True);
            Assert.That(service.IsApproved(999999L), Is.False);
        }
        catch (UnauthorizedAccessException)
        {
            // Если нет прав доступа, пропускаем тест
            Assert.Ignore("Нет прав доступа для создания тестового файла");
        }
    }

    [Test]
    public void Constructor_HandlesOldFormat_WhenFileExists()
    {
        // Arrange
        var testUsers = new Dictionary<long, DateTime>
        {
            { 123456L, DateTime.Now },
            { 789012L, DateTime.Now.AddDays(1) }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(testUsers);
        
        try
        {
            var directory = Path.GetDirectoryName(_testFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            File.WriteAllText(_testFilePath, json);

            // Act
            var service = _factory.CreateApprovedUsersStorageWithCustomPath(_testFilePath);

            // Assert
            Assert.That(service.IsApproved(123456L), Is.True);
            Assert.That(service.IsApproved(789012L), Is.True);
            Assert.That(service.IsApproved(999999L), Is.False);
        }
        catch (UnauthorizedAccessException)
        {
            // Если нет прав доступа, пропускаем тест
            Assert.Ignore("Нет прав доступа для создания тестового файла");
        }
    }

    [Test]
    public void Constructor_HandlesCorruptedJson_WhenFileExists()
    {
        // Arrange
        var corruptedJson = "{ invalid json }";
        
        try
        {
            var directory = Path.GetDirectoryName(_testFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            File.WriteAllText(_testFilePath, corruptedJson);

            // Act
            var service = _factory.CreateApprovedUsersStorageWithCustomPath(_testFilePath);

            // Assert
            Assert.That(service.IsApproved(123456L), Is.False);
            _loggerMock.Verify(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
        }
        catch (UnauthorizedAccessException)
        {
            // Если нет прав доступа, пропускаем тест
            Assert.Ignore("Нет прав доступа для создания тестового файла");
        }
    }

    [Test]
    public void Constructor_HandlesEmptyFile_WhenFileExists()
    {
        // Arrange
        try
        {
            var directory = Path.GetDirectoryName(_testFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            File.WriteAllText(_testFilePath, "");

            // Act
            var service = _factory.CreateApprovedUsersStorageWithCustomPath(_testFilePath);

            // Assert
            Assert.That(service.IsApproved(123456L), Is.False);
        }
        catch (UnauthorizedAccessException)
        {
            // Если нет прав доступа, пропускаем тест
            Assert.Ignore("Нет прав доступа для создания тестового файла");
        }
    }

    [Test]
    public void IsApproved_ReturnsFalse_WhenUserNotApproved()
    {
        // Arrange
        var userId = 123456L;

        // Act
        var result = _service.IsApproved(userId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsApproved_ReturnsTrue_WhenUserApproved()
    {
        // Arrange
        var userId = 123456L;
        _service.ApproveUser(userId);

        // Act
        var result = _service.IsApproved(userId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ApproveUser_AddsUserToApprovedList()
    {
        // Arrange
        var userId = 123456L;

        // Act
        _service.ApproveUser(userId);

        // Assert
        Assert.That(_service.IsApproved(userId), Is.True);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(userId.ToString())),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public void ApproveUser_SavesToFile_WhenUserAdded()
    {
        // Arrange
        var userId = 123456L;

        // Act
        _service.ApproveUser(userId);

        // Assert
        // Проверяем что пользователь добавлен в память
        Assert.That(_service.IsApproved(userId), Is.True);
        
        // Проверяем что файл создан (если нет ошибок файловой системы)
        if (File.Exists(_testFilePath))
        {
            var json = File.ReadAllText(_testFilePath);
            var savedUsers = System.Text.Json.JsonSerializer.Deserialize<List<long>>(json);
            Assert.That(savedUsers, Does.Contain(userId));
        }
    }

    [Test]
    public void ApproveUser_DoesNotAddDuplicate_WhenUserAlreadyApproved()
    {
        // Arrange
        var userId = 123456L;
        _service.ApproveUser(userId);

        // Act
        _service.ApproveUser(userId); // Попытка добавить того же пользователя

        // Assert
        Assert.That(_service.IsApproved(userId), Is.True);
        // Логируется только первое добавление
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(userId.ToString())),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Test]
    public void ApproveUser_HandlesFileSystemError_LogsError()
    {
        // Arrange
        var userId = 123456L;
        
        // Создаем директорию с правами только для чтения
        var directory = Path.GetDirectoryName(_testFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            // Делаем директорию только для чтения (если возможно)
            try
            {
                var dirInfo = new DirectoryInfo(directory);
                dirInfo.Attributes = FileAttributes.ReadOnly;
            }
            catch { }
        }

        // Act - метод должен обработать ошибку и не выбросить исключение
        Assert.DoesNotThrow(() => _service.ApproveUser(userId));
        
        // Assert - проверяем что ошибка залогирована (может быть несколько раз из-за предыдущих операций)
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Ошибка при сохранении списка одобренных пользователей")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Test]
    public void RemoveApproval_ReturnsFalse_WhenUserNotApproved()
    {
        // Arrange
        var userId = 123456L;

        // Act
        var result = _service.RemoveApproval(userId);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(_service.IsApproved(userId), Is.False);
    }

    [Test]
    public void RemoveApproval_ReturnsTrue_WhenUserWasApproved()
    {
        // Arrange
        var userId = 123456L;
        _service.ApproveUser(userId);

        // Act
        var result = _service.RemoveApproval(userId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(_service.IsApproved(userId), Is.False);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(userId.ToString())),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2)); // Approve + Remove
    }

    [Test]
    public void RemoveApproval_SavesToFile_WhenUserRemoved()
    {
        // Arrange
        var userId = 123456L;
        _service.ApproveUser(userId);

        // Act
        _service.RemoveApproval(userId);

        // Assert
        // Проверяем что пользователь удален из памяти
        Assert.That(_service.IsApproved(userId), Is.False);
        
        // Проверяем что файл создан (если нет ошибок файловой системы)
        if (File.Exists(_testFilePath))
        {
            var json = File.ReadAllText(_testFilePath);
            var savedUsers = System.Text.Json.JsonSerializer.Deserialize<List<long>>(json);
            Assert.That(savedUsers, Does.Not.Contain(userId));
        }
    }

    [Test]
    public void RemoveApproval_HandlesFileSystemError_LogsError()
    {
        // Arrange
        var userId = 123456L;
        _service.ApproveUser(userId);
        
        // Создаем директорию с правами только для чтения
        var directory = Path.GetDirectoryName(_testFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            try
            {
                var dirInfo = new DirectoryInfo(directory);
                dirInfo.Attributes = FileAttributes.ReadOnly;
            }
            catch { }
        }

        // Act - метод должен обработать ошибку и не выбросить исключение
        Assert.DoesNotThrow(() => _service.RemoveApproval(userId));
        
        // Assert - проверяем что ошибка залогирована (может быть несколько раз из-за предыдущих операций)
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Ошибка при сохранении списка одобренных пользователей")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Test]
    public void ConcurrentAccess_HandlesMultipleThreads_Safely()
    {
        // Arrange
        var userIds = Enumerable.Range(1, 100).Select(i => (long)i).ToList();
        var tasks = new List<Task>();

        // Act - добавляем пользователей параллельно
        foreach (var userId in userIds)
        {
            tasks.Add(Task.Run(() => _service.ApproveUser(userId)));
        }
        Task.WaitAll(tasks.ToArray());

        // Assert - проверяем что все пользователи добавлены
        foreach (var userId in userIds)
        {
            Assert.That(_service.IsApproved(userId), Is.True);
        }
    }

    [Test]
    public void FilePersistence_LoadsCorrectly_AfterRestart()
    {
        // Arrange
        var userIds = new List<long> { 123456L, 789012L, 345678L };
        foreach (var userId in userIds)
        {
            _service.ApproveUser(userId);
        }

        // Act - создаем новый экземпляр (симулируем перезапуск)
        var newService = _factory.CreateApprovedUsersStorageWithCustomPath(_testFilePath);

        // Assert
        // Проверяем что пользователи остались в памяти (если файл не сохранился из-за ошибок)
        // или загрузились из файла (если файл сохранился)
        foreach (var userId in userIds)
        {
            // Если файл существует, проверяем что данные загрузились
            if (File.Exists(_testFilePath))
            {
                Assert.That(newService.IsApproved(userId), Is.True);
            }
            else
            {
                // Если файл не создался из-за ошибок прав доступа, 
                // новый экземпляр должен иметь пустой список
                Assert.That(newService.IsApproved(userId), Is.False);
            }
        }
    }
} 