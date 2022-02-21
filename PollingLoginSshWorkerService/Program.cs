using PollingLoginSshWorkerService;

using var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => { options.ServiceName = "PollingLoginSshWorker Service"; })
    .ConfigureAppConfiguration(builder =>
    {
        builder.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<WindowsBackgroundService>();
        services.AddSingleton<SessionService>();
        services.AddSingleton<IAppSettings, AppSettings>();
    })
    .Build();

await host.RunAsync();