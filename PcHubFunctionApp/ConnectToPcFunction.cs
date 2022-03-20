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
<p>
Dear Student, <br/>
<br/>
Please run your SSH client and connect to <br/>
IP:&nbsp&nbsp&nbsp&nbsp{sshConnection.IpAddress} <br/>
Port:&nbsp&nbsp&nbsp&nbsp{sshConnection.Port} <br/>
User:&nbsp&nbsp&nbsp&nbsp{sshConnection.Username} <br/>
Password: <br/>{sshConnection.Password}<br/> 
<br/> 
Please refresh this page to get the latest PC status.<br/> 
<br/> 
If you are using Windows and installed <a href=""https://www.bitvise.com/ssh-client-download"">Bitwise SSH client</a>, run this command in search.<br/> 
<br/> 
BvSsh -host={sshConnection.IpAddress} -port={sshConnection.Port} -user={sshConnection.Username} -password=""{sshConnection.Password}"" -openRDP=y -loginOnStartup
<br/> 
<br/> 
If you are using Mac OS or Linux,run this command in terminal<br/> 
<br/> 
ssh {sshConnection.Username}@{sshConnection.IpAddress} -p{sshConnection.Port} -L 3389:0.0.0.0:3389 -L 5900:0.0.0.0:5900
<br/> 
Enter the SSH server password, open Remote Desktop client and connect to localhost.
<br/> 
Regards,<br/> 
Azure Hybrid Cloud Lab Environment <br/> 
</p>
<br/>
<h3>Remote with Bitwise SSH Client from Windows Demo</h3>
<img src=""https://github.com/wongcyrus/AzureHybridCloudLabEnvironment/raw/main/images/BitviseLoginDemo.gif"" />
<br/>
<h3>Remote with SSH command line from MacOS Demo</h3>
<img src=""https://github.com/wongcyrus/AzureHybridCloudLabEnvironment/raw/main/images/MacLoginDemo.gif"" />

";
        else
            message = $@"
<p>
Dear Student<br/> 
<br/> 
Please wait for 30 seconds and refresh this page again, if your lab class still is ongoing. <br/> 
Computer Connected to SSH server:&nbsp&nbsp&nbsp&nbsp    {computer.IsConnected}<br/> 
Computer is Online:&nbsp&nbsp&nbsp&nbsp                  {computer.IsOnline}<br/> 
Computer is reserved for you:&nbsp&nbsp&nbsp&nbsp        {computer.IsReserved}<br/> 
Creation Time:&nbsp&nbsp&nbsp&nbsp                       {sshConnection.Timestamp!.Value.ToString("dddd, dd MMMM yyyy HH:mm:ss")}<br/> 
<br/> 
Regards,<br/> 
Azure Hybrid Cloud Lab Environment <br/> 
</p>
";
        var html = $@"
<html>
    <head>
        <title>Azure Hybrid Cloud Lab Environment</title>
    </head>
    <body>
{message}
    </body>
</html>

";
        return new ContentResult { Content = html, ContentType = "text/html" };
      
    }
}