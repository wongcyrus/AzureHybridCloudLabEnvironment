using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using Common.Model;
using DeviceId;

namespace PollingLoginSshWorkerService;

public class SessionService
{
    private const string GetSessionFunction = "/api/GetSessionFunction";
    private readonly IAppSettings _appSettings;

    private readonly ILogger<WindowsBackgroundService> _logger;


    public SessionService(ILogger<WindowsBackgroundService> logger, IAppSettings appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    private string GetDeviceId()
    {
        return new DeviceIdBuilder()
            .AddMachineName()
            .AddMacAddress()
            .ToString();
    }

    private string GetMacAddress()
    {
        return new DeviceIdBuilder()
            .AddMacAddress()
            .ToString();
    }

    private string GetAsync(string baseUri, bool isConnected, string lastErrorMessage)
    {
        NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
        queryString.Add("Location", _appSettings.Location);
        queryString.Add("DeviceId", GetDeviceId());
        queryString.Add("IpAddress", GetLocalIPAddress());
        queryString.Add("MacAddress", GetMacAddress());
        queryString.Add("MachineName", Environment.MachineName);
        queryString.Add("isConnected", isConnected.ToString());
        queryString.Add("LastErrorMessage", lastErrorMessage);
        queryString.Add("code", _appSettings.Key);

        var uri = baseUri + "?" + queryString;
        _logger.LogInformation(uri);
        using var httpClient = new HttpClient(new HttpClientHandler
        { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
        var response = httpClient.GetAsync(uri).Result;
        response.EnsureSuccessStatusCode();
        var result = response.Content.ReadAsStringAsync().Result;
        return result;
    }

    public Session? GetSessionAsync(bool isConnected, string lastErrorMessage)
    {
        var sessionApiUrl = _appSettings.AzureFunctionBaseUrl + GetSessionFunction;
        try
        {
            // The API returns an array with a single entry.
            var result = GetAsync(sessionApiUrl, isConnected, lastErrorMessage);
            if (string.IsNullOrEmpty(result))
            {
                _logger.LogInformation("Empty session.");
                return null;
            }

            var session = JsonBase<Session>.FromJson(result, _logger);
            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError("Cannot access: " + sessionApiUrl);
            _logger.LogError(ex.ToString());
            return null;
        }
    }
}