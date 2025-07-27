# Анализ конфликта в AiChecksPhotoLoggingTest.cs

## Конфликт в методе Setup

### Ваша версия (HEAD):
```csharp
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
```

### Версия Мамая (upstream/next-dev):
```csharp
// Загружаем .env файл
DotNetEnv.Env.Load("ClubDoorman/.env");
```

## Анализ различий

### Ваша архитектура:
- Более гибкий поиск .env файла
- Явная загрузка переменных в Environment
- Отладочная информация
- Обработка ошибок

### Поведение Мамая:
- Простая загрузка из фиксированного пути
- Меньше кода

## Рекомендуемое решение

### Сохранить вашу версию:
```csharp
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
```

## Преимущества решения:
1. ✅ Сохраняется ваша улучшенная архитектура тестов
2. ✅ Лучшая обработка ошибок
3. ✅ Отладочная информация
4. ✅ Гибкость в поиске .env файла 