namespace PollingLoginSshWorkerService
{
    public class AppSettings: IAppSettings
    {
        protected readonly IConfiguration Configuration;

        public AppSettings(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public const string Section = "Config";

        public string AzureFunctionBaseUrl => Configuration.GetSection(AppSettings.Section)[nameof(AzureFunctionBaseUrl)];
        public string Key => Configuration.GetSection(AppSettings.Section)[nameof(Key)];

        public string Location => Configuration.GetSection(AppSettings.Section)[nameof(Location)];
    }

    public interface IAppSettings
    {
        string AzureFunctionBaseUrl { get; }
        string Key { get; }
        string Location { get;  }
    }
}
