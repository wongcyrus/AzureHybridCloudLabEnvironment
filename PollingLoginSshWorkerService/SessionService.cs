using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common.Model;
using DeviceId;

namespace PollingLoginSshWorkerService;



using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;


public class SessionService : IDisposable
{
    private const string GetSessionFunction = "/api/GetSessionFunction";
    private readonly IAppSettings _appSettings;

    private readonly ILogger<WindowsBackgroundService> _logger;

    private string? _deviceConnectionString;
    private DeviceClient? _client;
    public Session? Session { get; private set; }
    private TwinCollection? _reportedProperties;

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

    private string GetAsync(string baseUri)
    {
        NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
        queryString.Add("Location", _appSettings.Location);
        queryString.Add("DeviceId", GetDeviceId());
        queryString.Add("IpAddress", GetLocalIPAddress());
        queryString.Add("MacAddress", GetMacAddress());
        queryString.Add("MachineName", Environment.MachineName);
        queryString.Add("isOnline", false.ToString());
        queryString.Add("isConnected", false.ToString());
        queryString.Add("LastErrorMessage", "");
        queryString.Add("code", _appSettings.GetSessionFunctionKey);

        var uri = baseUri + "?" + queryString;
        _logger.LogInformation(uri);
        using var httpClient = new HttpClient(new HttpClientHandler
        { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
        var response = httpClient.GetAsync(uri).Result;
        response.EnsureSuccessStatusCode();
        var result = response.Content.ReadAsStringAsync().Result;
        return result;
    }

    public void SyncAzureIoTHub(bool isConnected, string lastErrorMessage)
    {
        var sessionApiUrl = _appSettings.AzureFunctionBaseUrl + GetSessionFunction;
        try
        {
            if (string.IsNullOrEmpty(_deviceConnectionString) || _client == null)
            {
                _deviceConnectionString = GetAsync(sessionApiUrl);
                _client = DeviceClient.CreateFromConnectionString(_deviceConnectionString, TransportType.Mqtt);
                _client.SetMethodHandlerAsync(nameof(OnNewSshMessage), OnNewSshMessage, null).Wait();
                _client.SetMethodHandlerAsync(nameof(OnRemoveSshMessage), OnRemoveSshMessage, null).Wait();
                _logger.LogInformation("Connecting to hub");
            }

            if (_reportedProperties?["isSshConnected"] != isConnected ||
                _reportedProperties?["lastErrorMessage"] != lastErrorMessage)
            {
                _reportedProperties = new TwinCollection
                {
                    ["isSshConnected"] = isConnected,
                    ["lastErrorMessage"] = lastErrorMessage
                };
                _client.UpdateReportedPropertiesAsync(_reportedProperties).Wait();
            }
        }
        catch (Exception ex)
        {
            _deviceConnectionString = "";
            _logger.LogError("Cannot access: " + sessionApiUrl);
            _logger.LogError(ex.ToString());
        }
    }

    public Task<MethodResponse> OnNewSshMessage(MethodRequest methodRequest, object userContext)
    {
        try
        {
            var payload = methodRequest.DataAsJson;
            _logger.LogInformation($"Payload: {payload}");
            Session = Session.FromJson(payload, _logger);
            // Update device twin with reboot time. 
            var lastSsh = new TwinCollection();
            var connect = new TwinCollection();
            var reportedProperties = new TwinCollection();
            lastSsh["lastSsh"] = payload;
            connect["ssh"] = lastSsh;
            reportedProperties["iothubDM"] = connect;
            _client?.UpdateReportedPropertiesAsync(reportedProperties).Wait();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in sample: {0}", ex.Message);
        }

        string result = @"{""result"":""Connected.""}";
        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
    }

    public Task<MethodResponse> OnRemoveSshMessage(MethodRequest methodRequest, object userContext)
    {
        try
        {
            var payload = methodRequest.DataAsJson;
            _logger.LogInformation($"Payload: {payload}");
            Session = null;
            // Update device twin with reboot time. 
            var lastSsh = new TwinCollection();
            var disconnect = new TwinCollection();
            var reportedProperties = new TwinCollection();
            lastSsh["lastSsh"] = payload;
            disconnect["ssh"] = lastSsh;
            reportedProperties["iothubDM"] = disconnect;
            _client?.UpdateReportedPropertiesAsync(reportedProperties).Wait();
        }
        catch (Exception ex)
        {

            _logger.LogError("Error in sample: {0}", ex.Message);
        }

        string result = @"{""result"":""Disconnected.""}";
        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
    }


    public void Dispose()
    {
        _reportedProperties = new TwinCollection
        {
            ["IsSshConnected"] = false
        };
        _client?.UpdateReportedPropertiesAsync(_reportedProperties).Wait();
        _client?.SetMethodHandlerAsync(nameof(OnNewSshMessage), null, null).Wait();
        _client?.SetMethodHandlerAsync(nameof(OnRemoveSshMessage), null, null).Wait();
        _client?.CloseAsync().Wait();
        _client?.Dispose();
    }
}