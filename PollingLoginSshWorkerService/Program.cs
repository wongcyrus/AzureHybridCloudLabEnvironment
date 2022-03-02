using System.Text;
using PollingLoginSshWorkerService;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;


using var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => { options.ServiceName = "PollingLoginSshWorker Service"; })
    .ConfigureAppConfiguration(builder =>
    {
        builder.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production"}.json", true);
    }).
    UseSerilog((hostingContext, loggerConfiguration) =>
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        const string loggerTemplate = @"{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u4}] [{SourceContext:l}] {Message:lj}{NewLine}{Exception}";

        loggerConfiguration
            .Enrich.FromLogContext() //Adds more information to our logs from built in Serilog 
            .Enrich.WithMachineName()
            .WriteTo.Console(LogEventLevel.Information, loggerTemplate, theme: AnsiConsoleTheme.Literate)
            .WriteTo.File(Path.Combine(baseDir, "logs", "error.txt"), LogEventLevel.Warning, loggerTemplate,
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .WriteTo.File(Path.Combine(baseDir, "logs", "info.txt"), LogEventLevel.Information, loggerTemplate,
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);

    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<WindowsBackgroundService>();
        services.AddSingleton<SessionService>();
        services.AddSingleton<IAppSettings, AppSettings>();
    })
    .Build();

await host.RunAsync();