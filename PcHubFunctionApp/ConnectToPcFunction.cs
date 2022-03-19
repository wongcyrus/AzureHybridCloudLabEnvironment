using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PcHubFunctionApp.Dao;
using PcHubFunctionApp.Helper;

namespace PcHubFunctionApp;

public static class ConnectToPcFunction
{
    [FunctionName(nameof(ConnectToPcFunction))]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]
        HttpRequest req,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation("ConnectToPcFunction HTTP trigger processed a request.");

        string location = req.Query["Location"];
        string email = req.Query["Email"];
        string token = req.Query["Token"];
        var config = new Config(context);
        var sshConnectionDao = new SshConnectionDao(config, log);
        var sshConnection = sshConnectionDao.Get(location, email);

        if (sshConnection == null) return new OkObjectResult("Cannot get ssh connection information.");
        if (!sshConnection.Password.Substring(0, 10).Equals(token)) return new OkObjectResult("Invalid Token.");
        var computerDao = new ComputerDao(config, log);
        var computer = computerDao.Get(location, sshConnection.MacAddress);
        if (computer == null) return new OkObjectResult("Cannot get Computer information.");

        string message;
        if (computer.IsConnected && computer.IsOnline && computer.IsReserved)
            message = $@"
Dear Student,

Please run your SSH client and connect to 
IP:             {sshConnection.IpAddress}
Port:           {sshConnection.Port}
User:           {sshConnection.Username}
Password:

{sshConnection.Password}

Please refresh this page to get the latest PC status.

Regards,
Azure Hybrid Cloud Lab Environment 
";
        else
            message = $@"
Dear Student,

Please wait for 30 seconds and refresh this page again! 
Computer Connected to SSH server:       {computer.IsConnected}
Computer is Online:                     {computer.IsOnline}
Computer is reserved for you:           {computer.IsReserved}
Creation Time:                          {sshConnection.Timestamp!.Value.ToString("dddd, dd MMMM yyyy HH:mm:ss")}

Regards,
Azure Hybrid Cloud Lab Environment 
";
        return new OkObjectResult(message);
    }
}