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