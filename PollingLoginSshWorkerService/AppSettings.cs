namespace PollingLoginSshWorkerService;

public class AppSettings : IAppSettings
{
    public const string Section = "Config";
    protected readonly IConfiguration Configuration;

    public AppSettings(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public string AzureFunctionBaseUrl => Configuration.GetSection(Section)[nameof(AzureFunctionBaseUrl)];
    public string Key => Configuration.GetSection(Section)[nameof(Key)];
    public string Location => Configuration.GetSection(Section)[nameof(Location)];
}

public interface IAppSettings
{
    string AzureFunctionBaseUrl { get; }
    string Key { get; }
    string Location { get; }
}