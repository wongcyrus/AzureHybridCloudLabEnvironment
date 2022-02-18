using System.Net;
using Common.Model;

namespace PollingLoginSshWorkerService;

public class SessionService
{
    private const string SessionApiUrl = "http://localhost:7071/api/GetReservationFunction";

    private readonly ILogger<WindowsBackgroundService> _logger;

    public SessionService(HttpClient httpClient, ILogger<WindowsBackgroundService> logger)
    {
        _logger = logger;
    }

    private string GetAsync(string uri)
    {
        using var httpClient = new HttpClient(new HttpClientHandler
            {AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate});
        httpClient.BaseAddress = new Uri(uri);
        var response = httpClient.GetAsync("").Result;
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