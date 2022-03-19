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

    public string GetDeviceConnectionStringFunctionKey =>
        Configuration.GetSection(Section)[nameof(GetDeviceConnectionStringFunctionKey)];

    public string Location => Configuration.GetSection(Section)[nameof(Location)];
}

public interface IAppSettings
{
    string AzureFunctionBaseUrl { get; }
    string GetDeviceConnectionStringFunctionKey { get; }
    string Location { get; }
}