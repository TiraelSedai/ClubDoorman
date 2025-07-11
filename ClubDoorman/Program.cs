using Serilog;
using Serilog.Events;

namespace ClubDoorman;

public class Program
{
    public static async Task Main(string[] args)
    {
        InitData();
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog(
                (_, _, config) =>
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
                services.AddSingleton<BadMessageManager>();
                
                // Условная регистрация системы одобрения
                if (Config.UseNewApprovalSystem)
                {
                    services.AddSingleton<ApprovedUsersStorageV2>();
                    services.AddSingleton<UserManagerV2>();
                    services.AddSingleton<IUserManager>(provider => provider.GetRequiredService<UserManagerV2>());
                }
                else
                {
                    services.AddSingleton<ApprovedUsersStorage>();
                    services.AddSingleton<UserManager>();
                    services.AddSingleton<IUserManager>(provider => provider.GetRequiredService<UserManager>());
                }
            })
            .Build();

        await host.RunAsync();
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
}
