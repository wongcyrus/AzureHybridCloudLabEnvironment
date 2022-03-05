namespace IoTSshClientService;

public class AppSettings : IAppSettings
{
    public const string Section = "Config";
    protected readonly IConfiguration Configuration;

    public AppSettings(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public string AzureFunctionBaseUrl => Configuration.GetSection(Section)[nameof(AzureFunctionBaseUrl)];
    public string GetDeviceConnectionStringFunction => Configuration.GetSection(Section)[nameof(GetDeviceConnectionStringFunction)];
    public string Location => Configuration.GetSection(Section)[nameof(Location)];
}

public interface IAppSettings
{
    string AzureFunctionBaseUrl { get; }
    string GetDeviceConnectionStringFunction { get; }
    string Location { get; }
}