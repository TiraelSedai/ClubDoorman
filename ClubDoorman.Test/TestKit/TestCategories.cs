namespace ClubDoorman.Test.TestKit;

/// <summary>
/// Категории тестов для оптимизации CI/CD и параллельного выполнения
/// Основаны на существующих категориях в проекте
/// </summary>
public static class TestCategories
{
    // === СУЩЕСТВУЮЩИЕ КАТЕГОРИИ ===
    
    /// <summary>
    /// Быстрые тесты (до 1 секунды) - выполняются в CI первыми
    /// </summary>
    public const string Fast = "fast";
    
    /// <summary>
    /// Критически важные тесты - выполняются всегда
    /// </summary>
    public const string Critical = "critical";
    
    /// <summary>
    /// Unit тесты - изолированные, только с моками
    /// </summary>
    public const string Unit = "unit";
    
    /// <summary>
    /// Интеграционные тесты - с реальными компонентами
    /// </summary>
    public const string Integration = "integration";
    
    /// <summary>
    /// End-to-end тесты - полные сценарии
    /// </summary>
    public const string E2E = "e2e";
    
    /// <summary>
    /// BDD тесты - поведенческие тесты с SpecFlow
    /// </summary>
    public const string BDD = "BDD";
    
    /// <summary>
    /// Тесты с реальными API - медленные, с внешними сервисами
    /// </summary>
    public const string RealApi = "real-api";
    
    /// <summary>
    /// Демонстрационные тесты - показывают возможности инфраструктуры
    /// </summary>
    public const string Demo = "demo";
    
    /// <summary>
    /// Тесты инфраструктуры - фабрики, моки, утилиты
    /// </summary>
    public const string TestInfrastructure = "test-infrastructure";
    
    /// <summary>
    /// Тесты бизнес-логики - основная функциональность
    /// </summary>
    public const string BusinessLogic = "business-logic";
    
    // === ДОМЕННЫЕ КАТЕГОРИИ ===
    
    /// <summary>
    /// Тесты обработчиков сообщений
    /// </summary>
    public const string Handlers = "handlers";
    
    /// <summary>
    /// Тесты сервисов
    /// </summary>
    public const string Services = "services";
    
    /// <summary>
    /// Тесты модерации
    /// </summary>
    public const string Moderation = "moderation";
    
    /// <summary>
    /// Тесты AI компонентов
    /// </summary>
    public const string Ai = "ai";
    
    /// <summary>
    /// Тесты AI анализа
    /// </summary>
    public const string AiAnalysis = "ai-analysis";
    
    /// <summary>
    /// Тесты AI фото
    /// </summary>
    public const string AiPhoto = "ai-photo";
    
    /// <summary>
    /// Тесты ML компонентов
    /// </summary>
    public const string ML = "ml";
    
    /// <summary>
    /// Тесты капчи
    /// </summary>
    public const string Captcha = "captcha";
    
    /// <summary>
    /// Тесты Telegram API
    /// </summary>
    public const string Telegram = "telegram";
    
    /// <summary>
    /// Тесты статистики
    /// </summary>
    public const string Statistics = "statistics";
    
    /// <summary>
    /// Тесты бана пользователей
    /// </summary>
    public const string Ban = "ban";
    
    /// <summary>
    /// Тесты подозрительных пользователей
    /// </summary>
    public const string SuspiciousUsers = "suspicious-users";
    
    /// <summary>
    /// Тесты инфраструктуры
    /// </summary>
    public const string Infrastructure = "infrastructure";
    
    /// <summary>
    /// Тесты обработки ошибок
    /// </summary>
    public const string ErrorHandling = "ErrorHandling";
    
    /// <summary>
    /// Тесты производительности
    /// </summary>
    public const string Performance = "performance";
    
    // === НОВЫЕ КАТЕГОРИИ ДЛЯ ОПТИМИЗАЦИИ ===
    
    /// <summary>
    /// Медленные тесты (1-5 секунд) - выполняются отдельно
    /// </summary>
    public const string Slow = "slow";
    
    /// <summary>
    /// Нестабильные тесты - могут падать время от времени
    /// </summary>
    public const string Flaky = "flaky";
    
    /// <summary>
    /// AutoFixture тесты - демонстрация возможностей
    /// </summary>
    public const string AutoFixture = "autofixture";
} 