using Serilog;
using Serilog.Events;

namespace ClubDoorman;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog(
                (context, _, config) =>
                {
                    config
                        .MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .Enrich.FromLogContext()
                        .WriteTo.Async(a => a.Console());
                }
            )
            .ConfigureServices(services =>
            {
                services.AddHostedService<Worker>();
                services.AddSingleton<SpamHamClassifier>();
                services.AddSingleton<UserManager>();
            })
            .Build();

        await host.RunAsync();
    }
}
