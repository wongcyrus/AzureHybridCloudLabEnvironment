using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using PcHubFunctionApp.Dao;
using PcHubFunctionApp.Helper;
using PcHubFunctionApp.Model;


namespace PcHubFunctionApp;

public static class AddSshConnectionFunction
{
    [FunctionName(nameof(AddSshConnectionFunction))]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        [Queue("allocate-pc")] ICollector<SshConnection> allocatePcQueue,
        ExecutionContext context,
        ILogger log)
    {
        log.LogInformation("AddSshConnectionFunction HTTP trigger function processed a request.");
        var status = req.Form["Status"];
        var output = req.Form["Output"];
        var variables = req.Form["Variables"];

        log.LogInformation(status);
        log.LogInformation(output);
        log.LogInformation(variables);

        var config = new Config(context);
        if (status == "CREATING")
        {
            var arc = new Dictionary<string, string>
            {
                {"BASTION_ARC_ADMIN_USERNAME", config.GetConfig(Config.Key.BastionArcAdminUsername)},
                {"BASTION_ARC_ADMIN_PASSWORD", config.GetConfig(Config.Key.BastionArcAdminPassword)},
                {"BASTION_ARC_LOGIN_SERVER", config.GetConfig(Config.Key.BastionArcLoginServer)}
            };
            return new JsonResult(arc);
        }

        var sshConnection = SshConnection.FromJson(output, log);
        if (sshConnection == null) return new OkObjectResult("sshConnection null.");

        var sshConnectionDao = new SshConnectionDao(config, log);
        sshConnection.PartitionKey = sshConnection.Location;
        sshConnection.RowKey = sshConnection.Email;
        sshConnection.Status = "UNASSIGNED";
        sshConnection.ETag = ETag.All;
        sshConnection.Variables = variables;

        var computerDao = new ComputerDao(config, log);

        log.LogInformation(status);

        var sshInformation = new Dictionary<string, string>
        {
            {"Email",sshConnection.Email},
            {"Lab", sshConnection.Lab},
            {"SshStatus", sshConnection.Status},
            {"Status", status}
        };
        if (status == "CREATED")
        {
            sshConnectionDao.Upsert(sshConnection);
            allocatePcQueue.Add(sshConnection);
            log.LogInformation("Added to allocate pc queue.");
            return new JsonResult(sshInformation);
        }

        if (status == "DELETED" || status == "DELETING")
        {
            var computer = computerDao.GetComputerByEmail(sshConnection.Location, sshConnection.Email);

            if (computer == null) return new OkObjectResult(sshConnection);
            computerDao.UpdateReservation(computer, "");
            await Helper.Azure.ChangeSshConnectionToDevice(config, log, computer.GetIoTDeviceId(), null);
            sshConnection.Status = "COMPLETED";
            sshConnectionDao.Upsert(sshConnection);

            if (status == "DELETING")
            {
                var email = new Email(config, log);
                var emailMessage = new EmailMessage
                {
                    Subject = $"{sshConnection.Lab}: PC in {sshConnection.Location} session has ended",
                    To = sshConnection.Email,
                    Body = $@"
Dear Student,

Your PC session in {sshConnection.Location} has ended and please disconnect your SSH client..

Regards,
Azure Hybrid Cloud Lab Environment 
"
                };
                email.Send(emailMessage, null);
            }

            sshInformation["SshStatus"] = sshConnection.Status;
            return new JsonResult(sshInformation);
        }

        log.LogInformation(sshConnection.ToString());
        return new JsonResult(sshInformation);
    }



}