using Telegram.Bot.Types;
using ClubDoorman.Test.TestKit;

namespace ClubDoorman.Test.TestKit.Builders;

/// <summary>
/// Builder для создания пользователей Telegram
/// <tags>builders, user, telegram, fluent-api</tags>
/// </summary>
public class UserBuilder
{
    private User _user = TestKitBogus.CreateRealisticUser();
    
    /// <summary>
    /// Устанавливает ID пользователя
    /// <tags>builders, user, id, fluent-api</tags>
    /// </summary>
    public UserBuilder WithId(long userId)
    {
        _user.Id = userId;
        return this;
    }
    
    /// <summary>
    /// Устанавливает username пользователя
    /// <tags>builders, user, username, fluent-api</tags>
    /// </summary>
    public UserBuilder WithUsername(string username)
    {
        _user.Username = username;
        return this;
    }
    
    /// <summary>
    /// Устанавливает имя пользователя
    /// <tags>builders, user, firstname, fluent-api</tags>
    /// </summary>
    public UserBuilder WithFirstName(string firstName)
    {
        _user.FirstName = firstName;
        return this;
    }
    
    /// <summary>
    /// Устанавливает пользователя как бота
    /// <tags>builders, user, bot, fluent-api</tags>
    /// </summary>
    public UserBuilder AsBot()
    {
        _user.IsBot = true;
        _user.FirstName = "TestBot";
        _user.Username = "test_bot";
        return this;
    }
    
    /// <summary>
    /// Устанавливает пользователя как обычного пользователя
    /// <tags>builders, user, regular, fluent-api</tags>
    /// </summary>
    public UserBuilder AsRegularUser()
    {
        _user.IsBot = false;
        return this;
    }
    
    /// <summary>
    /// Устанавливает пользователя как подозрительного
    /// <tags>builders, user, suspicious, fluent-api</tags>
    /// </summary>
    public UserBuilder AsSuspicious()
    {
        _user.FirstName = "SuspiciousUser";
        _user.Username = "suspicious_user_123";
        _user.IsBot = false;
        return this;
    }
    
    /// <summary>
    /// Устанавливает пользователя как нарушающего правила
    /// <tags>builders, user, violating, fluent-api</tags>
    /// </summary>
    public UserBuilder AsViolating()
    {
        _user.FirstName = "SpammerUser";
        _user.Username = "spammer_bot_fake";
        _user.IsBot = false;
        return this;
    }
    
    /// <summary>
    /// Устанавливает пользователя как нового
    /// <tags>builders, user, new, fluent-api</tags>
    /// </summary>
    public UserBuilder AsNew()
    {
        _user.FirstName = "NewUser";
        _user.Username = "new_user_" + DateTime.UtcNow.Ticks;
        _user.IsBot = false;
        return this;
    }
    
    /// <summary>
    /// Устанавливает пользователя с очень длинным именем для тестирования бана
    /// <tags>builders, user, long-name, ban-test, fluent-api</tags>
    /// </summary>
    public UserBuilder WithExtremelyLongName()
    {
        _user.FirstName = "ОченьДлинноеИмяПользователяКотороеПревышаетДопустимуюДлинуИДолжноБытьЗаблокированоПоПравиламМодерации";
        _user.LastName = "ОченьДлиннаяФамилияПользователяКотороеПревышаетДопустимуюДлинуИДолжноБытьЗаблокированоПоПравиламМодерации";
        _user.Username = "very_long_username_that_exceeds_maximum_length_and_should_be_banned_by_moderation_rules";
        return this;
    }
    
    /// <summary>
    /// Строит пользователя
    /// <tags>builders, user, build, fluent-api</tags>
    /// </summary>
    public User Build() => _user;
    
    /// <summary>
    /// Неявное преобразование в User
    /// <tags>builders, user, conversion, fluent-api</tags>
    /// </summary>
    public static implicit operator User(UserBuilder builder) => builder.Build();
} 