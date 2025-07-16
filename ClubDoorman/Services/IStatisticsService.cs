using ClubDoorman.Models;

namespace ClubDoorman.Services;

/// <summary>
/// Сервис статистики
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Увеличивает счетчик капч
    /// </summary>
    void IncrementCaptcha(long chatId);

    /// <summary>
    /// Увеличивает счетчик банов из блэклиста
    /// </summary>
    void IncrementBlacklistBan(long chatId);

    /// <summary>
    /// Увеличивает счетчик известных плохих сообщений
    /// </summary>
    void IncrementKnownBadMessage(long chatId);

    /// <summary>
    /// Увеличивает счетчик банов за длинное имя
    /// </summary>
    void IncrementLongNameBan(long chatId);

    /// <summary>
    /// Получает статистику по всем чатам
    /// </summary>
    IDictionary<long, ChatStats> GetAllStats();

    /// <summary>
    /// Очищает статистику
    /// </summary>
    void ClearStats();

    /// <summary>
    /// Генерирует отчет по статистике
    /// </summary>
    Task<string> GenerateReportAsync();
} 