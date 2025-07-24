using System.Diagnostics;
using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;

namespace ClubDoorman;

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
    private readonly Lock _predictionLock = new();
    private readonly ILogger<SpamHamClassifier> _logger;
    private readonly MLContext _mlContext = new();
    private PredictionEngine<MessageData, MessagePrediction>? _engine;
    private bool _needsRetraining;
    private readonly PeriodicTimer _retrainTimer = new(TimeSpan.FromMinutes(1));

    private async Task RetrainLoop()
    {
        while (true)
        {
            await _retrainTimer.WaitForNextTickAsync();
            if (_needsRetraining)
            {
                await Train();
                _needsRetraining = false;
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    public async Task<(bool Spam, float Score)> IsSpam(string message)
    {
        var msg = new MessageData { Text = message.ReplaceLineEndings(" ") };
        while (_engine == null)
            await Task.Delay(50);
        lock (_predictionLock)
        {
            var predict = _engine.Predict(msg);
            return (predict.PredictedLabel, predict.Score);
        }
    }

    public Task AddSpam(string message) => AddSpamHam(message, true);

    public Task AddHam(string message) => AddSpamHam(message, false);

    private async Task AddSpamHam(string message, bool spam)
    {
        message = message.ReplaceLineEndings(" ");
        message = message.Replace("\"", "\"\"");
        message = $"\"{message}\", {spam}";
        using var token = await SemaphoreHelper.AwaitAsync(_datasetLock);
        var utf8WithoutBom = new UTF8Encoding(false);
        await File.AppendAllLinesAsync(SpamHamDataset, [message], utf8WithoutBom);
        _needsRetraining = true;
    }

    private async Task Train()
    {
        try
        {
            using var token = await SemaphoreHelper.AwaitAsync(_datasetLock);
            var sw = Stopwatch.StartNew();
            var stopWords = (await File.ReadAllTextAsync("data/exclude-tokens.txt")).Split(',').Select(x => x.Trim()).ToArray();

            List<MessageData> dataset;
            using (var reader = new StreamReader(SpamHamDataset))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                dataset = csv.GetRecords<MessageData>().ToList();

            foreach (var item in dataset)
                item.Text = TextProcessor.NormalizeText(item.Text);
            var data = _mlContext.Data.LoadFromEnumerable(dataset);

            var pipeline = _mlContext
                .Transforms.Text.FeaturizeText(
                    "Features",
                    new TextFeaturizingEstimator.Options
                    {
                        StopWordsRemoverOptions = new CustomStopWordsRemovingEstimator.Options { StopWords = stopWords },
                        WordFeatureExtractor = new WordBagEstimator.Options
                        {
                            Weighting = NgramExtractingEstimator.WeightingCriteria.TfIdf,
                            NgramLength = 3,
                            UseAllLengths = true,
                        },
                        KeepPunctuations = false,
                    },
                    "Text"
                )
                .AppendCacheCheckpoint(_mlContext)
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());

            var model = pipeline.Fit(data);
            _engine = _mlContext.Model.CreatePredictionEngine<MessageData, MessagePrediction>(model);
            _logger.LogDebug("Train ok in {Elapsed}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception during training");
        }
    }
}
