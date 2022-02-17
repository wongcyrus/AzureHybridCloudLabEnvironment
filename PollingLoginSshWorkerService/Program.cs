using PollingLoginSshWorkerService;

using var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => { options.ServiceName = "PollingLoginSshWorker Service"; })
    .ConfigureServices(services =>
    {
        services.AddHostedService<WindowsBackgroundService>();
        services.AddHttpClient<SessionService>();
    })
    .Build();

await host.RunAsync();