namespace ClubDoorman;

internal class Consts
{
    // Пороговые значения для AI проверок профилей
    public const double LlmLowProbability = 0.75;   // Средняя вероятность спама - уведомление админам
    public const double LlmHighProbability = 0.9;   // Высокая вероятность - автоматическая модерация
    
    // UI элементы для админских кнопок
    public const string BanButton = "❌❌❌ ban";
    public const string OkButton = "✅✅✅ ok";
    
    // Прочие константы
    public const int BigChannelSubsCount = 1000;
} 