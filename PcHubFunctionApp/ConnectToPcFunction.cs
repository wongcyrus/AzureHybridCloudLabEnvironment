using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PcHubFunctionApp.Dao;
using PcHubFunctionApp.Helper;

namespace PcHubFunctionApp
{
    public static class ConnectToPcFunction
    {
        [FunctionName(nameof(ConnectToPcFunction))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ExecutionContext context,
            ILogger log)
        {
            log.LogInformation("ConnectToPcFunction HTTP trigger processed a request.");

            string lab = req.Query["Lab"];
            string email = req.Query["Email"];
            string token = req.Query["Token"];
            var config = new Config(context);
            var sshConnectionDao = new SshConnectionDao(config, log);
            var sshConnection = sshConnectionDao.Get(lab, email);

            if (!sshConnection.Password.StartsWith(token)) return new OkObjectResult("Invalid Information.");
            var computerDao = new ComputerDao(config, log);
            var computer = computerDao.Get(lab, sshConnection.ComputerId);

            if (computer.IsConnected && computer.IsOnline && computer.IsReserved)
            {
                var message = $@"
Dear Student,

Please run your SSH client and connect to 
IP:             {sshConnection.IpAddress}
Port:           {sshConnection.Port}
User:           {sshConnection.Username}
Password:

{sshConnection.Password}

Regards,
Azure Hybrid Cloud Lab Environment 
";
                return new OkObjectResult(message);
            }
            else
            {
                var message = $@"
Dear Student,

Please wait for 30 seconds and refresh this page again! 
Computer Connected to SSH server:       {computer.IsConnected}
Computer Online:                        {computer.IsOnline}
Computer reservation:                   {computer.IsReserved}

Regards,
Azure Hybrid Cloud Lab Environment 
";

            }


            return new OkObjectResult("Invalid Information.");
        }
    }
}
