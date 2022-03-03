using System.Collections.Specialized;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Common.Model;
using DeviceId;
using Newtonsoft.Json;

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
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
        socket.Connect("8.8.8.8", 65530);
        var endPoint = socket.LocalEndPoint as IPEndPoint;
        return endPoint?.Address.ToString() ?? "";
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

    private string GetDeviceConnectionString(string baseUri)
    {
        NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
        queryString.Add("Location", _appSettings.Location);
        queryString.Add("DeviceId", GetDeviceId());
        queryString.Add("IpAddress", GetLocalIPAddress());
        queryString.Add("MacAddress", GetMacAddress());
        queryString.Add("MachineName", Environment.MachineName);
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

    public async Task SyncAzureIoTHub(bool isConnected, string lastErrorMessage)
    {
        var sessionApiUrl = _appSettings.AzureFunctionBaseUrl + GetSessionFunction;
        try
        {
            if (string.IsNullOrEmpty(_deviceConnectionString) || _client == null)
            {
                _deviceConnectionString = string.IsNullOrEmpty(_deviceConnectionString) ? GetDeviceConnectionString(sessionApiUrl) : _deviceConnectionString;

                _client = DeviceClient.CreateFromConnectionString(_deviceConnectionString, TransportType.Mqtt);
                _logger.LogInformation("After Mqtt");
                _client.SetMethodHandlerAsync(nameof(OnNewSshMessage), OnNewSshMessage, null).Wait();
                _client.SetMethodHandlerAsync(nameof(OnRemoveSshMessage), OnRemoveSshMessage, null).Wait();

                var twin = await _client.GetTwinAsync();
                twin.Properties.Reported = new TwinCollection
                {
                    ["isSshConnected"] = false,
                    ["lastErrorMessage"] = ""
                };
                _reportedProperties = twin.Properties.Reported;
                await _client?.UpdateReportedPropertiesAsync(_reportedProperties)!;
                _logger.LogInformation("Connected to hub");
            }
            //Update if changed.
            if (_reportedProperties?["isSshConnected"] != isConnected ||
                _reportedProperties?["lastErrorMessage"] != lastErrorMessage)
            {
                var twin = await _client.GetTwinAsync();
                twin.Properties.Reported = new TwinCollection
                {
                    ["isSshConnected"] = isConnected,
                    ["lastErrorMessage"] = lastErrorMessage
                };
                _reportedProperties = twin.Properties.Reported;
                await _client?.UpdateReportedPropertiesAsync(_reportedProperties)!;
                _logger.LogInformation(twin.ToJson(Formatting.Indented));
                _logger.LogInformation("After UpdateReportedPropertiesAsync");
            }
        }
        catch (Exception ex)
        {
            _deviceConnectionString = "";
            _logger.LogError("Cannot access: " + sessionApiUrl);
            _logger.LogError("ex message: " + ex?.Message);

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