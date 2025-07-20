using System.Diagnostics;
using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using ClubDoorman.Infrastructure;

namespace ClubDoorman.Services;

internal class MessageData
{
    [LoadColumn(0)]
    public string Text { get; set; } = "";

    [LoadColumn(1)]
    public bool Label { get; set; }
}

internal class MessagePrediction : MessageData
{
    public float Score { get; set; }
    public bool PredictedLabel { get; set; }
}

public class SpamHamClassifier
{
    public SpamHamClassifier(ILogger<SpamHamClassifier> logger)
    {
        _logger = logger;
        Task.Run(Train);
        Task.Run(RetrainLoop);
    }

    private const string SpamHamDataset = "data/spam-ham.txt";
    private readonly SemaphoreSlim _datasetLock = new(1);
    private readonly SemaphoreSlim _predictionLock = new(1);
    private readonly ILogger<SpamHamClassifier> _logger;
    private readonly MLContext _mlContext = new();
    private PredictionEngine<MessageData, MessagePrediction>? _engine;
    private bool _needsRetraining;

    public void Touch()
    {
        _logger.LogDebug("Touch");
    }

    private async Task RetrainLoop()
    {
        _logger.LogInformation("RetrainLoop запущен - переобучение каждые 5 минут при необходимости");
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(5));
            if (_needsRetraining)
            {
                _logger.LogInformation("🔄 Запускаем переобучение модели (новые данные добавлены)");
                await Train();
                _needsRetraining = false;
                _logger.LogInformation("✅ Переобучение завершено, модель обновлена");
            }
            else
            {
                _logger.LogDebug("Переобучение не требуется");
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    public async Task<(bool Spam, float Score)> IsSpam(string message)
    {
        using var token = await SemaphoreHelper.AwaitAsync(_predictionLock);
        var msg = new MessageData { Text = message.ReplaceLineEndings(" ") };
        
        if (_engine == null)
        {
            // PERFORMANCE OPTIMIZATION - Consider using LoggerMessage delegate for better performance
            _logger.LogWarning("ML движок не инициализирован! Жду инициализации с таймаутом...");
            
            // Ждем инициализации с таймаутом 10 секунд
            var timeout = TimeSpan.FromSeconds(10);
            var sw = Stopwatch.StartNew();
            
            while (_engine == null && sw.Elapsed < timeout)
            {
                await Task.Delay(100);
            }
            
            if (_engine == null)
            {
                _logger.LogError("ML движок не инициализирован за {Timeout}ms! Возвращаем fallback результат", timeout.TotalMilliseconds);
                // BUSINESS LOGIC - Fallback assumes 'not spam' for safety, consider configurable behavior
                return (false, 0.0f); // Fallback: считаем не спамом
            }
            
            _logger.LogInformation("ML движок инициализирован за {Elapsed}ms", sw.ElapsedMilliseconds);
        }
        
        var predict = _engine.Predict(msg);
        _logger.LogDebug("ML предсказание: текст='{Text}', предсказано={Predicted}, скор={Score}", 
            message.Length > 50 ? message.Substring(0, 50) + "..." : message, 
            predict.PredictedLabel, predict.Score);
            
        return (predict.PredictedLabel, predict.Score);
    }

    public Task AddSpam(string message) 
    {
        _logger.LogInformation("📝 Добавляем СПАМ в датасет: '{Message}'", message.Length > 100 ? message.Substring(0, 100) + "..." : message);
        return AddSpamHam(message, true);
    }

    public Task AddHam(string message) 
    {
        _logger.LogInformation("📝 Добавляем НЕ-СПАМ в датасет: '{Message}'", message.Length > 100 ? message.Substring(0, 100) + "..." : message);
        return AddSpamHam(message, false);
    }

    private async Task AddSpamHam(string message, bool spam)
    {
        message = message.ReplaceLineEndings(" ");
        message = message.Replace("\"", "\"\"");
        var csvLine = $"\"{message}\", {spam}";
        
        using var token = await SemaphoreHelper.AwaitAsync(_datasetLock);
        var utf8WithoutBom = new UTF8Encoding(false);
        await File.AppendAllLinesAsync(SpamHamDataset, [csvLine], utf8WithoutBom);
        
        _needsRetraining = true;
        _logger.LogDebug("✅ Пример добавлен в файл {File}, установлен флаг переобучения", SpamHamDataset);
    }

    private async Task Train()
    {
        try
        {
            _logger.LogInformation("Начинаем обучение ML модели...");
            using var token = await SemaphoreHelper.AwaitAsync(_datasetLock);
            var sw = Stopwatch.StartNew();
            
            if (!File.Exists(SpamHamDataset))
            {
                _logger.LogError("Файл датасета {File} не найден!", SpamHamDataset);
                return;
            }
            
            var stopWords = (await File.ReadAllTextAsync("data/exclude-tokens.txt")).Split(',').Select(x => x.Trim()).ToArray();
            _logger.LogDebug("Загружено {Count} стоп-слов", stopWords.Length);

            List<MessageData> dataset;
            using (var reader = new StreamReader(SpamHamDataset))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                dataset = csv.GetRecords<MessageData>().ToList();
            }
            
            _logger.LogInformation("Загружено {Count} записей из датасета", dataset.Count);
            
            var spamCount = dataset.Count(x => x.Label);
            var hamCount = dataset.Count(x => !x.Label);
            _logger.LogInformation("Спам: {SpamCount}, НЕ спам: {HamCount}", spamCount, hamCount);

            foreach (var item in dataset)
                item.Text = TextProcessor.NormalizeText(item.Text);
            var data = _mlContext.Data.LoadFromEnumerable(dataset);

            _logger.LogDebug("Создаем pipeline для обучения...");
            var pipeline = _mlContext
                .Transforms.Text.FeaturizeText(
                    "Features",
                    new TextFeaturizingEstimator.Options
                    {
                        StopWordsRemoverOptions = new CustomStopWordsRemovingEstimator.Options { StopWords = stopWords },
                        WordFeatureExtractor = new WordBagEstimator.Options
                        {
                            Weighting = NgramExtractingEstimator.WeightingCriteria.TfIdf,
                            NgramLength = 2,
                            UseAllLengths = true
                        },
                        KeepPunctuations = false
                    },
                    "Text"
                )
                .AppendCacheCheckpoint(_mlContext)
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());

            _logger.LogDebug("Обучаем модель...");
            var model = pipeline.Fit(data);
            _engine = _mlContext.Model.CreatePredictionEngine<MessageData, MessagePrediction>(model);
            _logger.LogInformation("✅ ML модель успешно обучена за {Elapsed}ms! Движок готов к работе.", sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "❌ КРИТИЧЕСКАЯ ОШИБКА: Не удалось обучить ML модель!");
            _engine = null;
        }
    }
}
