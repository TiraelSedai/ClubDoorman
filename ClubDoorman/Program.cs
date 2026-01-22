using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Telegram.Bot;

namespace ClubDoorman;

public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(new CompactJsonFormatter())
                .CreateLogger();

            InitData();
            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog(
                    (context, services, loggerConfig) =>
                    {
                        loggerConfig
                            .MinimumLevel.Debug()
                            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                            .Enrich.FromLogContext()
                            .WriteTo.Console(new CompactJsonFormatter());
                    }
                )
                .ConfigureServices(services =>
                {
                    services.AddHostedService<Worker>();
                    services.AddSingleton<Config>();
                    services.AddSingleton<ITelegramBotClient>(provider => new TelegramBotClient(
                        provider.GetRequiredService<Config>().BotApi
                    ));
                    services.AddSingleton<CaptchaManager>();
                    services.AddSingleton<MessageProcessor>();
                    services.AddSingleton<StatisticsReporter>();
                    services.AddSingleton<SpamHamClassifier>();
                    services.AddSingleton<UserManager>();
                    services.AddSingleton<AdminCommandHandler>();
                    services.AddSingleton<ReactionHandler>();
                    services.AddSingleton<BadMessageManager>();
                    services.AddSingleton<AiChecks>();
                    services.AddSingleton<RecentMessagesStorage>();
                    services.AddDbContext<AppDbContext>(opts => opts.UseSqlite("Data Source=data/app.db"));
                    services.AddHybridCache();
                })
                .Build();

            using (var scope = host.Services.CreateScope())
            {
                using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
                SeedData(db);
            }

            await host.RunAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine("Unhandled exception in Main");
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
    }

    private static void InitData()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var dataInit = Path.Combine(basePath, "data_init");
        if (!Directory.Exists(dataInit))
            return;

        var data = Path.Combine(basePath, "data");
        if (!Directory.Exists(data))
            Directory.CreateDirectory(data);
        foreach (var sourceFullPath in Directory.EnumerateFiles(dataInit))
        {
            var file = Path.GetFileName(sourceFullPath);
            var target = Path.Combine(data, file);
            if (!File.Exists(target))
                File.Copy(sourceFullPath, target);
        }
    }

    private static void SeedData(AppDbContext db)
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;

        if (!db.SpamHamRecords.Any())
        {
            var dataFile = Path.Combine(basePath, "data", "spam-ham.txt");
            if (File.Exists(dataFile))
            {
                Log.Information("No spam-ham records detected in the database, starting initial seeding from text file");
                using var reader = new StreamReader(dataFile);
                using var csv = new CsvReader(
                    reader,
                    new CsvConfiguration(CultureInfo.InvariantCulture) { PrepareHeaderForMatch = args => args.Header.ToLower() }
                );

                var records = csv.GetRecords<CsvRow>().Select(r => new SpamHamRecord { Text = r.Text, IsSpam = r.Label });

                db.SpamHamRecords.AddRange(records);
                db.SaveChanges();
                Log.Information("Seeded SpamHamRecords from {File}", dataFile);
            }
        }

        if (!db.ApprovedUsers.Any())
        {
            var dataFile = Path.Combine(basePath, "data", "approved-users.txt");
            if (File.Exists(dataFile))
            {
                Log.Information("No approved users detected in the database, starting initial seeding from text file");
                var userIds = File.ReadAllLines(dataFile)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => new ApprovedUser { Id = long.Parse(line) });

                db.ApprovedUsers.AddRange(userIds);
                db.SaveChanges();
                Log.Information("Seeded ApprovedUsers from {File}", dataFile);
            }
        }
    }

    private sealed record CsvRow(string Text, bool Label);
}
