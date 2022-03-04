using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
using Common.Model;
using DeviceId;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace PollingLoginSshWorkerService;

public class SessionService : IDisposable
{
    private const string GetSessionFunction = "/api/GetDeviceConnectionString";
    private readonly IAppSettings _appSettings;

    private readonly ILogger<WindowsBackgroundService> _logger;
    private DeviceClient? _client;

    private string? _deviceConnectionString;

    private int _failureCount;
    private TwinCollection? _reportedProperties;

    public SessionService(ILogger<WindowsBackgroundService> logger, IAppSettings appSettings)
    {
        _logger = logger;
        _appSettings = appSettings;
        _failureCount = 0;
    }

    public Session? CurrentSession { get; private set; }

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
        var queryString = HttpUtility.ParseQueryString(string.Empty);
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

    public async Task<int> SyncAzureIoTHub(bool isConnected, string lastErrorMessage)
    {
        var sessionApiUrl = _appSettings.AzureFunctionBaseUrl + GetSessionFunction;
        try
        {
            if (string.IsNullOrEmpty(_deviceConnectionString) || _client == null)
            {
                _deviceConnectionString = string.IsNullOrEmpty(_deviceConnectionString)
                    ? GetDeviceConnectionString(sessionApiUrl)
                    : _deviceConnectionString;

                _client = DeviceClient.CreateFromConnectionString(_deviceConnectionString, TransportType.Mqtt);
                _client.SetMethodHandlerAsync(nameof(OnNewSshMessage), OnNewSshMessage, null).Wait();
                _client.SetMethodHandlerAsync(nameof(OnRemoveSshMessage), OnRemoveSshMessage, null).Wait();
                await _client.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyUpdate, null);

                var twin = await _client.GetTwinAsync();
                twin.Properties.Reported = new TwinCollection
                {
                    ["isSshConnected"] = false,
                    ["lastErrorMessage"] = ""
                };
                _reportedProperties = twin.Properties.Reported;

                //Handle previous session in case of service or computer restart.
                if (twin!.Properties.Desired.Contains("session"))
                {
                    var previousSessionString = twin!.Properties.Desired["session"]!.Value as string;
                    if (!string.IsNullOrEmpty(previousSessionString))
                    {
                        CurrentSession = Session.FromJson(previousSessionString, _logger);
                        _logger.LogInformation("Recover previous session: " + CurrentSession!.ToJson());
                    }
                }

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
                    ["lastErrorMessage"] = lastErrorMessage,
                    ["session"] = CurrentSession == null ? "" : CurrentSession.ToJson()
                };
                _reportedProperties = twin.Properties.Reported;
                await _client?.UpdateReportedPropertiesAsync(_reportedProperties)!;
                _logger.LogInformation(twin.ToJson(Formatting.Indented));
                _logger.LogInformation("Completed UpdateReportedPropertiesAsync");
            }

            return 10;
        }
        catch (Exception ex)
        {
            _deviceConnectionString = "";
            _logger.LogError("Cannot access: " + sessionApiUrl);
            _logger.LogError("ex message: " + ex?.Message);
            _failureCount++;
            return Math.Min(60 * _failureCount, 60 * 30);
        }
    }

    private async Task OnDesiredPropertyUpdate(TwinCollection desiredProperties, object userContext)
    {
        _logger.LogInformation("OnDesiredPropertyUpdate");
        if (desiredProperties["session"] == null) return;
        var sessionString = desiredProperties["session"].Value as string;
        _logger.LogInformation("session:" + sessionString);

        if (string.IsNullOrEmpty(sessionString))
        {
            CurrentSession = Session.FromJson(sessionString!, _logger);
            _logger.LogInformation("Get ssh session from Desired Property: " + sessionString);
        }
        var reportedProperties = new TwinCollection
        {
            ["session"] = sessionString
        };
        await _client!.UpdateReportedPropertiesAsync(reportedProperties);
    }

    public Task<MethodResponse> OnNewSshMessage(MethodRequest methodRequest, object userContext)
    {
        try
        {
            var payload = methodRequest.DataAsJson;
            _logger.LogInformation($"Payload: {payload}");
            CurrentSession = Session.FromJson(payload, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in sample: {0}", ex.Message);
        }

        var result = @"{""result"":""Updated CurrentSession.""}";
        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
    }

    public Task<MethodResponse> OnRemoveSshMessage(MethodRequest methodRequest, object userContext)
    {
        try
        {
            var payload = methodRequest.DataAsJson;
            _logger.LogInformation($"Payload: {payload}");
            CurrentSession = null;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error in sample: {0}", ex.Message);
        }

        var result = @"{""result"":""Removed CurrentSession.""}";
        return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
    }

    public async Task UpdateConnectionStatus(bool isConnected)
    {
        if (_client == null) return;
        try
        {
            if (_reportedProperties?["isSshConnected"] == isConnected) return;

            var twin = await _client.GetTwinAsync();
            if (_reportedProperties != null)
            {
                _reportedProperties["isSshConnected"] = isConnected;
                await _client?.UpdateReportedPropertiesAsync(_reportedProperties)!;
            }
        }
        catch (Exception e)
        {
            _logger.LogError("UpdateConnectionStatus failed.");
        }
    }
}