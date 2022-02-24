using PollingLoginSshWorkerService;
using Serilog;

using var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => { options.ServiceName = "PollingLoginSshWorker Service"; })
    .ConfigureAppConfiguration(builder =>
    {
        builder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production"}.json", true);
        Log.Logger = new LoggerConfiguration() // initiate the logger configuration
            .ReadFrom.Configuration(builder.Build()) // connect serilog to our configuration folder
            .Enrich.FromLogContext() //Adds more information to our logs from built in Serilog 
            .CreateLogger(); //initialise the logger
    })
    .ConfigureLogging((loggingBuilder) =>
    {
        loggingBuilder.AddSerilog(Log.Logger, dispose: true);
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<WindowsBackgroundService>();
        services.AddSingleton<SessionService>();
        services.AddSingleton<IAppSettings, AppSettings>();
    })
    .Build();

await host.RunAsync();