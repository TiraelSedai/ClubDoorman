namespace ClubDoorman.Infrastructure;

/// <summary>
/// Базовое исключение для всех ошибок ClubDoorman
/// </summary>
public abstract class ClubDoormanException : Exception
{
    /// <summary>
    /// Инициализирует новый экземпляр исключения ClubDoorman
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    protected ClubDoormanException(string message) : base(message) { }

    /// <summary>
    /// Инициализирует новый экземпляр исключения ClubDoorman
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    /// <param name="innerException">Внутреннее исключение</param>
    protected ClubDoormanException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Исключение для ошибок модерации
/// </summary>
public class ModerationException : ClubDoormanException
{
    /// <summary>
    /// Инициализирует новый экземпляр исключения модерации
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    public ModerationException(string message) : base(message) { }

    /// <summary>
    /// Инициализирует новый экземпляр исключения модерации
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    /// <param name="innerException">Внутреннее исключение</param>
    public ModerationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Исключение для ошибок управления пользователями
/// </summary>
public class UserManagementException : ClubDoormanException
{
    /// <summary>
    /// Инициализирует новый экземпляр исключения управления пользователями
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    public UserManagementException(string message) : base(message) { }

    /// <summary>
    /// Инициализирует новый экземпляр исключения управления пользователями
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    /// <param name="innerException">Внутреннее исключение</param>
    public UserManagementException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Исключение для ошибок AI сервисов
/// </summary>
public class AiServiceException : ClubDoormanException
{
    /// <summary>
    /// Инициализирует новый экземпляр исключения AI сервиса
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    public AiServiceException(string message) : base(message) { }

    /// <summary>
    /// Инициализирует новый экземпляр исключения AI сервиса
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    /// <param name="innerException">Внутреннее исключение</param>
    public AiServiceException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Исключение для ошибок Telegram API
/// </summary>
public class TelegramApiException : ClubDoormanException
{
    /// <summary>
    /// Инициализирует новый экземпляр исключения Telegram API
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    public TelegramApiException(string message) : base(message) { }

    /// <summary>
    /// Инициализирует новый экземпляр исключения Telegram API
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    /// <param name="innerException">Внутреннее исключение</param>
    public TelegramApiException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Исключение для ошибок конфигурации
/// </summary>
public class ConfigurationException : ClubDoormanException
{
    /// <summary>
    /// Инициализирует новый экземпляр исключения конфигурации
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    public ConfigurationException(string message) : base(message) { }

    /// <summary>
    /// Инициализирует новый экземпляр исключения конфигурации
    /// </summary>
    /// <param name="message">Сообщение об ошибке</param>
    /// <param name="innerException">Внутреннее исключение</param>
    public ConfigurationException(string message, Exception innerException) : base(message, innerException) { }
} 