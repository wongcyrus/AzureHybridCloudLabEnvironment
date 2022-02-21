namespace PollingLoginSshWorkerService
{
    public class AppSettings: IAppSettings
    {
        protected readonly IConfiguration _configuration;

        public AppSettings(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public const string Section = "Config";

        public string AzureFunctionBaseUrl => _configuration.GetSection(AppSettings.Section)[nameof(AzureFunctionBaseUrl)];

        public string Location => _configuration.GetSection(AppSettings.Section)[nameof(Location)];
    }

    public interface IAppSettings
    {
        string AzureFunctionBaseUrl { get; }
        string Location { get;  }
    }
}
