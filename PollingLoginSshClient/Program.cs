// See https://aka.ms/new-console-template for more information

using Common.Model;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System.Net;

var factory = new LoggerFactory();

ILogger logger = factory.CreateLogger("PollingLoginSshClient");

string GetAsync(string uri)
{
    using var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
    client.BaseAddress = new Uri(uri);
    var response = client.GetAsync("").Result;
    response.EnsureSuccessStatusCode();
    var result = response.Content.ReadAsStringAsync().Result;
    Console.WriteLine("Result: " + result);
    return result;
}

var result = GetAsync("http://localhost:7071/api/GetReservationFunction");

var session = JsonBase<Common.Model.Session>.FromJson(result, logger);

var connectionInfo = new ConnectionInfo(session.IpAddress, 2222,
    session.Username, new PasswordAuthenticationMethod(session.Username, session.Password));
using var client = new SshClient(connectionInfo);
client.Connect();

Console.WriteLine(client.IsConnected);

client.AddForwardedPort(new ForwardedPortRemote(3389, "localhost", 3389));
foreach (var clientForwardedPort in client.ForwardedPorts)
{
    clientForwardedPort.Start();
    Console.WriteLine(clientForwardedPort.IsStarted);
}

Console.WriteLine("Enter any key to disconnect!");
Console.ReadLine();

client.Disconnect();