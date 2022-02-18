using System.Net;
using System.Net.Sockets;
using Common.Model;
using DeviceId;
using Microsoft.AspNetCore.WebUtilities;

namespace PollingLoginSshWorkerService;

public class SessionService
{
    private const string SessionApiUrl = "http://localhost:7071/api/GetSessionFunction";

    private readonly ILogger<WindowsBackgroundService> _logger;

    public SessionService(HttpClient httpClient, ILogger<WindowsBackgroundService> logger)
    {
        _logger = logger;
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
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
        var query = new Dictionary<string, string>()
        {
            ["DeviceId"] = GetDeviceId(),
            ["IpAddress"] = GetLocalIPAddress(),
            ["MacAddress"] = GetMacAddress(),
            ["MachineName"] = Environment.MachineName
        };

        var uri = QueryHelpers.AddQueryString(baseUri, query);
        using var httpClient = new HttpClient(new HttpClientHandler
        { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
        _logger.LogInformation(uri);
        var response = httpClient.GetAsync(uri).Result;
        response.EnsureSuccessStatusCode();
        var result = response.Content.ReadAsStringAsync().Result;
        return result;
    }

    public Session? GetSessionAsync()
    {
        try
        {
            // The API returns an array with a single entry.
            var result = GetAsync(SessionApiUrl);
            var session = JsonBase<Session>.FromJson(result, _logger);
            return session;
        }
        catch (Exception)
        {
            _logger.LogError("Cannot access: " + SessionApiUrl);
            return null;
        }
    }
}