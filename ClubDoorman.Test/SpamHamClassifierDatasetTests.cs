using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClubDoorman.Test;

public sealed class SpamHamClassifierDatasetTests
{
    [Test]
    public async Task GetLatestSpamHamRecords_ReturnsNewestRecordsByDescendingId()
    {
        await using var fixture = await SpamHamClassifierFixture.Create();
        await fixture.AddRecords(
            new SpamHamRecord { Text = "old ham", IsSpam = false },
            new SpamHamRecord { Text = "middle ham", IsSpam = false },
            new SpamHamRecord { Text = "new spam", IsSpam = true }
        );

        var records = await fixture.Classifier.GetLatestSpamHamRecords(2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(records.Select(x => x.Text), Is.EqualTo(new[] { "new spam", "middle ham" }));
            Assert.That(records.Select(x => x.Id), Is.Ordered.Descending);
        }
    }

    [Test]
    public async Task GetLatestSpamHamRecords_ReturnsEmptyListWhenCountIsLessThanOne()
    {
        await using var fixture = await SpamHamClassifierFixture.Create();
        await fixture.AddRecords(new SpamHamRecord { Text = "kept ham", IsSpam = false });

        var records = await fixture.Classifier.GetLatestSpamHamRecords(0);

        Assert.That(records, Is.Empty);
    }

    [Test]
    public async Task DeleteSpamHamRecord_RemovesMatchingRecordAndReturnsIt()
    {
        await using var fixture = await SpamHamClassifierFixture.Create();
        var recordToDelete = new SpamHamRecord { Text = "wrong spam", IsSpam = true };
        await fixture.AddRecords(
            new SpamHamRecord { Text = "old ham", IsSpam = false },
            recordToDelete,
            new SpamHamRecord { Text = "new ham", IsSpam = false }
        );

        var deleted = await fixture.Classifier.DeleteSpamHamRecord(recordToDelete.Id);
        var remaining = await fixture.Classifier.GetLatestSpamHamRecords(10);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.Not.Null);
            Assert.That(deleted!.Id, Is.EqualTo(recordToDelete.Id));
            Assert.That(deleted.Text, Is.EqualTo("wrong spam"));
            Assert.That(remaining.Select(x => x.Text), Is.EqualTo(new[] { "new ham", "old ham" }));
        }
    }

    [Test]
    public async Task DeleteSpamHamRecord_ReturnsNullWhenRecordDoesNotExist()
    {
        await using var fixture = await SpamHamClassifierFixture.Create();
        await fixture.AddRecords(new SpamHamRecord { Text = "kept ham", IsSpam = false });

        var deleted = await fixture.Classifier.DeleteSpamHamRecord(999);
        var remaining = await fixture.Classifier.GetLatestSpamHamRecords(10);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.Null);
            Assert.That(remaining.Select(x => x.Text), Is.EqualTo(new[] { "kept ham" }));
        }
    }

    private sealed class SpamHamClassifierFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _serviceProvider;

        private SpamHamClassifierFixture(SqliteConnection connection, ServiceProvider serviceProvider, SpamHamClassifier classifier)
        {
            _connection = connection;
            _serviceProvider = serviceProvider;
            Classifier = classifier;
        }

        public SpamHamClassifier Classifier { get; }

        public static async Task<SpamHamClassifierFixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var serviceProvider = new ServiceCollection()
                .AddDbContext<AppDbContext>(options => options.UseSqlite(connection))
                .BuildServiceProvider();

            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.EnsureCreatedAsync();
            }

            var classifier = new SpamHamClassifier(
                NullLogger<SpamHamClassifier>.Instance,
                serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                SpamHamClassifierStartupMode.SkipBackgroundTraining
            );
            return new SpamHamClassifierFixture(connection, serviceProvider, classifier);
        }

        public async Task AddRecords(params SpamHamRecord[] records)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SpamHamRecords.AddRange(records);
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Classifier.Dispose();
            await _serviceProvider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
